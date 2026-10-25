using UnityEngine;

// Scene-authored parallax and wind. No procedural bitmap assets are created.
public sealed class DirtmouthAtmosphere : MonoBehaviour
{
    public float parallax;
    public float sway;
    public float drift;
    private Vector3 origin;
    private float phase;
    private Camera sceneCamera;
    private float cameraOrigin;
    private void Start()
    {
        origin = transform.position;
        phase = origin.x * 0.71f + origin.y;
        sceneCamera = Camera.main;
        if (sceneCamera != null) cameraOrigin = sceneCamera.transform.position.x;
    }
    private void LateUpdate()
    {
        float cameraOffset = sceneCamera != null ? (sceneCamera.transform.position.x - cameraOrigin) * parallax : 0f;
        transform.position = origin + new Vector3(cameraOffset + Mathf.Sin(Time.time * 0.12f + phase) * drift, 0f);
        if (sway > 0f) transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.2f + phase) * sway);
    }
}
