using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BuildingController : MonoBehaviour
{
    public InventoryController inventory;
    public EquipmentController equipment;
    public PlayerGatherAnimation actions;
    public ForestNavigation navigation;
    public ExplorationManager exploration;
    public EnemySpawner enemies;
    public ConstructionUI ui;
    public CraftingRecipe[] recipes;
    public BuildingDefinition[] buildings;
    public Transform structures;
    public Material material;
    public Camera worldCamera;
    [Min(.5f)]
    public float placementRange = 3;
    public static BuildingController Instance { get; private set; }
    public static bool ModalOpen => Instance && Instance.Menu != 0;
    public static bool Active => Instance && Instance.Placing;
    public bool Placing { get; private set; }
    public int Menu { get; private set; }
    public CraftingWorkbench Workbench { get; private set; }
    public PlaceableStructure SelectedStructure { get; private set; }
    public string Message { get; private set; } = "";
    public bool ConfirmDismantle { get; private set; }

    SpriteRenderer ghost;
    LineRenderer footprintOutline;
    float nextCraft, nextPlace, nextPreview;
    bool previewValid;
    string reason;
    Vector2 previewPosition;
    BuildingDefinition previewDefinition;
    readonly List<Vector2> route = new List<Vector2>();
    InteractionTarget[] essentials;
    PassiveAnimal[] animals;
    void OnEnable() => Instance = this;
    void Start()
    {
        if (!inventory || !equipment || !actions || !navigation || !exploration || !enemies || !ui || !structures || !worldCamera)
        {
            Debug.LogError("BuildingController is missing a required scene reference.", this);
            enabled = false;
            return;
        }
        essentials = FindObjectsByType<InteractionTarget>();
        animals = FindObjectsByType<PassiveAnimal>();
    }

    void OnDisable()
    {
        Close();
        CancelPlacement();
        if (Instance == this)
            Instance = null;
    }

    bool Ready => isActiveAndEnabled && inventory && !inventory.ManagementLocked && !PlayerSurvivalController.InputLocked && actions && !actions.Busy;

    public void OpenWorkbench(CraftingWorkbench bench)
    {
        if (!Ready || !bench || !bench.Reachable(transform.position))
            return;
        Open(1);
        Workbench = bench;
        if (ui)
            ui.Show();
    }

    public void OpenStructure(PlaceableStructure structure)
    {
        if (!Ready || !structure || !structure.Reachable(transform.position))
            return;
        Open(2);
        SelectedStructure = structure;
        if (ui)
            ui.Show();
    }

    void Open(int menu)
    {
        CancelPlacement();
        var survival = GetComponent<PlayerSurvivalController>();
        if (survival && survival.inventoryUI)
            survival.inventoryUI.Close();
        Menu = menu;
        ConfirmDismantle = false;
        Message = "";
        if (ui)
            ui.Show();
    }

    public void Close()
    {
        Menu = 0;
        Workbench = null;
        SelectedStructure = null;
        ConfirmDismantle = false;
        if (ui)
            ui.Hide();
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public bool WorkshopUnlocked => !Workbench || !Workbench.GetComponent<SawmillStation>() || Workbench.GetComponent<SawmillStation>().Unlocked;
    public void UnlockSawmill()
    {
        if (!Ready || Menu != 1 || !Workbench || !Workbench.Reachable(transform.position)) return;
        var station = Workbench.GetComponent<SawmillStation>();
        Message = station && station.TryUnlock(inventory) ? "WORKSHOP UNLOCKED � CHOOSE A RECIPE" : "10 WOOD LOGS REQUIRED";
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        if (ui) ui.Show();
    }
    public int StationLevel => Workbench && Workbench.GetComponent<SawmillStation>() ? Workbench.GetComponent<SawmillStation>().Level : 1;
    public void UpgradeSawmill(){if(!Ready || Menu!=1 || !Workbench || !Workbench.Reachable(transform.position))return;var s=Workbench.GetComponent<SawmillStation>();Message=s&&s.TryUpgrade(inventory)?"SAWMILL UPGRADED":"MORE MATERIALS REQUIRED OR MAX LEVEL";}
    public bool CanCraft(CraftingRecipe recipe) => Ready && WorkshopUnlocked && Menu == 1 && Workbench && Workbench.Reachable(transform.position) && recipe && System.Array.IndexOf(recipes,recipe)>=0 && StationLevel>=recipe.requiredSawmillLevel && inventory.CraftBatch(recipe.ingredients, recipe.result, recipe.quantity, false);
    public bool Craft(CraftingRecipe recipe)
    {
        if (Time.time < nextCraft || !CanCraft(recipe))
            return false;
        nextCraft = Time.time + .2f;
        bool ok = inventory.CraftBatch(recipe.ingredients, recipe.result, recipe.quantity);
        Message = ok ? "CRAFTED " + recipe.result.DisplayName : "MATERIALS OR SPACE UNAVAILABLE";
        return ok;
    }

    public BuildingDefinition SelectedBuilding
    {
        get
        {
            var item = equipment ? equipment.Equipped : null;
            if (buildings != null)
                foreach (var b in buildings)
                    if (b && b.item == item)
                        return b;
            return null;
        }
    }

    public bool TogglePlacement()
    {
        if (Placing)
        {
            CancelPlacement();
            return true;
        }

        if (!Ready || Menu != 0 || TradingUI.IsOpen || InventoryUI.IsOpen || !SelectedBuilding)
        {
            Message = "Equip a building item in your hotbar first";
            GetComponent<PlayerInteractionController>().Notice(Message);
            return false;
        }

        Placing = true;
        nextPreview = 0;
        var go = new GameObject("Building Preview");
        ghost = go.AddComponent<SpriteRenderer>();
        ghost.sharedMaterial = material;
        ghost.sortingLayerName = "Foreground";
        ghost.sortingOrder = 10;
        footprintOutline = go.AddComponent<LineRenderer>();
        footprintOutline.useWorldSpace = false;
        footprintOutline.loop = true;
        footprintOutline.positionCount = 4;
        footprintOutline.widthMultiplier = .04f;
        footprintOutline.sharedMaterial = material;
        footprintOutline.sortingLayerName = "Foreground";
        footprintOutline.sortingOrder = 11;
        return true;
    }

    public void CancelPlacement()
    {
        Placing = false;
        if (ghost)
            Destroy(ghost.gameObject);
        if (ui)
            ui.SetBuildHint("");
    }

    public bool CanPlace(BuildingDefinition def, Vector2 pos, out string error)
    {
        error = "";
        if (!Ready || !def || !def.prefab || !def.item || inventory.Count(def.item) < 1)
        {
            error = "NOT ENOUGH ITEMS";
            return false;
        }

        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || Vector2.Distance(pos, transform.position) > Mathf.Max(.5f, placementRange))
        {
            error = "TOO FAR AWAY";
            return false;
        }

        Vector2 size = def.footprint;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (!navigation.Terrain(pos + new Vector2(size.x * x, size.y * y) * .5f))
                {
                    error = "BLOCKED TERRAIN";
                    return false;
                }

        foreach (var c in Physics2D.OverlapBoxAll(pos, size + Vector2.one * .12f, 0))
            if (!c.isTrigger)
            {
                error = "OVERLAPPING OBJECT";
                return false;
            }

        // Preserve access with actual route checks rather than banning entire axes.

        if (essentials != null)
            foreach (var target in essentials)
                if (target && (target is CraftingWorkbench || target is CampfireInteraction || target is LootChest || target is ResourcePickup) && Vector2.Distance(pos, target.transform.position) < 1.4f)
                {
                    error = "KEEP INTERACTION ACCESS CLEAR";
                    return false;
                }

        var watch = System.Diagnostics.Stopwatch.StartNew();
        navigation.SimulatedObstacle = new Rect(pos - size * .5f - Vector2.one * .3f, size + Vector2.one * .6f);
        try
        {
            Vector2 hub = new Vector2(0, -2);
            if (!navigation.Find(transform.position, hub, route, true))
            {
                error = "PLAYER PATH WOULD BE BLOCKED";
                return false;
            }

            foreach (var r in exploration.regions)
            {
                if (!navigation.Find(hub, r.bounds.center, route, true))
                {
                    error = "REGION PATH WOULD BE BLOCKED";
                    return false;
                }

                if (watch.ElapsedMilliseconds > 80)
                {
                    error = "PATH CHECK TIMED OUT";
                    return false;
                }
            }

            foreach (var enemy in enemies.Snapshot())
                if (enemy && !navigation.Find(enemy.transform.position, hub, route, true))
                {
                    error = "ENEMY WOULD BE TRAPPED";
                    return false;
                }

            if (animals != null)
                foreach (var animal in animals)
                    if (animal && !navigation.Find(animal.transform.position, hub, route, true))
                    {
                        error = "WILDLIFE WOULD BE TRAPPED";
                        return false;
                    }
        }
        finally
        {
            navigation.SimulatedObstacle = null;
        }

        return true;
    }

    public bool Place(BuildingDefinition def, Vector2 pos)
    {
        if (def != SelectedBuilding || !Placing || Menu != 0 || Time.time < nextPlace || !CanPlace(def, pos, out reason))
            return false;
        nextPlace = Time.time + .2f;
        var go = Instantiate(def.prefab, pos, Quaternion.identity, structures);
        go.navigation = navigation;
        if (!inventory.Remove(def.item, 1))
        {
            Destroy(go.gameObject);
            return false;
        }

        Physics2D.SyncTransforms();
        navigation.Invalidate();
        Message = "PLACED " + def.item.DisplayName;
        nextPreview = 0;
        if (inventory.Count(def.item) == 0)
            CancelPlacement();
        return true;
    }

    public void Repair()
    {
        if (!Ready || Menu != 2 || !SelectedStructure || !SelectedStructure.Reachable(transform.position))
            return;
        float restored = SelectedStructure.Repair(inventory);
        Message = restored > 0 ? "REPAIRED +" + restored : "DAMAGED STRUCTURE AND REPAIR KIT REQUIRED";
    }

    public void Dismantle()
    {
        if (!Ready || Menu != 2 || !SelectedStructure || !SelectedStructure.Reachable(transform.position))
            return;
        if (!ConfirmDismantle)
        {
            ConfirmDismantle = true;
            Message = "Click CONFIRM DISMANTLE again. No material refund.";
            return;
        }

        if (!SelectedStructure.Dismantle())
        {
            Message = "EMPTY STORAGE BEFORE DISMANTLING";
            ConfirmDismantle = false;
            return;
        }

        Close();
    }

    public void Transfer(bool deposit, int slot, int amount)
    {
        if (!Ready || Menu != 2 || !SelectedStructure || !SelectedStructure.Reachable(transform.position) || !SelectedStructure.storage)
            return;
        if (deposit && SelectedStructure.IsDestroyed)
        {
            Message = "WRECK: WITHDRAW ONLY";
            return;
        }

        int n = deposit ? SelectedStructure.storage.Deposit(inventory, slot, amount) : SelectedStructure.storage.Withdraw(inventory, slot, amount);
        Message = n > 0 ? "TRANSFERRED " + n : "DESTINATION FULL OR ITEM UNAVAILABLE";
    }

    void Update()
    {
        if (!Ready)
        {
            Close();
            CancelPlacement();
            return;
        }

        if (Menu == 1 && (!Workbench || !Workbench.Reachable(transform.position)) || Menu == 2 && (!SelectedStructure || !SelectedStructure.Reachable(transform.position)))
            Close();
        var k = Keyboard.current;
        var mouse = Mouse.current;
        if (k != null && k.escapeKey.wasPressedThisFrame)
        {
            if (ui && ui.PurchaseOpen) { ui.CancelPurchase(); return; }
            Close();
            CancelPlacement();
            return;
        }

        if (k != null && k.bKey.wasPressedThisFrame && !EquipmentController.Typing)
            TogglePlacement();
        if (!Placing)
            return;
        if (InventoryUI.IsOpen)
        {
            CancelPlacement();
            return;
        }

        if (mouse == null || !worldCamera)
            return;
        if (mouse.rightButton.wasPressedThisFrame)
        {
            CancelPlacement();
            return;
        }

        var def = SelectedBuilding;
        if (!def)
        {
            CancelPlacement();
            return;
        }

        Vector3 screen = mouse.position.ReadValue();
        screen.z = -worldCamera.transform.position.z;
        Vector2 pos = worldCamera.ScreenToWorldPoint(screen);
        pos = new Vector2(Mathf.Round(pos.x * 2) / 2, Mathf.Round(pos.y * 2) / 2);
        if (Time.time >= nextPreview || def != previewDefinition)
        {
            previewPosition = pos;
            previewDefinition = def;
            nextPreview = Time.time + .25f;
            previewValid = CanPlace(def, pos, out reason);
        }

        ghost.sprite = def.prefab.visual.sprite;
        ghost.transform.position = pos;
        ghost.color = previewValid && pos == previewPosition ? new Color(.2f, 1, .3f, .55f) : new Color(1, .2f, .2f, .55f);
        Vector2 half = def.footprint * .5f;
        footprintOutline.SetPosition(0, new Vector3(-half.x, -half.y));
        footprintOutline.SetPosition(1, new Vector3(-half.x, half.y));
        footprintOutline.SetPosition(2, new Vector3(half.x, half.y));
        footprintOutline.SetPosition(3, new Vector3(half.x, -half.y));
        footprintOutline.startColor = footprintOutline.endColor = ghost.color;
        string validity = pos != previewPosition ? "CHECKING..." : previewValid ? "VALID • CLICK TO PLACE" : reason;
        ui.SetBuildHint("BUILDING: " + def.item.DisplayName + " — " + validity + "   RMB / ESC: CANCEL");
        if (mouse.leftButton.wasPressedThisFrame && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            Place(def, pos);
    }
}
