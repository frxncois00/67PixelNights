using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;
    private Vector3 velocity;
    private float cameraZ;

    private void Awake()
    {
        cameraZ = transform.position.z;
        if (!target)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player) target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (!target) return;
        Vector3 destination = new Vector3(target.position.x, target.position.y, cameraZ);
        Vector3 position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime);
        position.z = cameraZ;
        transform.position = position;
    }

    private void OnDisable() => velocity = Vector3.zero;
}
