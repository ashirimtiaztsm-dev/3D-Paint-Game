# Asset Catalog

Full inventory of assets under `Assets/`, organized by folder. Generated 2026-07-24.
`.meta` files are omitted (one exists per listed asset, as usual for Unity).

## `#Paint Game/` — Game-specific content

| Asset | Type | Notes |
|---|---|---|
| `PlayerController.cs` | Script | Joystick-driven `CharacterController` movement, rotation, gravity |
| `CameraFollow.cs` | Script | Follow/look camera + DOTween hit-shake (`PlayHitShake`) |
| `PlayerMaterial.mat` | Material | Player character material |
| `BlackRoad.mat` | Material | Ground/road material |
| `black.jpg` | Texture | Source texture for `BlackRoad.mat` |

### `#Paint Game/Joystick Pack/` — Third-party on-screen joystick asset

| Asset | Type | Notes |
|---|---|---|
| `Documentaion.pdf` | Doc | Asset documentation |
| `Scripts/Base/Joystick.cs` | Script | Base joystick class |
| `Scripts/Joysticks/FixedJoystick.cs` | Script | Fixed-position joystick |
| `Scripts/Joysticks/FloatingJoystick.cs` | Script | Floating joystick (appears at touch point) |
| `Scripts/Joysticks/DynamicJoystick.cs` | Script | Dynamic joystick |
| `Scripts/Joysticks/VariableJoystick.cs` | Script | Variable joystick — used by `PlayerController` |
| `Scripts/Editor/*.cs` (4 files) | Editor script | Custom inspectors for each joystick type |
| `Prefabs/Fixed Joystick.prefab` | Prefab | |
| `Prefabs/Floating Joystick.prefab` | Prefab | |
| `Prefabs/Dynamic Joystick.prefab` | Prefab | |
| `Prefabs/Variable Joystick.prefab` | Prefab | |
| `Sprites/All Axis Backgrounds/*.png` (6) | Sprite | Joystick background art |
| `Sprites/Handles/*.png` (6) | Sprite | Joystick handle art |
| `Sprites/Horizontal Backgrounds/*.png` (6) | Sprite | |
| `Sprites/Vertical Backgrounds/*.png` (6) | Sprite | |
| `Examples/Example Scene.unity` | Scene | Sample scene demoing the joysticks |
| `Examples/JoystickPlayerExample.cs` | Script | Example player controller |
| `Examples/JoystickSetterExample.cs` | Script | Example joystick-type switcher |
| `Examples/Ground.mat`, `Player.mat` | Material | Example scene materials |
| `Examples/Example Scene/LightingData.asset`, `ReflectionProbe-0.exr` | Baked lighting | Example scene bake data |

## `Scenes/`

| Asset | Type | Notes |
|---|---|---|
| `SampleScene.unity` | Scene | Main/only gameplay scene |

## `Settings/` — URP configuration

| Asset | Type | Notes |
|---|---|---|
| `PC_RPAsset.asset` / `PC_Renderer.asset` | URP asset/renderer | Desktop rendering profile |
| `Mobile_RPAsset.asset` / `Mobile_Renderer.asset` | URP asset/renderer | Mobile rendering profile |
| `DefaultVolumeProfile.asset` | Volume Profile | Global post-processing defaults |
| `SampleSceneProfile.asset` | Volume Profile | Post-processing for `SampleScene` |
| `UniversalRenderPipelineGlobalSettings.asset` | URP global settings | |

## `InputSystem_Actions.inputactions`

Default Input System action map asset (installed with the `com.unity.inputsystem` package); not
currently wired into `PlayerController` (movement uses the joystick pack instead).

## `TextMesh Pro/` — TMP runtime resources

| Asset | Type | Notes |
|---|---|---|
| `Fonts/LiberationSans.ttf` (+ OFL license) | Font | Default TMP font source |
| `Resources/Fonts & Materials/LiberationSans SDF*.asset/.mat` | Font asset/material | Default TMP SDF font, outline & drop-shadow materials |
| `Resources/Sprite Assets/EmojiOne.asset` | Sprite asset | Default emoji sprite asset |
| `Resources/Style Sheets/Default Style Sheet.asset` | Style sheet | |
| `Resources/TMP Settings.asset` | Settings | Global TMP settings |
| `Resources/LineBreaking * Characters.txt` | Data | Line-breaking rule sets |
| `Shaders/*.shader`, `*.shadergraph`, `*.cginc`, `SDFFunctions.hlsl` | Shaders | TMP rendering shaders (bitmap, SDF, mobile, HDRP/URP variants) |
| `Sprites/EmojiOne.png` / `.json` / attribution `.txt` | Sprite atlas | Default emoji atlas + metadata |

## `Plugins/Demigiant/` — DOTween & DOTween Pro

