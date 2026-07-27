using EmoScape.Networking;
using UnityEngine;

namespace EmoScape.Landscape
{
    /// <summary>Tags a spline node GameObject with its session data, read by NodeTooltipController on raycast hit.</summary>
    public class SessionNodeRef : MonoBehaviour
    {
        public SessionDto Session;
    }
}
