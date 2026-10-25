using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class DirtmouthCamera : MonoBehaviour
{
    public Transform target;
    [Tooltip("Camera movement boundary. The Dirtmouth cameraColl polygon is rectangular.")]
    public Collider2D cameraColl;
    [Min(0)] public float smoothTime = 0.10f;
    // Retained for compatibility with the original scene-building editor tools.
    [HideInInspector]
    public float minX = -45f, maxX = 66f;
    [HideInInspector]
    public float height = 4.3f;
    [HideInInspector]
    public bool frameCentralSquare;
    private Vector2 velocity;
    private Camera view;
    private float authoredSize;
    private void Awake()
    {
        view = GetComponent<Camera>();
        authoredSize = view.orthographicSize;
        ResolveBoundary();
    }
    private void Start() => SnapToTarget();
    private void ResolveBoundary()
    {
        if (cameraColl != null) return;
        foreach (var root in gameObject.scene.GetRootGameObjects())
        foreach (var collider in root.GetComponentsInChildren<Collider2D>())
            if (collider.name == "cameraColl") { cameraColl = collider; return; }
    }
    private Vector2 Confine(Vector2 desired)
    {
        if (view == null) { view = GetComponent<Camera>(); authoredSize = view.orthographicSize; }
        if (cameraColl == null || !cameraColl.enabled || !cameraColl.gameObject.activeInHierarchy) return desired;
        Bounds bounds = cameraColl.bounds;
        // Account for the whole viewport, rather than clamping just the camera's centre.
        // If the boundary is smaller than the view, fit it without showing outside the boundary.
        float aspect = Mathf.Max(0.01f, view.aspect);
        view.orthographicSize = Mathf.Max(0.01f, Mathf.Min(authoredSize, bounds.extents.y, bounds.extents.x / aspect));
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * aspect;
        return new Vector2(
            Mathf.Clamp(desired.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth),
            Mathf.Clamp(desired.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight));
    }
    public void SnapToTarget()
    {
        if (target == null) return;
        ResolveBoundary();
        Vector2 position = Confine(target.position);
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        velocity = Vector2.zero;
    }
    private void LateUpdate()
    {
        if (target == null) return;
        Vector2 desired = Confine(target.position);
        Vector2 position = smoothTime <= 0 ? desired : Vector2.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        // Reconfine after smoothing, including when cameraColl moves or the aspect ratio changes.
        position = Confine(position);
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}
