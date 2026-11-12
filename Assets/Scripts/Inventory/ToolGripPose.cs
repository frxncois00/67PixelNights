using UnityEngine;

// Grip locations in the existing 64px Tiny Swords icons, relative to their centres.
// Position the handle at the hand; rotating a tool must not rotate it away from it.
public static class ToolGripPose
{
    public static Vector3 Grip(string id) => id == "basic_pickaxe"
        ? new Vector3(6f / 64, -5f / 64, 0)
        : id == "basic_axe" ? new Vector3(5f / 64, 6f / 64, 0)
        : new Vector3(-4f / 64, -4f / 64, 0);

    public static float HeadAngle(string id) => id == "basic_pickaxe" ? 135 : id == "basic_axe" ? -135 : 45;

    public static void Place(SpriteRenderer renderer, string id, Vector3 hand, float angle, bool flip = false)
    {
        Vector3 grip = Grip(id);
        if (flip) grip.x = -grip.x;
        renderer.flipX = flip;
        renderer.transform.rotation = Quaternion.Euler(0, 0, angle);
        renderer.transform.localScale = Vector3.one;
        renderer.transform.position = hand - renderer.transform.rotation * grip;
    }
}
