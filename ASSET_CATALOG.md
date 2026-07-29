# Asset Catalog

Full inventory of assets under `Assets/`, organized by folder. Updated 2026-07-29 to reflect the
Stage 11 character/blaster/animation integration (see [CHANGELOG.md](CHANGELOG.md)) — new
`ColorGame/Animations/` and `ColorGame/Prefabs/Player/` folders, and the two new vendor packs
(`RPG Tiny Hero Duo/`, `Cosmic_Retro_Blasters Pack_1_FREE/`). `.meta` files are omitted (one exists
per listed asset, as usual for Unity). See [HIERARCHY.md](HIERARCHY.md) for how these scripts
depend on each other at runtime.

## `ColorGame/Scripts/Player/`

| Asset | Type | Notes |
|---|---|---|
| `PlayerMovementController.cs` | Script | `CharacterController`-based movement driven by `MobileInputReader`, projected onto camera-relative forward/right, with acceleration/gravity/visual rotation. Also exposes read-only `HorizontalSpeed`/`NormalizedHorizontalSpeed`/`IsMoving` for animation |
| `PlayerAnimationController.cs` | Script | Drives Animator `MoveSpeed`/`IsSpraying` from `PlayerMovementController`/`PaintGunFireController`; no input reading or gameplay logic of its own |

## `ColorGame/Scripts/Camera/`

| Asset | Type | Notes |
|---|---|---|
| `ThirdPersonOrbitCamera.cs` | Script | Drag-to-look orbit camera; smooths yaw/pitch from `CameraLookInput` and pulls in on wall collisions (`SphereCast`) |

## `ColorGame/Scripts/Input/`

| Asset | Type | Notes |
|---|---|---|
| `MobileInputReader.cs` | Script | Reads joystick/keyboard movement into `MovementInput`; consumed by `PlayerMovementController` |
| `CameraLookInput.cs` | Script | Tracks drag/right-mouse look delta; consumed once per frame by `ThirdPersonOrbitCamera` |
| `MobileControlLayout.cs` | Script | Splits the HUD safe area into joystick/look/action-button regions |
| `HoldActionButton.cs` | Script | Generic press-and-hold UI button; raises `HoldStarted`/`HoldEnded`; used by `ContextualActionUI` |

## `ColorGame/Scripts/Interaction/`

| Asset | Type | Notes |
|---|---|---|
| `InteractionActionType.cs` | Enum | `None, Fill, Fire` — shared action kind |
| `PlayerInteractionZone.cs` | Script | Descriptor placed on tanks/targets (`ActionType`, `IsAvailable`, `PromptText`) |
| `PlayerInteractionDetector.cs` | Script | Finds nearest available zone in range; raises `SelectedZoneChanged` |

## `ColorGame/Scripts/Paint/`

| Asset | Type | Notes |
|---|---|---|
| `PaintColorId.cs` | Enum | `None, Red, Blue, Yellow, Green, Purple, Orange` — canonical color identity |
| `PaintColorDefinition.cs` | ScriptableObject script | Designer-authored color description (`ColorId`, `DisplayName`, `DisplayColor`) |
| `PaintTargetDefinition.cs` | ScriptableObject script | Designer-authored target with a list of `Region`s (mask, required paint, completion threshold) |
| `PaintGunReservoir.cs` | Script | Owns carried paint state; raises `PaintColorChanged`/`AmountChanged` |
| `PaintGunVisual.cs` | Script | Tints/scales the gun's visual mesh from reservoir state |
| `PaintTank.cs` | Script | Fillable paint source; `TakePaint` transfers to the reservoir |
| `PaintFillController.cs` | Script | Hold-to-fill loop between a `PaintTank` and the reservoir |
| `PaintGunFireController.cs` | Script | Hold-to-fire loop; consumes paint and drives `PaintSprayer` |
| `PaintSprayer.cs` | Script | Performs the spray raycast and drives spray/impact particle systems |
| `PaintSprayHit.cs` | Struct | Immutable data for one successful spray sample |
| `PaintSurfaceMarker.cs` | Script | Marks a collider as spray-target; gates and forwards accepted hits |
| `PaintableSurface.cs` | Script | GPU paint accumulation via ping-ponged render textures + `PaintBrush.shader` blits |
| `PaintStampData.cs` | Struct | Immutable data for one applied brush stamp |
| `PaintTargetMaskProvider.cs` | Script | Builds per-color "allowed to paint here" masks from the active `PaintTargetDefinition` |
| `PaintCoverageTracker.cs` | Script | Tracks correct-color coverage per region on a CPU grid; raises progress/completion events |
| `PaintRegionProgress.cs` | Struct | Snapshot of one region's runtime progress, for UI consumption |
| `PaintProgressUI.cs` | Script | Overall + per-region progress bars, built from tracker events |

