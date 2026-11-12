using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class ConstructionUI : MonoBehaviour
{
    public BuildingController controller;
    public Canvas canvas;
    [Header("Optional theme assets (auto-populated if left empty)")]
    public Font customFont;
    public Sprite panelWood, panelBanner, slotNormal, slotSelect, barTrack, barFill, iconLeaves, iconUpgrade;

    GameObject panel;
    Text title, status, buildHint;
    Button[] recipeButtons, playerButtons, storageButtons;
    Image[] recipeFrames, playerIcons, playerFrames, storageIcons, storageFrames;
    Button upgrade;
    Button repair, dismantle, depositOne, depositStack, withdrawOne, withdrawStack;
    int playerSlot = -1, storageSlot = -1;
    float nextRefresh;
    Font font;
    Image healthBar, healthTrack;
    GameObject workshopContent;
    GameObject unlockModal;
    Text unlockCost, unlockNote;
    Image unlockWood, unlockPreview;
    Button unlockConfirm;
    GameObject purchaseModal;
    CraftingRecipe pendingPurchase;
    Text purchaseName, purchaseCost, purchaseMessage;
    Image purchaseIcon;
    Button purchaseConfirm;
    public bool PurchaseOpen => purchaseModal && purchaseModal.activeSelf;

    public void CancelPurchase()
    {
        pendingPurchase = null;
        if (purchaseModal) purchaseModal.SetActive(false);
        if (UnityEngine.EventSystems.EventSystem.current)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    void SelectRecipe(CraftingRecipe recipe)
    {
        if (PurchaseOpen || !recipe || !recipe.result) return;
        if (!recipe.result.craftedWeapon) { controller.Craft(recipe); return; }
        pendingPurchase = recipe;
        purchaseName.text = recipe.displayName.ToUpperInvariant() + "  ×1";
        purchaseIcon.sprite = recipe.result.Icon;
        purchaseIcon.color = recipe.result.IconTint;
        purchaseModal.SetActive(true);
        purchaseModal.transform.SetAsLastSibling();
        RefreshPurchase();
    }

    void ConfirmPurchase()
    {
        if (!PurchaseOpen || !pendingPurchase) return;
        if (!controller.CanCraft(pendingPurchase)) { RefreshPurchase(); return; }
        var recipe = pendingPurchase;
        // Clear pending state before committing so repeated clicks cannot buy twice.
        CancelPurchase();
        controller.Craft(recipe);
        Refresh();
    }

    void RefreshPurchase()
    {
        if (!PurchaseOpen || !pendingPurchase) return;
        if (controller.Menu != 1) { CancelPurchase(); return; }
        var costs = new StringBuilder();
        foreach (var c in pendingPurchase.ingredients)
        {
            int have = controller.inventory.Count(c.item);
            costs.Append(c.item.DisplayName).Append("   <color=")
                .Append(have >= c.amount ? "#9BE061" : "#FF9E7D").Append(">")
                .Append(c.amount).Append("</color>   <size=18>(you have ").Append(have).Append(")</size>\n");
        }
        purchaseCost.text = costs.ToString().TrimEnd();
        bool can = controller.CanCraft(pendingPurchase);
        purchaseConfirm.interactable = can;
        purchaseMessage.text = can ? "Materials are spent only when you confirm." :
            controller.StationLevel < pendingPurchase.requiredSawmillLevel ? "Requires sawmill level " + pendingPurchase.requiredSawmillLevel : "Not enough materials or inventory space.";
    }

    void BuildUnlockModal()
    {
        var shade = DecorImage(panel.transform, "Locked Workshop", null, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(1120,690));
        shade.color = Color.clear;
        shade.raycastTarget = true;
        unlockModal = shade.gameObject;
        var card = DecorImage(shade.transform, "Unlock Frame", panelWood, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(780,550), true);
        card.raycastTarget = true;
        if (!panelWood) card.color = new Color(.16f,.12f,.07f);
        var title = Label(card.transform, "Unlock Heading", new Vector2(32,24), new Vector2(716,42), 30, TextAnchor.MiddleCenter);
        title.text = "RESTORE THE SAWMILL"; title.color = Gold;
        var subtitle = Label(card.transform, "Unlock Subtitle", new Vector2(32,72), new Vector2(716,44), 20, TextAnchor.MiddleCenter);
        subtitle.text = "A little timber. A whole workshop of possibilities."; subtitle.color = WarmSub;
        unlockPreview = DecorImage(card.transform, "Sawmill Preview", null, new Vector2(0,1), new Vector2(0,1), new Vector2(68,-142), new Vector2(150,150));
        unlockPreview.preserveAspect = true;
        var benefit = Label(card.transform, "Unlock Benefits", new Vector2(256,140), new Vector2(480,104), 23);
        benefit.text = "UNLOCK LEVEL 1 CRAFTING\n<size=20>Craft wooden tools and fences.\nUpgrade later for stronger equipment.</size>"; benefit.color = Cream;
        var costSlot = DecorImage(card.transform, "Wood Cost Slot", slotNormal, new Vector2(0,1), new Vector2(0,1), new Vector2(256,-266), new Vector2(76,76), true);
        unlockWood = DecorImage(costSlot.transform, "Wood", null, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(56,56));
        unlockWood.preserveAspect = true;
        unlockCost = Label(card.transform, "Unlock Cost", new Vector2(350,266), new Vector2(380,76), 25);
        unlockNote = Label(card.transform, "Unlock Hint", new Vector2(32,366), new Vector2(716,56), 20, TextAnchor.MiddleCenter);
        unlockConfirm = MakeButton(card.transform, "UNLOCK WORKSHOP", new Vector2(32,452), new Vector2(430,58), panelBanner, controller.UnlockSawmill, 23);
        unlockConfirm.GetComponentInChildren<Text>().color = Gold;
        MakeButton(card.transform, "NOT NOW", new Vector2(482,452), new Vector2(266,58), panelBanner, controller.Close, 22);
        unlockModal.SetActive(false);
    }

    void BuildPurchaseModal()
    {
        var shade = DecorImage(panel.transform, "Purchase Confirmation", null, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(1120,690));
        shade.color = new Color(0,0,0,.78f);
        shade.raycastTarget = true;
        purchaseModal = shade.gameObject;
        var card = DecorImage(shade.transform, "Wood Frame", panelWood, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(700,490), true);
        card.raycastTarget = true;
        var heading = Label(card.transform, "Confirmation Title", new Vector2(32,24), new Vector2(636,40), 28);
        heading.text = "CONFIRM WEAPON PURCHASE"; heading.color = Gold;
        DecorImage(card.transform, "Divider", panelBanner, new Vector2(0,1), new Vector2(0,1), new Vector2(32,-74), new Vector2(636,4), true);
        var slot = DecorImage(card.transform, "Weapon Slot", slotNormal, new Vector2(0,1), new Vector2(0,1), new Vector2(32,-100), new Vector2(126,126), true);
        purchaseIcon = DecorImage(slot.transform, "Weapon", null, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(94,94));
        purchaseName = Label(card.transform, "Weapon Name", new Vector2(182,100), new Vector2(486,38), 24);
        var caption = Label(card.transform, "Materials Caption", new Vector2(182,146), new Vector2(486,30), 18);
        caption.text = "MATERIAL COST"; caption.color = WarmSub;
        purchaseCost = Label(card.transform, "Material Cost", new Vector2(182,180), new Vector2(486,120), 22, TextAnchor.UpperLeft);
        purchaseMessage = Label(card.transform, "Purchase Note", new Vector2(32,316), new Vector2(636,52), 19, TextAnchor.MiddleCenter);
        purchaseMessage.horizontalOverflow = HorizontalWrapMode.Wrap;
        MakeButton(card.transform, "CANCEL", new Vector2(368,398), new Vector2(300,56), panelBanner, CancelPurchase, 22);
        purchaseConfirm = MakeButton(card.transform, "CONFIRM PURCHASE", new Vector2(32,398), new Vector2(318,56), panelBanner, ConfirmPurchase, 22);
        purchaseConfirm.GetComponentInChildren<Text>().color = Gold;
        if(iconLeaves) DecorImage(card.transform,"Leaf",iconLeaves,new Vector2(1,1),new Vector2(.5f,.5f),new Vector2(-12,-12),new Vector2(36,36));
        CancelPurchase();
    }

    static readonly Color Cream = new Color(1f, .95f, .84f);
    static readonly Color WarmSub = new Color(.91f, .81f, .63f);
    static readonly Color Gold = new Color(1f, .85f, .42f);
    static readonly Color DarkOutline = new Color(.08f, .05f, .02f, .92f);

    Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize = 22, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.color = Cream;
        t.alignment = align;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var ol = go.GetComponent<Outline>();
        ol.effectColor = DarkOutline;
        ol.effectDistance = new Vector2(1f, -1f);
        return t;
    }

    Image DecorImage(Transform parent, string name, Sprite sp, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, bool sliced = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var im = go.GetComponent<Image>();
        im.sprite = sp;
        im.type = sliced && sp ? Image.Type.Sliced : Image.Type.Simple;
        im.preserveAspect = !sliced;
        im.raycastTarget = false;
        return im;
    }

    Button MakeButton(Transform parent, string name, Vector2 position, Vector2 size, Sprite bgSprite, UnityEngine.Events.UnityAction action, int fontSize = 18)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
        var img = go.GetComponent<Image>();
        if (bgSprite)
        {
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else img.color = new Color(.24f, .3f, .16f);

        var b = go.GetComponent<Button>();
        var cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, .96f, .85f, 1f);
        cb.pressedColor = new Color(.85f, .78f, .65f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(.62f, .58f, .52f, .88f);
        b.colors = cb;
        b.onClick.AddListener(action);

        var label = Label(go.transform, "Label", new Vector2(8, 4), size - new Vector2(16, 8), fontSize, TextAnchor.MiddleCenter);
        label.text = name;
        return b;
    }

    void Start()
    {
        font = customFont ? customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Reuse the pre-existing "Construction Window" on Canvas if present so Inspector/Editor views stay clean
        var existing = canvas.transform.Find("Construction Window");
        if (existing) Destroy(existing.gameObject);

        panel = new GameObject("Construction Window", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(1120, 690);
        var pImg = panel.GetComponent<Image>();
        if (panelWood)
        {
            pImg.sprite = panelWood;
            pImg.type = Image.Type.Sliced;
            pImg.color = Color.white;
            var ll = DecorImage(panel.transform, "LeafLeft", iconLeaves, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(10, -10), new Vector2(32, 32));
            var lr = DecorImage(panel.transform, "LeafRight", iconLeaves, new Vector2(1, 1), new Vector2(.5f, .5f), new Vector2(-10, -10), new Vector2(32, 32));
            lr.rectTransform.localScale = new Vector3(-1, 1, 1);
        }
        else pImg.color = new Color(.08f, .1f, .06f, .97f);

        // Header bar & divider
        var headerBanner = DecorImage(panel.transform, "HeaderBanner", panelBanner, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(936, 52), true);
        if (iconUpgrade) DecorImage(headerBanner.transform, "HeaderIcon", iconUpgrade, new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(28, 0), new Vector2(28, 28));
        title = Label(panel.transform, "Title", new Vector2(74, 22), new Vector2(870, 44), 26, TextAnchor.MiddleLeft);
        title.color = Gold;

        var closeBtn = MakeButton(panel.transform, "CLOSE", new Vector2(972, 20), new Vector2(124, 48), panelBanner, controller.Close, 20);
        closeBtn.GetComponent<Image>().color = new Color(1f, .78f, .72f);

        // Recipe cards (2 columns)
        recipeButtons = new Button[controller.recipes.Length];
        recipeFrames = new Image[controller.recipes.Length];
        for (int i = 0; i < recipeButtons.Length; i++)
        {
            int index = i;
            var btn = MakeButton(panel.transform, "Recipe", new Vector2(30 + (i % 2) * 536, 84 + (i / 2) * 108), new Vector2(524, 96), panelBanner, () => SelectRecipe(controller.recipes[index]), 18);
            recipeButtons[i] = btn;

            // Left align the text to the right of the icon slot
            var lbl = btn.transform.Find("Label").GetComponent<Text>();
            var lrt = lbl.rectTransform;
            lrt.anchoredPosition = new Vector2(104, -6);
            lrt.sizeDelta = new Vector2(406, 84);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.lineSpacing = 1.08f;

            // Recessed icon box on the left of each recipe card
            var slotBg = DecorImage(btn.transform, "IconSlot", slotNormal, new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(52, 0), new Vector2(76, 76), true);
            var icon = DecorImage(slotBg.transform, "Recipe Icon", controller.recipes[index].result.Icon, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(54, 54));
            icon.color = controller.recipes[index].result.IconTint;

            // Gold highlight frame when craftable
            if (slotSelect)
            {
                var hi = DecorImage(btn.transform, "CraftableFrame", slotSelect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(526, 98), true);
                hi.enabled = false;
                recipeFrames[i] = hi;
            }
        }

        // Upgrade Sawmill button at the bottom of the crafting view
        upgrade = MakeButton(panel.transform, "UPGRADE SAWMILL", new Vector2(30, 526), new Vector2(1060, 54), panelBanner, controller.UpgradeSawmill, 20);
        if (slotSelect)
        {
            var upHi = DecorImage(upgrade.transform, "UpgradeBorder", slotSelect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1062, 56), true);
            upHi.color = new Color(1f, .9f, .55f, .65f);
        }

        // Player & Storage slots (for Storage Chest modal)
        playerButtons = new Button[25];
        playerIcons = new Image[25];
        playerFrames = new Image[25];
        for (int i = 0; i < 25; i++)
        {
            int index = i;
            var b = MakeButton(panel.transform, "Player slot", new Vector2(28 + (i % 5) * 102, 96 + (i / 5) * 72), new Vector2(96, 66), slotNormal, () =>
            {
                playerSlot = index;
                Refresh();
            }, 15);
            playerButtons[i] = b;
            playerIcons[i] = DecorImage(b.transform, "ItemIcon", null, new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(24, 0), new Vector2(36, 36));
            playerIcons[i].enabled = false;
            var lbl = b.transform.Find("Label").GetComponent<Text>();
            lbl.rectTransform.anchoredPosition = new Vector2(42, -4);
            lbl.rectTransform.sizeDelta = new Vector2(50, 58);
            lbl.alignment = TextAnchor.MiddleLeft;
            if (slotSelect)
            {
                playerFrames[i] = DecorImage(b.transform, "SelectFrame", slotSelect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(98, 68), true);
                playerFrames[i].enabled = false;
            }
        }

        storageButtons = new Button[12];
        storageIcons = new Image[12];
        storageFrames = new Image[12];
        for (int i = 0; i < 12; i++)
        {
            int index = i;
            var b = MakeButton(panel.transform, "Storage slot", new Vector2(600 + (i % 3) * 164, 96 + (i / 3) * 86), new Vector2(154, 78), slotNormal, () =>
            {
                storageSlot = index;
                Refresh();
            }, 16);
            storageButtons[i] = b;
            storageIcons[i] = DecorImage(b.transform, "ItemIcon", null, new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(30, 0), new Vector2(44, 44));
            storageIcons[i].enabled = false;
            var lbl = b.transform.Find("Label").GetComponent<Text>();
            lbl.rectTransform.anchoredPosition = new Vector2(54, -4);
            lbl.rectTransform.sizeDelta = new Vector2(94, 70);
            lbl.alignment = TextAnchor.MiddleLeft;
            if (slotSelect)
            {
                storageFrames[i] = DecorImage(b.transform, "SelectFrame", slotSelect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(156, 80), true);
                storageFrames[i].enabled = false;
            }
        }

        depositOne = MakeButton(panel.transform, "DEPOSIT ONE", new Vector2(28, 468), new Vector2(242, 46), panelBanner, () => controller.Transfer(true, playerSlot, 1), 18);
        depositStack = MakeButton(panel.transform, "DEPOSIT STACK", new Vector2(282, 468), new Vector2(242, 46), panelBanner, () => controller.Transfer(true, playerSlot, 9999), 18);
        withdrawOne = MakeButton(panel.transform, "WITHDRAW ONE", new Vector2(600, 468), new Vector2(236, 46), panelBanner, () => controller.Transfer(false, storageSlot, 1), 18);
        withdrawStack = MakeButton(panel.transform, "WITHDRAW STACK", new Vector2(846, 468), new Vector2(244, 46), panelBanner, () => controller.Transfer(false, storageSlot, 9999), 18);
        repair = MakeButton(panel.transform, "REPAIR (1 KIT)", new Vector2(28, 526), new Vector2(320, 48), panelBanner, controller.Repair, 19);
        dismantle = MakeButton(panel.transform, "DISMANTLE", new Vector2(600, 526), new Vector2(490, 48), panelBanner, controller.Dismantle, 19);

        status = Label(panel.transform, "Status", new Vector2(30, 594), new Vector2(1060, 66), 20, TextAnchor.MiddleCenter);
        status.color = Gold;

        healthTrack = DecorImage(panel.transform, "Structure Health Track", barTrack, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -582), new Vector2(1060, 14), true);
        var health = new GameObject("Structure Health", typeof(RectTransform), typeof(Image));
        health.transform.SetParent(panel.transform, false);
        var healthRect = health.GetComponent<RectTransform>();
        healthRect.anchorMin = healthRect.anchorMax = healthRect.pivot = new Vector2(0, 1);
        healthRect.anchoredPosition = new Vector2(33, -585);
        healthRect.sizeDelta = new Vector2(1054, 8);
        healthBar = health.GetComponent<Image>();
        if (barFill) { healthBar.sprite = barFill; healthBar.type = Image.Type.Sliced; }
        healthBar.color = new Color(.55f, .85f, .32f);
        healthBar.raycastTarget = false;

        // Floating Building Placement Hint on Canvas with auto-sizing banner backdrop
        buildHint = Label(canvas.transform, "Building Hint", new Vector2(460, 220), new Vector2(1000, 52), 21, TextAnchor.MiddleCenter);
        if (panelBanner)
        {
            var bdGo = new GameObject("BuildingHintBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HUDBackdrop));
            bdGo.transform.SetParent(canvas.transform, false);
            bdGo.transform.SetSiblingIndex(buildHint.transform.GetSiblingIndex());
            var bdImg = bdGo.GetComponent<Image>();
            bdImg.sprite = panelBanner; bdImg.type = Image.Type.Sliced; bdImg.color = Color.white; bdImg.raycastTarget = false;
            var bd = bdGo.GetComponent<HUDBackdrop>();
            bd.target = buildHint; bd.paddingLeft = 26; bd.paddingRight = 26; bd.paddingY = 10;
        }

        BuildPurchaseModal();
        // Keep the workshop and restore screen as separate views.
        workshopContent = new GameObject("Workshop Content", typeof(RectTransform));
        workshopContent.transform.SetParent(panel.transform, false);
        var contentRect = workshopContent.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
        for (int i = panel.transform.childCount - 1; i >= 0; i--)
        {
            var child = panel.transform.GetChild(i);
            if (child == workshopContent.transform) continue;
            child.SetParent(workshopContent.transform, false);
            child.SetAsFirstSibling();
        }
        BuildUnlockModal();
        panel.SetActive(false);
    }

    public void Show()
    {
        playerSlot = storageSlot = -1;
        if (panel)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
        }
    }

    public void Hide()
    {
        CancelPurchase();
        if (panel)
            panel.SetActive(false);
    }

    public void SetBuildHint(string text)
    {
        if (buildHint)
            buildHint.text = text;
    }

    void Update()
    {
        if (controller.Menu != 0 && panel && !panel.activeSelf)
            Show();
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + .15f;
            Refresh();
        }
    }

    static void Text(Button b, string text) => b.GetComponentInChildren<Text>().text = text;
    void Refresh()
    {
        if (!panel || !panel.activeSelf)
            return;
        bool crafting = controller.Menu == 1;
        bool needsUnlock = crafting && !controller.WorkshopUnlocked;
        if (workshopContent) workshopContent.SetActive(!needsUnlock);
        panel.GetComponent<Image>().enabled = !needsUnlock;
        if (unlockModal)
        {
            unlockModal.SetActive(needsUnlock);
            if (needsUnlock)
            {
                CancelPurchase();
                unlockModal.transform.SetAsLastSibling();
                var wood = controller.inventory.FindItem("wood_log");
                int have = wood ? controller.inventory.Count(wood) : 0;
                unlockWood.sprite = wood ? wood.Icon : null;
                unlockWood.enabled = wood && wood.Icon;
                var mill = controller.Workbench.GetComponent<SawmillStation>();
                unlockPreview.sprite = mill.visual ? mill.visual.sprite : null;
                unlockPreview.enabled = unlockPreview.sprite;
                unlockCost.text = "10 WOOD LOGS\n<size=20>You have " + have + " / 10</size>";
                unlockCost.color = have >= SawmillStation.UnlockCost ? new Color(.62f,.88f,.4f) : Cream;
                unlockConfirm.interactable = wood && have >= SawmillStation.UnlockCost && !controller.inventory.ManagementLocked;
                unlockNote.text = have >= SawmillStation.UnlockCost ? "Ready to build. Unlocking spends 10 wood logs once." : "Gather " + (SawmillStation.UnlockCost-have) + " more wood logs to open your workshop.";
            }
        }
        if (needsUnlock) return;
        var structure = controller.SelectedStructure;
        bool storage = !crafting && structure && structure.storage;
        title.text = crafting
            ? "SAWMILL WORKSHOP  <color=#FFF3D6>—  LEVEL " + controller.StationLevel + "</color>"
            : storage ? "PLAYER INVENTORY                                      STORAGE CHEST" : "STRUCTURE MANAGEMENT";

        for (int i = 0; i < recipeButtons.Length; i++)
        {
            var b = recipeButtons[i];
            b.gameObject.SetActive(crafting);
            if (!crafting)
                continue;
            var recipe = controller.recipes[i];
            bool locked = controller.StationLevel < recipe.requiredSawmillLevel;
            bool can = controller.CanCraft(recipe);

            var text = new StringBuilder();
            text.Append("<size=20><color=#FFF3D6>").Append(recipe.displayName.ToUpperInvariant()).Append("</color></size>\n");
            foreach (var cost in recipe.ingredients)
            {
                int have = controller.inventory.Count(cost.item);
                string col = have >= cost.amount ? "#9BE061" : "#FF9E7D";
                text.Append("<color=#E8CFA1>").Append(cost.item.DisplayName).Append(":</color> <color=").Append(col).Append(">")
                    .Append(have).Append(" / ").Append(cost.amount).Append("</color>   ");
            }
            if (locked)
                text.Append("\n<size=15><color=#FF785A>REQUIRES SAWMILL LEVEL ").Append(recipe.requiredSawmillLevel).Append("</color></size>");
            else if (can)
                text.Append(recipe.result.craftedWeapon ? "\n<size=16><color=#8EE06A>CLICK TO REVIEW PURCHASE</color></size>" : "\n<size=16><color=#8EE06A>CLICK TO CRAFT</color></size>");
            else
                text.Append("\n<size=15><color=#D9B382>MATERIALS OR SPACE REQUIRED</color></size>");

            Text(b, text.ToString());
            b.interactable = !needsUnlock && !PurchaseOpen && (can || recipe.result.craftedWeapon);
            if (recipeFrames != null && recipeFrames[i]) recipeFrames[i].enabled = can;
        }

        for (int i = 0; i < 25; i++)
        {
            playerButtons[i].gameObject.SetActive(storage);
            if (storage)
            {
                var slot = controller.inventory.GetSlot(i);
                if (playerIcons != null && playerIcons[i])
                {
                    playerIcons[i].sprite = !slot.Empty ? slot.Item.Icon : null;
                    playerIcons[i].color = !slot.Empty ? slot.Item.IconTint : Color.white;
                    playerIcons[i].enabled = !slot.Empty && slot.Item.Icon;
                }
                if (playerFrames != null && playerFrames[i]) playerFrames[i].enabled = (i == playerSlot);
                Text(playerButtons[i], slot.Empty ? "<color=#7A6855>—</color>" : "<size=13>" + slot.Item.DisplayName + "</size>\n<color=#FFD86B>x" + slot.Quantity + "</color>");
            }
        }

        for (int i = 0; i < 12; i++)
        {
            storageButtons[i].gameObject.SetActive(storage);
            if (storage)
            {
                var slot = structure.storage.GetSlot(i);
                if (storageIcons != null && storageIcons[i])
                {
                    storageIcons[i].sprite = !slot.Empty ? slot.Item.Icon : null;
                    storageIcons[i].color = !slot.Empty ? slot.Item.IconTint : Color.white;
                    storageIcons[i].enabled = !slot.Empty && slot.Item.Icon;
                }
                if (storageFrames != null && storageFrames[i]) storageFrames[i].enabled = (i == storageSlot);
                Text(storageButtons[i], slot.Empty ? "<color=#7A6855>Empty</color>" : slot.Item.DisplayName + "\n<color=#FFD86B>x" + slot.Quantity + "</color>");
            }
        }

        foreach (var b in new[] { depositOne, depositStack, withdrawOne, withdrawStack })
            b.gameObject.SetActive(storage);
        repair.gameObject.SetActive(false);
        upgrade.gameObject.SetActive(crafting);
        if (crafting)
        {
            var mill = controller.Workbench ? controller.Workbench.GetComponent<SawmillStation>() : null;
            bool canUp = mill && mill.Level < 3;
            upgrade.interactable = !needsUnlock && canUp && !PurchaseOpen;
            var info = new StringBuilder(canUp ? "<color=#FFD86B>UPGRADE TO LEVEL " + (mill.Level + 1) + "</color>   —   " : "<color=#8EE06A>MAXIMUM SAWMILL LEVEL REACHED</color>");
            if (canUp)
            {
                foreach (var c in mill.NextCost)
                {
                    int have = controller.inventory.Count(c.item);
                    string col = have >= c.amount ? "#9BE061" : "#FF9E7D";
                    info.Append("<color=#FFF3D6>").Append(c.item.DisplayName).Append(":</color> <color=").Append(col).Append(">")
                        .Append(have).Append("/").Append(c.amount).Append("</color>    ");
                }
            }
            Text(upgrade, info.ToString());
        }
        dismantle.gameObject.SetActive(!crafting);
        if (!crafting && structure)
        {
            repair.interactable = !structure.IsDestroyed && structure.CurrentHealth < structure.MaxHealth;
            Text(dismantle, controller.ConfirmDismantle ? "<color=#FF6B5A>CONFIRM DISMANTLE</color>" : "DISMANTLE");
        }

        status.text = controller.Message + (!crafting && structure ? "\nHEALTH " + structure.CurrentHealth + " / " + structure.MaxHealth : "");
        RefreshPurchase();
        if (healthTrack) healthTrack.gameObject.SetActive(!crafting && structure);
        healthBar.gameObject.SetActive(!crafting && structure);
        if (!crafting && structure)
            healthBar.rectTransform.sizeDelta = new Vector2(1054 * structure.NormalizedHealth, 8);
    }

    void OnDisable()
    {
        if (controller)
            controller.Close();
    }
}