| Asset | Type | Notes |
|---|---|---|
| `DemiLib/Core/DemiLib.dll`, `Editor/DemiEditor.dll` (+ XML docs) | Managed DLL | Shared UI library used by DOTween's editor windows |
| `DemiLib/Core/Editor/Imgs/*.png` (~90 files) | Editor icons | Icons for DOTween/DemiLib editor windows |
| `DOTween/DOTween.dll` (+ XML) | Managed DLL | Core tweening engine — used by `CameraFollow.PlayHitShake()` |
| `DOTween/Editor/DOTweenEditor.dll` (+ XML, icons) | Editor DLL | DOTween Utility Panel |
| `DOTween/Modules/*.cs` (7 files) | Script | Optional integration modules (Audio, Physics, Physics2D, Sprite, UI, Utils, version shim) |
| `DOTween/readme.txt` | Doc | |
| `DOTweenPro/DOTweenPro.dll`, `Editor/DOTweenProEditor.dll` (+ XML) | Managed DLL | DOTween Pro extensions + inspector |
| `DOTweenPro/*.cs` (6 files) | Script | Pro shortcuts, DOTweenAnimation component, TextMeshPro/tk2d/audio extensions |
| `DOTweenPro Examples/*.unity` (3 scenes) + `.lighting` | Scene | DOTween Pro demo scenes (Animation Basics/Advanced, Path) |
| `DOTweenPro Examples/Examples Assets/dotweenpro_logo.png` | Texture | |
| `readme_DOTweenPro.txt` | Doc | |

## `Plugins/NuGet/` — Managed dependencies for the Unity MCP plugin

Pulled in to support `com.ivanmurzak.unity.mcp` (AI-agent ↔ Editor bridge). Not game code.

| DLL | Purpose |
|---|---|
| `McpPlugin.dll`, `McpPlugin.Common.dll` | Unity MCP plugin core |
| `ReflectorNet.dll` | Reflection-based object graph library used by the MCP tools |
| `Microsoft.AspNetCore.SignalR.*.dll`, `Microsoft.AspNetCore.Http.Connections.*.dll`, `Microsoft.AspNetCore.Connections.Abstractions.dll` | SignalR client (MCP transport) |
| `Microsoft.CodeAnalysis.dll`, `Microsoft.CodeAnalysis.CSharp.dll` | Roslyn (used by the `script-execute` / `script-update-or-create` MCP tools) |
| `Microsoft.Extensions.*.dll` (Caching, Configuration, DependencyInjection, Diagnostics, Features, FileProviders, Hosting, Logging, Options, Primitives) | .NET generic host / DI plumbing |
| `Microsoft.Bcl.AsyncInterfaces.dll`, `Microsoft.Bcl.Memory.dll`, `Microsoft.Bcl.TimeProvider.dll` | BCL polyfills |
| `System.*.dll` (Buffers, Collections.Immutable, ComponentModel.Annotations, Diagnostics.DiagnosticSource, IO.Pipelines, Memory, Numerics.Vectors, Reflection.Metadata, Runtime.CompilerServices.Unsafe, Text.Encoding.CodePages, Text.Encodings.Web, Text.Json, Threading.Channels, Threading.Tasks.Extensions) | .NET standard library polyfills required by the above |
| `R3.dll` | Reactive Extensions (used internally by the MCP plugin) |
| `.nuget-installed.json` | Bookkeeping file tracking which NuGet packages were installed |

## `TutorialInfo/` — Unity default template assets

Standard boilerplate added by Unity's project templates; not specific to this game and safe to
delete if unused.

| Asset | Type | Notes |
|---|---|---|
| `Icons/URP.png` | Texture | URP template icon |
| `Layout.wlt` | Editor layout | Default window layout |
| `Scripts/Readme.cs` | Script | `Readme` ScriptableObject definition |
| `Scripts/Editor/ReadmeEditor.cs` | Editor script | Custom inspector rendering `Assets/Readme.asset` |
| `Readme.asset` (project root of `Assets/`) | Data | The rendered "Welcome to URP" readme shown by `ReadmeEditor` |

## Summary by File Type

| Extension | Count | Primarily |
|---|---:|---|
| `.png` | 154 | Editor icons (DemiLib/DOTween) + joystick sprites + TMP/URP art |
| `.dll` | 48 | DOTween/DOTween Pro/DemiLib + NuGet deps for the Unity MCP plugin |
| `.cs` | 31 | Gameplay scripts, joystick pack, DOTween modules, editor tooling |
| `.shader` | 14 | TextMesh Pro |
| `.asset` | 14 | URP settings, TMP settings/fonts, lighting data |
| `.txt` | 7 | Readmes/licenses/attribution |
| `.mat` | 6 | Player/road/TMP materials |
| `.unity` | 5 | 1 gameplay scene + 4 example/demo scenes (Joystick Pack, DOTween Pro) |
| `.shadergraph` | 4 | TextMesh Pro (HDRP/URP variants) |
| `.prefab` | 4 | Joystick prefabs |
| `.cginc` | 4 | TextMesh Pro shader includes |
| `.lighting` | 3 | DOTween Pro example scene lightmap settings |
| `.xml` / `.XML` | 6 | DLL doc comments |
| `.json` | 2 | TMP emoji sprite metadata, NuGet install bookkeeping |
| `.jpg` | 2 | Road texture, DOTween header image |
| `.inputactions` | 1 | Input System action map |
| `.hlsl` | 1 | TMP SDF shader functions |
| `.pdf` | 1 | Joystick Pack documentation |
| `.ttf` | 1 | Liberation Sans font |
| `.exr` | 1 | Baked reflection probe (Joystick Pack example scene) |
| `.wlt` | 1 | Editor window layout |