## `ColorGame/Scripts/UI/`

| Asset | Type | Notes |
|---|---|---|
| `ContextualActionUI.cs` | Script | Shows the Fill/Fire button based on the detector's selected zone |
| `PaintReservoirUI.cs` | Script | HUD readout bound to `PaintGunReservoir` events |
| `PaintRegionProgressRow.cs` | Script | One reusable progress row, instantiated per target region by `PaintProgressUI` |
| `LevelCompleteController.cs` | Script | Stage 10. On `PaintCoverageTracker` on the `Canvas` GameObject (must stay active while the panel it controls starts hidden); freezes gameplay input and shows `LevelCompletePanel` on `Completed` |

## `ColorGame/Animations/` — Stage 11 project-owned animation assets

| Asset | Type | Notes |
|---|---|---|
| `Controllers/MaleCharacterPlayer.controller` | Animator Controller | Base Layer 1D blend tree (`MoveSpeed`: Idle/Move) + `RightArmSpray` masked layer (`IsSpraying`: Empty/SprayDefend). Parameters: `MoveSpeed` (float), `IsSpraying` (bool) |
| `Masks/RightArmSpray.mask` | Avatar Mask | Humanoid body parts: `RightArm` + `RightFingers` only — isolates the spray pose to the right arm |
| `Clips/` | — | Empty — the three vendor clips used (see below) already loop correctly and needed no project-owned copies |

## `ColorGame/Prefabs/Player/`

| Asset | Type | Notes |
|---|---|---|
| `MaleCharacterPlayer.prefab` | Prefab Variant (of `MaleCharacterPBR.prefab`) | Stage 11. The configured player visual: `weapon_l`/`weapon_r` deactivated, `RightHandWeaponSocket` under `hand_r` holding `PaintGun`/`Cosmic_Retro_Blaster_1`, `Animator` assigned `MaleCharacterPlayer.controller` with `applyRootMotion` disabled |

## `ColorGame/Shaders/`

| Asset | Type | Notes |
|---|---|---|
| `PaintBrush.shader` | Shader | Blit shader used by `PaintableSurface` to stamp a soft-edged circular brush, masked by `PaintTargetMaskProvider` |
| `PaintableSurface.shader` | Shader | URP unlit surface shader; lerps base color to the accumulated paint texture |

## `ColorGame/ScriptableObjects/PaintColors/`

| Asset | Type | Notes |
|---|---|---|
| `RedPaint.asset`, `BluePaint.asset`, `YellowPaint.asset` | ScriptableObject (`PaintColorDefinition`) | The three currently-defined paint colors |

## `ColorGame/ScriptableObjects/Targets/`

| Asset | Type | Notes |
|---|---|---|
| `HeartTarget_Red.asset` | ScriptableObject (`PaintTargetDefinition`) | Single region — heart shape, Red |
| `HeartTarget_RedBlue.asset` | ScriptableObject (`PaintTargetDefinition`) | Two regions — heart split Red/Blue, each with its own mask |

## `ColorGame/Materials/`

| Asset | Type | Notes |
|---|---|---|
| `BlackRoad.mat` | Material | Ground/road material |
| `PaintableSurface_Test.mat` | Material | Test material using `PaintableSurface.shader` |
| `black.jpg` | Texture | Source texture for `BlackRoad.mat` |

## `ColorGame/TestAssets/`

| Asset | Type | Notes |
|---|---|---|
| `HeartMask_Test.png` | Texture | Full-heart mask used by `HeartTarget_Red` |
| `HeartLeftMask_Test.png`, `HeartRightMask_Test.png` | Texture | Split-heart masks used by `HeartTarget_RedBlue`'s two regions |

