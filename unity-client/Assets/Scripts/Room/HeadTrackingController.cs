using UnityEngine;

namespace EmoScape.Room
{
    /// <summary>Direct port of the head-tracking lerp in frontend/room.html's animate() loop.</summary>
    public class HeadTrackingController : MonoBehaviour
    {
        public Transform HeadBone;
        public Transform CameraTransform;
        float t;

        void Update()
        {
            if (HeadBone == null || CameraTransform == null) return;
            t += Time.deltaTime;

            Vector3 headPos = HeadBone.position;
            Vector3 d = CameraTransform.position - headPos;
            float dist = d.magnitude;
            if (dist < 0.0001f) return;

            float targetY = Mathf.Atan2(d.x, d.z) * 0.55f + Mathf.Sin(t * 0.32f) * 0.018f;
            float targetX = -Mathf.Asin(Mathf.Clamp(d.y / dist, -1f, 1f)) * 0.55f + Mathf.Sin(t * 0.26f) * 0.012f;

            var e = HeadBone.localEulerAngles;
            float curY = NormalizeAngle(e.y);
            float curX = NormalizeAngle(e.x);
            float newY = curY + (targetY * Mathf.Rad2Deg - curY) * 0.04f;
            float newX = curX + (targetX * Mathf.Rad2Deg - curX) * 0.04f;
            HeadBone.localEulerAngles = new Vector3(newX, newY, e.z);
        }

        static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }
    }
}
