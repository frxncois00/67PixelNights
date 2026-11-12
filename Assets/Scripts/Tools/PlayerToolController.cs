using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerToolController : MonoBehaviour {
    public ResourceNode[] nodes;
    public PlayerInteractionController interaction;
    [Min(1)] public int damage=1;
    [Min(.1f)] public float range=1.5f, cooldown=.5f;
    public RectTransform[] hudPanels;
    float nextSwing;
    PlayerMovement movement;
    EquipmentController equipment;
    void Awake() { movement=GetComponent<PlayerMovement>(); equipment=GetComponent<EquipmentController>(); }
    void LateUpdate() {
        if (PlayerInteractionController.InputBlocked) return;
        bool key = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
            !(EventSystem.current && EventSystem.current.IsPointerOverGameObject());
        if (key || click) {
            if (equipment && equipment.SwordEquipped) GetComponent<PlayerMeleeCombat>()?.Attack();
            else Swing(ResourceType.Wood);
        }
    }
    public ResourceNode FindTarget(ResourceType type) {
        if (!movement || !isActiveAndEnabled || nodes == null || !equipment || !equipment.CanHarvest(type)) return null;
        ResourceNode best=null;
        float nearest=range*range;
        foreach(var node in nodes) {
            if(!node||!node.isActiveAndEnabled||node.Depleted||!node.acceptsAxe||node.type!=type)continue;
            var delta=node.HitPoint-(Vector2)transform.position;
            if(delta.sqrMagnitude>nearest||Vector2.Dot(delta.normalized,movement.Facing)<.707f)continue;
            nearest=delta.sqrMagnitude;best=node;
        }
        return best;
    }

    public bool Swing(ResourceType type) {
        if (!interaction || !interaction.actions || interaction.actions.Busy ||
            Time.time < nextSwing || PlayerInteractionController.InputBlocked) return false;
        if (!equipment || !equipment.CanHarvest(type)) {
            interaction.Notice(type == ResourceType.Wood ? "EQUIP AN AXE IN YOUR HOTBAR TO CHOP TREES" : "EQUIP A PICKAXE IN YOUR HOTBAR TO MINE");
            return false;
        }
        if (type == ResourceType.Wood && TryRemoveFence()) return true;
        var best = FindTarget(type);
        if (!best) {
            interaction.Notice(type == ResourceType.Wood ? "MOVE CLOSER AND FACE THE TREE TRUNK" : "MOVE CLOSER AND FACE A MINEABLE ROCK");
            return false;
        }
        nextSwing=Time.time+Mathf.Max(.1f,cooldown);
        Vector2 target=best.HitPoint;
        return interaction.actions.Play(true,target,()=>{
            if(best && Vector2.Distance(transform.position,best.HitPoint)<=range)best.Hit(damage,interaction);
        }, mining: best && best.type == ResourceType.Stone, equippedTool: equipment.Equipped);
    }
    bool TryRemoveFence()
    {
        PlaceableStructure best=null;float nearest=range;
        foreach(var c in Physics2D.OverlapCircleAll(transform.position,range)) {
            var s=c.GetComponent<PlaceableStructure>();
            if(!s||!s.definition||!s.definition.item||s.definition.item.Id!="wooden_fence"||s.IsDestroyed)continue;
            Vector2 point=c.ClosestPoint(transform.position), delta=point-(Vector2)transform.position;
            if(delta.magnitude>nearest||Vector2.Dot(delta.normalized,movement.Facing)<.5f)continue;
            best=s;nearest=delta.magnitude;
        }
        if(!best)return false;
        nextSwing=Time.time+cooldown;
        var target=best;
        return interaction.actions.Play(true,best.transform.position,()=>{
            if(!target||!equipment.AxeEquipped)return;
            var c=target.GetComponent<Collider2D>();
            if(c&&Vector2.Distance(transform.position,c.ClosestPoint(transform.position))<=range){target.Dismantle();interaction.Notice("FENCE REMOVED");}
        },equippedTool:equipment.Equipped);
    }
}