## `ColorGame/Joystick Pack/` — Third-party on-screen joystick asset

| Asset | Type | Notes |
|---|---|---|
| `Documentaion.pdf` | Doc | Asset documentation |
| `Scripts/Base/Joystick.cs` | Script | Base joystick class |
| `Scripts/Joysticks/FixedJoystick.cs` | Script | Fixed-position joystick |
| `Scripts/Joysticks/FloatingJoystick.cs` | Script | Floating joystick (appears at touch point) |
| `Scripts/Joysticks/DynamicJoystick.cs` | Script | Dynamic joystick |
| `Scripts/Joysticks/VariableJoystick.cs` | Script | Variable joystick — used by `MobileInputReader` |
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

## `RPG Tiny Hero Duo/` — Third-party Humanoid character pack (Stage 11)

| Asset | Type | Notes |
|---|---|---|
| `Prefab/MaleCharacterPBR.prefab` | Prefab (Humanoid, valid Avatar) | Source character for the player visual — never modified; the player uses a Prefab Variant (see `ColorGame/Prefabs/Player/`) |
| `Prefab/FemaleCharacterPBR.prefab` | Prefab | Unused sibling character, shipped with the pack |
| `Animation/SwordAndShield/Idle_Normal_SwordAndShield.fbx` | Animation (Humanoid, loops) | Used as the Base Layer's idle blend-tree clip |
| `Animation/SwordAndShield/InPlace/MoveFWD_Normal_InPlace_SwordAndShield.fbx` | Animation (Humanoid, loops, in-place) | Used as the Base Layer's move blend-tree clip |
| `Animation/SwordAndShield/Defend_SwordAndShield.fbx` | Animation (Humanoid, loops) | Used (mirrored) as the `RightArmSpray` layer's `SprayDefend` state |
| `Animation/SwordAndShield/*.fbx` (~30 more clips) | Animation | Vendor combat/movement clip set (attacks, hits, death, jumps, etc.) — not used by this stage |
| `Animator/SwordAndShieldStance.controller`, `AnimationLayer.controller`, `RootMotion.controller` | Animator Controller | Vendor demo controllers, still assigned to `MaleCharacterPBR.prefab`/`FemaleCharacterPBR.prefab` themselves; the player uses its own `MaleCharacterPlayer.controller` instead |
| *(textures, materials, additional meshes)* | Various | Full character pack contents; not individually catalogued here |

## `Cosmic_Retro_Blasters Pack_1_FREE/` — Third-party weapon model pack (Stage 11)

| Asset | Type | Notes |
|---|---|---|
| `Prefabs/Cosmic_Retro_Blaster_1.prefab` | Prefab | The blaster model instantiated inside `PaintGun` as the player's visible weapon — never modified |
| `Prefabs/Cosmic_Retro_Blaster_10.prefab`, `Cosmic_Retro_Blaster_11.prefab` | Prefab | Other pack variants — unused |
| `Models/*.fbx` (3 blasters) | Model | Source meshes for the prefabs above |
| `Materials/*.mat`, `Textures/*.png` | Material/Texture | Blaster surface materials and textures |
| `Demo_Scene/SceneDemo_Cosmic_Retro_Blaster_1_FREE.unity` | Scene | Vendor demo scene — not part of the game's build |

## `Scenes/`

