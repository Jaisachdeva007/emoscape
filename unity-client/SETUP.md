# EmoScape Unity client — first-time setup

This project was authored entirely as files (no Unity Editor available in the environment that built it), so a couple of one-time steps that normally happen automatically in the Editor need to be done by hand the first time you open it.

## 1. Open the project

Open `unity-client/` in Unity Hub with **Unity 2022.3 LTS** (any recent patch — the project doesn't depend on an exact patch version). Let it resolve packages from `Packages/manifest.json` on first open; this can take a few minutes (it pulls URP, XR Interaction Toolkit, OpenXR, glTFast, Input System, Newtonsoft Json).

## 2. Create and assign the URP asset (~1 minute)

Deliberately not hand-authored as a file: the internal GUIDs Unity's URP package uses for its pipeline asset classes are only knowable once the package has actually resolved on your machine, and guessing them wrong would produce a silently broken renderer (every runtime-created material in this project uses `Shader.Find("Universal Render Pipeline/Lit")`, which returns null if URP isn't active). Instead:

1. In the Project window, create an `Assets/Settings` folder (if it doesn't already exist), right-click it → **Create → Rendering → URP Asset (with Universal Renderer)**. Name it `URP-HighFidelity`.
2. **Edit → Project Settings → Graphics** → set *Scriptable Render Pipeline Settings* to `URP-HighFidelity`.
3. **Edit → Project Settings → Quality** → for each quality level, set *Render Pipeline Asset* to `URP-HighFidelity`.
4. Optional but recommended: on the `URP-HighFidelity` asset, enable **HDR** (needed for Bloom to read correctly) and on its Renderer asset confirm **Post Processing** is enabled.

Everything else (Bloom, Vignette, fog, the emotional spline, particles, avatar, UI) is built entirely from code at runtime — nothing else needs Editor setup to function.

## 2b. Enable the OpenXR provider (~30 seconds, only needed for Quest/VR)

Same reasoning as the URP asset above — `ProjectSettings/XRGeneralSettingsPerBuildTarget.asset` wasn't hand-authored either, since it also links to package-internal GUIDs. **Edit → Project Settings → XR Plug-in Management** → install if prompted → check **OpenXR** under both the Android and (if you also want desktop testing) Windows/Mac/Linux tabs. Unity will flag any missing OpenXR interaction profile; add the "Meta Quest Support" or generic "Khronos Simple Controller" feature as needed.

Not required to run the flat/desktop build without VR — the app runs fine with no XR provider enabled, `OrbitCameraRig`/`RoomHudBuilder` just check `XRSettings.isDeviceActive` and behave like a normal desktop app.

## 3. Set the backend URL

`Assets/Scripts/Networking/ApiClient.cs` defaults `BaseUrl` to `http://localhost:8000`. If you're testing on a Quest headset against a backend running on your dev machine, change this to your machine's LAN IP (e.g. `http://192.168.1.23:8000`) before building, same as the web app's `window.location.origin` trick — Unity has no equivalent of that, so it's a fixed constant here.

## 4. Run it

Press Play with `Bootstrap.unity` open (or add all three scenes to Build Settings — already done via `ProjectSettings/EditorBuildSettings.asset` — and just make sure `Bootstrap` is index 0). `AppRoot` sets up the persistent API/session singletons and loads the Landscape scene additively.

## Known unverified spots (flagged, not silently hidden)

- **MP3 playback on Quest**: `/tts` streams MP3 via edge-tts; desktop playback via `UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG)` is reliable, but Android/Quest MP3 streaming decode has historically been flakier. If TTS audio doesn't play on-device, the fallback is requesting WAV from the backend instead.
- **glTFast package version drift**: pinned to `6.0.1` in the manifest; if OpenUPM resolves a slightly newer version, double check `GltfImport`/`InstantiateMainSceneAsync` signatures still match (they've been stable across the 5.x/6.x line, but worth a glance).
- **`avatar.glb` bone/animation assumptions**: `AvatarLoader.cs` assumes a bone literally named `"Head"` and reads `GetAnimationClips()[0]` as the idle clip — matches what `room.html` assumed, but wasn't re-verified against the binary GLB contents here.
- **`ProjectVersion.txt` revision hash** is a placeholder (`0000000000000`) — Unity Hub keys off the version number primarily, so this is very unlikely to block opening, but if Hub complains, just pick "open with installed 2022.3.x" when prompted.
