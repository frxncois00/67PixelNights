using UnityEngine;

// Tiny self-destroying pixel particle used by WeaponSwing impact effects.
public class WeaponFxParticle : MonoBehaviour
{
    public Vector2 velocity;
    public float gravity, life = .3f;
    float age;
    SpriteRenderer r;
    Color color;
    Vector3 startScale;

    void Start()
    {
        r = GetComponent<SpriteRenderer>();
        color = r.color;
        startScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / Mathf.Max(.01f, life);
        if (k >= 1) { Destroy(gameObject); return; }
        velocity.y -= gravity * Time.deltaTime;
        velocity *= Mathf.Max(0, 1 - 4 * Time.deltaTime);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        r.color = new Color(color.r, color.g, color.b, color.a * (1 - k));
        transform.localScale = startScale * (1 - k * .5f);
    }
}
