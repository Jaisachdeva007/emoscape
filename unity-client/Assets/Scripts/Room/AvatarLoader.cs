using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace EmoScape.Room
{
    /// <summary>
    /// Ports buildAvatar() from frontend/room.html: loads StreamingAssets/avatar.glb at
    /// runtime via glTFast (must go through glTFast's own downloader rather than
    /// File.ReadAllBytes, since StreamingAssets lives inside the APK on Quest/Android),
    /// finds the "Head" bone, and plays the single idle animation clip.
    /// </summary>
    public class AvatarLoader : MonoBehaviour
    {
        public Transform HeadBone { get; private set; }
        public bool IsLoaded { get; private set; }

        public async Task LoadAsync()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "avatar.glb");
            var gltfImport = new GltfImport();
            bool ok = await gltfImport.Load(path);
            if (!ok)
            {
                Debug.LogError("avatar.glb failed to load from StreamingAssets.");
                return;
            }

            var root = new GameObject("Avatar").transform;
            root.SetParent(transform, false);
            await gltfImport.InstantiateMainSceneAsync(root);

            HeadBone = FindRecursive(root, "Head");
            if (HeadBone == null)
                Debug.LogWarning("avatar.glb has no bone named \"Head\" — head tracking will be disabled.");

            var clips = gltfImport.GetAnimationClips();
            if (clips != null && clips.Length > 0)
            {
                var clip = clips[0];
                clip.wrapMode = WrapMode.Loop;
                clip.legacy = true; // legacy Animation component requires this; AnimatorController can't be authored without the Editor
                var anim = root.gameObject.GetComponent<Animation>();
                if (anim == null) anim = root.gameObject.AddComponent<Animation>();
                anim.AddClip(clip, clip.name);
                anim.Play(clip.name);
            }

            IsLoaded = true;
        }

        static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
