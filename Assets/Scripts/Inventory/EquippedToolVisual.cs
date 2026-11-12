using UnityEngine;

// Neutral walking frames plus the actual inventory item avoid a baked-in axe.
public class EquippedToolVisual : MonoBehaviour
{
    public EquipmentController equipment;
    public PlayerGatherAnimation actions;
    public SpriteRenderer character;
    public SpriteRenderer heldTool;

    public Sprite[] axeIdle, axeRun, pickaxeIdle, pickaxeRun, swordIdle, swordRun;
    Rigidbody2D body;
    SawmillWeaponAnimations sawmillAnimations;
    PlayerMovement movement;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sawmillAnimations = GetComponent<SawmillWeaponAnimations>();
        movement = GetComponent<PlayerMovement>();
    }
    void LateUpdate()
    {
        if (!heldTool || !character || !equipment) return;
        var item = equipment.Equipped;
        heldTool.enabled = item && item.Equippable && item.Icon &&
            !(actions && actions.Busy) && !PlayerSurvivalController.InputLocked;
        if (!heldTool.enabled) return;
        bool moving = body && body.linearVelocity.sqrMagnitude > .01f;
        if (sawmillAnimations && movement && sawmillAnimations.ShowHeld(item, movement.Facing, moving, character))
        {
            heldTool.enabled = false;
            return;
        }
        Sprite[] frames = item.Id == "basic_axe" ? (moving ? axeRun : axeIdle) :
            item.Id == "basic_pickaxe" ? (moving ? pickaxeRun : pickaxeIdle) :
            item.Id == "basic_sword" ? (moving ? swordRun : swordIdle) : null;
        if (frames != null && frames.Length > 0) {
            character.sprite = frames[(int)(Time.time * 10) % frames.Length];
            heldTool.enabled = false; // The matching native frame already holds the tool.
            return;
        }
        heldTool.sprite = item.Icon;
        heldTool.color = item.IconTint;
        heldTool.sortingLayerID = character.sortingLayerID;
        heldTool.sortingOrder = character.sortingOrder + 1;
        if (GetComponent<BodyAActionFrames>())
        {
            Vector2 facing = GetComponent<PlayerMovement>().Facing;
            heldTool.transform.position = transform.position + new Vector3(facing.x < 0 ? -.18f : .18f, .3f, 0);
            heldTool.transform.rotation = Quaternion.Euler(0, 0, item.Id == "basic_pickaxe" ? (facing.x < 0 ? 90 : -90) : 0);
            // Match the attack's world size, with a modest 12% increase while held.
            float heldScale = item.craftedWeapon
                ? WeaponSwing.WeaponLength(item) * 1.12f / Mathf.Max(.01f, item.Icon.bounds.size.y)
                : 1f;
            heldTool.transform.localScale = Vector3.one * heldScale;
            if (item.craftedWeapon && item.rangedWeapon)
                heldTool.transform.position -= (Vector3)item.Icon.bounds.center * heldScale;
            // The native axe blade points left in its source sprite.
            heldTool.flipX = item.Id == "basic_axe" ? facing.x >= 0 : facing.x < 0;
            heldTool.sortingOrder = character.sortingOrder + (facing.y > .7f ? -1 : 1);
            return;
        }
        var hand = transform.position + new Vector3(character.flipX ? -.25f : .25f, .48f, 0);
        float angle = item.Id == "basic_axe" ? 180 : item.Id == "basic_pickaxe" ? -45 : 0;
        ToolGripPose.Place(heldTool, item.Id, hand, character.flipX ? -angle : angle, character.flipX);
    }

    void OnDisable() { if (heldTool) heldTool.enabled = false; }
}
