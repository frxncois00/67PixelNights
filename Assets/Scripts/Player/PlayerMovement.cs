using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0)] private float movementSpeed = 4f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visual;
    private Rigidbody2D body;
    private Vector2 movement;
    private Vector2 facing = Vector2.down;
    public Vector2 Facing { get; private set; } = Vector2.down;
    public int CurrentElevationLevel { get; private set; } = 0;
    private string currentAnimation;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!visual) visual = GetComponentInChildren<SpriteRenderer>();
        if (visual)
        {
            if (visual.transform != transform)
                visual.transform.localPosition = Vector3.zero;
            visual.sortingLayerName = "Objects";
            visual.sortingOrder = 0;
            visual.spriteSortPoint = SpriteSortPoint.Pivot;
        }
        UpdateElevationLevel();
    }

    private void UpdateElevationLevel()
    {
        Vector2 pos = transform.position;
        bool onElevatedTemple = pos.x >= -23.4f && pos.x <= -11.6f && pos.y >= 13.8f && pos.y <= 18.6f;
        bool onUpperWaterfallCliff = pos.x >= -33.5f && pos.x <= -9.5f && pos.y >= 19.6f && pos.y <= 26.5f;
        CurrentElevationLevel = (onElevatedTemple || onUpperWaterfallCliff) ? 1 : 0;
    }

    private void Update()
    {
        UpdateElevationLevel();
        var keyboard = Keyboard.current;
        movement = Vector2.zero;
        if (keyboard != null && !TradingUI.IsOpen && !BuildingController.ModalOpen && !InventoryUI.IsOpen && !PlayerSurvivalController.InputLocked)
        {
            movement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            movement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                       - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            movement = Vector2.ClampMagnitude(movement, 1f);
            if (movement != Vector2.zero) Facing = movement.normalized;
        }
        if (movement != Vector2.zero)
            facing = Mathf.Abs(movement.x) >= Mathf.Abs(movement.y)
                ? new Vector2(Mathf.Sign(movement.x), 0) : new Vector2(0, Mathf.Sign(movement.y));

        if (!animator) return;
        bool moving = movement != Vector2.zero;
        animator.SetBool("IsMoving", moving);
        animator.SetFloat("MoveX", movement.x);
        animator.SetFloat("MoveY", movement.y);
        animator.SetFloat("LastMoveX", facing.x);
        animator.SetFloat("LastMoveY", facing.y);
        // This pack only supplies side-facing frames. Vertical states reuse them.
        if (visual) visual.flipX = facing.x < 0;
        string direction = facing.x < 0 ? "Left" : facing.x > 0 ? "Right" : facing.y > 0 ? "Up" : "Down";
        string nextAnimation = (moving ? "Walk_" : "Idle_") + direction;
        if (nextAnimation != currentAnimation)
        {
            animator.Play(nextAnimation);
            currentAnimation = nextAnimation;
        }
    }

    private void FixedUpdate() => body.linearVelocity = TradingUI.IsOpen || BuildingController.ModalOpen || InventoryUI.IsOpen || PlayerSurvivalController.InputLocked ? Vector2.zero : movement * movementSpeed;

    private void OnDisable()
    {
        movement = Vector2.zero;
        if (body) body.linearVelocity = Vector2.zero;
        currentAnimation = null;
    }
}
