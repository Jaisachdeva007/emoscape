using UnityEngine;

namespace EmoScape.Landscape
{
    /// <summary>
    /// Desktop mouse-drag orbit camera, direct port of the sph.theta/phi/r rig in
    /// frontend/index.html. Disabled while an XR session is presenting (matches the
    /// original, which never wires orbit-drag into the WebXR camera).
    /// Note: Unity's mouse Y is bottom-up (opposite of browser clientY), so the phi
    /// term's sign is flipped relative to the JS source to preserve "drag down" feel.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCameraRig : MonoBehaviour
    {
        public Vector3 target = Vector3.zero;
        float theta = 0f, phi = 1.1f, radius = 28f;
        bool dragging;
        Vector2 prevMouse;

        public bool IsDragging => dragging;

        void Start() => UpdateCameraPosition();

        void Update()
        {
            if (UnityEngine.XR.XRSettings.isDeviceActive) return;

            if (Input.GetMouseButtonDown(0)) { dragging = true; prevMouse = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0)) dragging = false;

            if (dragging)
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - prevMouse;
                theta -= delta.x * 0.008f;
                phi = Mathf.Clamp(phi + delta.y * 0.008f, 0.3f, Mathf.PI - 0.3f);
                prevMouse = cur;
                UpdateCameraPosition();
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                radius = Mathf.Clamp(radius - scroll * 2.4f, 6f, 80f);
                UpdateCameraPosition();
            }
        }

        void UpdateCameraPosition()
        {
            transform.position = target + new Vector3(
                radius * Mathf.Sin(phi) * Mathf.Sin(theta),
                radius * Mathf.Cos(phi),
                radius * Mathf.Sin(phi) * Mathf.Cos(theta));
            transform.LookAt(target);
        }
    }
}
