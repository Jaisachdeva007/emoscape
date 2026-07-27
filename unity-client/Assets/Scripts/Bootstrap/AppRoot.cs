using System.Collections;
using EmoScape.Networking;
using EmoScape.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmoScape.Bootstrap
{
    /// <summary>
    /// Root of Bootstrap.unity (build index 0). Creates the persistent API/session
    /// singletons and the global post-processing volume, then additively loads the
    /// Landscape scene. Also owns the Landscape&lt;-&gt;Room scene-swap helpers used by
    /// both scene controllers, mirroring the original app's index.html &lt;-&gt; room.html
    /// page navigation while keeping only one particle-heavy scene loaded at a time.
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        public const string LandscapeScene = "Landscape";
        public const string RoomScene = "Room";

        static AppRoot instance;

        void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<ApiClient>();
            gameObject.AddComponent<SessionManager>();
            gameObject.AddComponent<PostProcessingBootstrapper>();
        }

        void Start()
        {
            SceneManager.LoadSceneAsync(LandscapeScene, LoadSceneMode.Additive);
        }

        public static void GoToRoom() => instance.StartCoroutine(instance.SwapScene(LandscapeScene, RoomScene));
        public static void GoToLandscape() => instance.StartCoroutine(instance.SwapScene(RoomScene, LandscapeScene));

        IEnumerator SwapScene(string from, string to)
        {
            yield return SceneManager.LoadSceneAsync(to, LoadSceneMode.Additive);
            yield return SceneManager.UnloadSceneAsync(from);
        }
    }
}