| Asset | Type | Notes |
|---|---|---|
| `SampleScene.unity` | Scene | Main/only gameplay scene. Its `Canvas` includes a `LevelCompletePanel` (hidden by default) with `PanelBackground`, `TitleText`, and a `Buttons` container holding `ReplayButton`/`NextLevelButton` (each with a TMP `Label` child). Its `Player/PlayerVisualRoot` now holds a `MaleCharacterPlayer` prefab-variant instance in place of the old placeholder `CharacterVisual` (deactivated) — see [HIERARCHY.md](HIERARCHY.md) for the full tree |

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
currently wired into gameplay (movement/look use the joystick pack + custom drag input instead).

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
| `DOTween/DOTween.dll` (+ XML) | Managed DLL | Core tweening engine |
| `DOTween/Editor/DOTweenEditor.dll` (+ XML, icons) | Editor DLL | DOTween Utility Panel |
| `DOTween/Modules/*.cs` (7 files) | Script | Optional integration modules (Audio, Physics, Physics2D, Sprite, UI, Utils, version shim) |
| `DOTween/readme.txt` | Doc | |
| `DOTweenPro/DOTweenPro.dll`, `Editor/DOTweenProEditor.dll` (+ XML) | Managed DLL | DOTween Pro extensions + inspector |
| `DOTweenPro/*.cs` (6 files) | Script | Pro shortcuts, DOTweenAnimation component, TextMeshPro/tk2d/audio extensions |
| `DOTweenPro Examples/*.unity` (3 scenes) + `.lighting` | Scene | DOTween Pro demo scenes (Animation Basics/Advanced, Path) |
| `DOTweenPro Examples/Examples Assets/dotweenpro_logo.png` | Texture | |
| `readme_DOTweenPro.txt` | Doc | |

> Note: DOTween is currently unused by gameplay code — the earlier `CameraFollow.PlayHitShake()`
> prototype that used it was removed (see [CHANGELOG.md](CHANGELOG.md), 2026-07-24 controller/camera
> rewrite).

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

Counts below cover `ColorGame/`, `Scenes/`, `Settings/`, `TextMesh Pro/`, `Plugins/`, `TutorialInfo/`,
and the project-authored parts of the two Stage 11 vendor packs; the vendor packs' own bulk content
(dozens of clips/materials/textures) is summarized rather than counted file-by-file.

| Extension | Count | Primarily |
|---|---:|---|
| `.png` | 157+ | Editor icons (DemiLib/DOTween) + joystick sprites + TMP/URP art + heart mask test textures (excludes `RPG Tiny Hero Duo`/`Cosmic_Retro_Blasters` textures) |
| `.cs` | 60 | Gameplay scripts (30 in `ColorGame/Scripts/`, incl. `PlayerAnimationController.cs`), joystick pack, DOTween modules, editor tooling |
| `.dll` | 48 | DOTween/DOTween Pro/DemiLib + NuGet deps for the Unity MCP plugin |
| `.asset` | 19 | URP settings, TMP settings/fonts, lighting data, 5 paint ScriptableObject instances |
| `.shader` | 16 | TextMesh Pro (14) + `PaintBrush`/`PaintableSurface` (2) |
| `.txt` | 7 | Readmes/licenses/attribution |
| `.mat` | 6 | Player/road/TMP materials + `PaintableSurface_Test.mat` (excludes vendor pack materials) |
| `.unity` | 6 | 1 gameplay scene + 4 example/demo scenes (Joystick Pack, DOTween Pro) + 1 Cosmic Retro Blasters demo scene |
| `.prefab` | 5 | Joystick prefabs (4) + `MaleCharacterPlayer.prefab` (excludes vendor pack prefabs) |
| `.shadergraph` | 4 | TextMesh Pro (HDRP/URP variants) |
| `.cginc` | 4 | TextMesh Pro shader includes |
| `.XML` / `.xml` | 6 | DLL doc comments |
| `.controller` | 1 | `MaleCharacterPlayer.controller` (excludes the 3 vendor `RPG Tiny Hero Duo` demo controllers) |
| `.mask` | 1 | `RightArmSpray.mask` |
| `.lighting` | 3 | DOTween Pro example scene lightmap settings |
| `.json` | 2 | TMP emoji sprite metadata, NuGet install bookkeeping |
| `.jpg` | 2 | Road texture, DOTween header image |
| `.inputactions` | 1 | Input System action map |
| `.hlsl` | 1 | TMP SDF shader functions |
| `.pdf` | 1 | Joystick Pack documentation |
| `.ttf` | 1 | Liberation Sans font |
| `.exr` | 1 | Baked reflection probe (Joystick Pack example scene) |
| `.wlt` | 1 | Editor window layout |
| `.fbx` | 30+ | `RPG Tiny Hero Duo` animation clips (3 used directly; rest unused) — model FBX counted separately per pack |
