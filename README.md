# 3D Paint Game (ColorGame)

A mobile-friendly 3D game built in Unity where the player walks a character around an arena,
fills a paint gun from tanks, and sprays a paintable surface to fully cover one or more target
regions with the correct color (e.g. a heart shape split into red/blue halves). Movement is driven
by an on-screen joystick, look is drag-to-orbit, and painting is GPU-based (render-texture blits)
with progress tracked live on a HUD.

## Tech Stack

| Area | Choice |
|---|---|
| Engine | Unity 6000.x (Unity 6) |
| Render pipeline | Universal Render Pipeline (URP) 17.1.0, with separate PC and Mobile render assets |
| Input | Player movement via a virtual joystick ([Joystick Pack](Assets/ColorGame/Joystick%20Pack)) read by `MobileInputReader`; camera look via drag/right-mouse (`CameraLookInput`); Unity Input System package also installed with `InputSystem_Actions.inputactions` (not currently wired into gameplay) |
| Painting | Custom URP shaders ([PaintBrush.shader](Assets/ColorGame/Shaders/PaintBrush.shader), [PaintableSurface.shader](Assets/ColorGame/Shaders/PaintableSurface.shader)) — permanent paint accumulated into a render texture, masked per target region |
| Tweening / FX | [DOTween / DOTween Pro](Assets/Plugins/Demigiant) (installed, currently unused by gameplay code) |
| UI Text | TextMesh Pro |
| Navigation | AI Navigation package (NavMesh) |
| Editor tooling | Unity MCP plugin (`com.ivanmurzak.unity.mcp`) — lets an AI agent drive the Unity Editor directly (see `.mcp.json`) |

## Project Structure

```
Assets/
├── ColorGame/               Game-specific code and content
│   ├── Scripts/
│   │   ├── Player/           PlayerMovementController
│   │   ├── Camera/            ThirdPersonOrbitCamera
│   │   ├── Input/              MobileInputReader, CameraLookInput, MobileControlLayout, HoldActionButton
│   │   ├── Interaction/        PlayerInteractionZone/Detector, InteractionActionType
│   │   ├── Paint/               Paint gun, tanks, surfaces, coverage tracking (17 scripts)
│   │   └── UI/                   ContextualActionUI, PaintReservoirUI, PaintRegionProgressRow
│   ├── Shaders/               PaintBrush.shader, PaintableSurface.shader
│   ├── ScriptableObjects/
│   │   ├── PaintColors/         RedPaint, BluePaint, YellowPaint
│   │   └── Targets/              HeartTarget_Red, HeartTarget_RedBlue
│   ├── Materials/              BlackRoad.mat, PaintableSurface_Test.mat, black.jpg
│   ├── TestAssets/              Heart mask textures used by the two heart targets
│   └── Joystick Pack/           Third-party on-screen joystick asset (Fixed/Floating/Dynamic/Variable joysticks)
├── Scenes/
│   └── SampleScene.unity     Main/only scene
├── Settings/                  URP render pipeline assets & volume profiles (PC + Mobile variants)
├── TextMesh Pro/               TMP runtime resources (fonts, shaders, default sprite asset)
├── Plugins/
│   ├── Demigiant/               DOTween & DOTween Pro
│   └── NuGet/                    Managed DLLs pulled in for the Unity MCP plugin (SignalR client, Roslyn, etc.)
├── TutorialInfo/                 Unity's default "Readme" template assets (safe to remove)
└── InputSystem_Actions.inputactions
```

See [ASSET_CATALOG.md](ASSET_CATALOG.md) for a full inventory of every asset in the project,
[HIERARCHY.md](HIERARCHY.md) for how the gameplay scripts depend on each other at runtime, and
[CHANGELOG.md](CHANGELOG.md) for the project's history.

## Core Gameplay Scripts

- **[PlayerMovementController.cs](Assets/ColorGame/Scripts/Player/PlayerMovementController.cs)** —
  `CharacterController`-based movement driven by `MobileInputReader`, projected onto camera-relative
  forward/right, with acceleration, gravity, and visual rotation.
- **[ThirdPersonOrbitCamera.cs](Assets/ColorGame/Scripts/Camera/ThirdPersonOrbitCamera.cs)** —
  drag-to-look orbit camera that smooths yaw/pitch and pulls in on wall collisions.
- **[PaintGunReservoir.cs](Assets/ColorGame/Scripts/Paint/PaintGunReservoir.cs)** /
  **[PaintFillController.cs](Assets/ColorGame/Scripts/Paint/PaintFillController.cs)** — hold-to-fill
  loop that transfers paint from a `PaintTank` into the player's carried reservoir.
- **[PaintGunFireController.cs](Assets/ColorGame/Scripts/Paint/PaintGunFireController.cs)** /
  **[PaintSprayer.cs](Assets/ColorGame/Scripts/Paint/PaintSprayer.cs)** — hold-to-fire loop that
  consumes paint and raycasts sprays onto valid surfaces.
- **[PaintableSurface.cs](Assets/ColorGame/Scripts/Paint/PaintableSurface.cs)** — permanent GPU
  paint accumulation via ping-ponged render textures and `PaintBrush.shader` blits, masked by
  **[PaintTargetMaskProvider.cs](Assets/ColorGame/Scripts/Paint/PaintTargetMaskProvider.cs)**.
- **[PaintCoverageTracker.cs](Assets/ColorGame/Scripts/Paint/PaintCoverageTracker.cs)** — tracks
  correct-color coverage per target region and drives the progress HUD
  (**[PaintProgressUI.cs](Assets/ColorGame/Scripts/Paint/PaintProgressUI.cs)**), and raises
  `Completed` once every required region reaches its threshold.
- **[LevelCompleteController.cs](Assets/ColorGame/Scripts/UI/LevelCompleteController.cs)** — on
  `PaintCoverageTracker.Completed`, freezes movement/camera/fill/fire input and shows the
  level-complete panel (see below).
- **[PlayerAnimationController.cs](Assets/ColorGame/Scripts/Player/PlayerAnimationController.cs)** —
  drives the character `Animator`'s `MoveSpeed`/`IsSpraying` parameters from
  `PlayerMovementController` and `PaintGunFireController` (see Character & Animation below).

See [HIERARCHY.md](HIERARCHY.md) for the full event/dependency flow between these systems.

## Character, Blaster & Animation

The placeholder capsule visual is replaced by the **MaleCharacterPBR** Humanoid character
(`Assets/RPG Tiny Hero Duo/`), holding the **Cosmic_Retro_Blaster_1** model
(`Assets/Cosmic_Retro_Blasters Pack_1_FREE/`) in its right hand:

- The configured character (visual + `Animator` + `MaleCharacterPlayer.controller` + right-hand
  socket + blaster) is saved as a **Prefab Variant** at
  [MaleCharacterPlayer.prefab](Assets/ColorGame/Prefabs/Player/MaleCharacterPlayer.prefab) — the
  original vendor prefabs are untouched.
- The vendor character's own sword (`weapon_r`) and shield (`weapon_l`) prop sockets are
  deactivated (not deleted, so the Humanoid Avatar and skeleton stay intact).
- `RightHandWeaponSocket` sits under the `hand_r` bone; the existing `PaintGun` gameplay object
  (with `PaintSprayer`, `SprayOrigin`, particles, and the paint-reservoir visual) is parented under
  it, so it follows the hand through every animation. `Cosmic_Retro_Blaster_1` is the visible model
  inside `PaintGun`.
- **[MaleCharacterPlayer.controller](Assets/ColorGame/Animations/Controllers/MaleCharacterPlayer.controller)**
  has two layers:
  - **Base Layer** — a 1D Blend Tree on the `MoveSpeed` float parameter, blending
    `Idle_Normal_SwordAndShield` (0) into `MoveFWD_Normal_InPlace_SwordAndShield` (1). Root motion
    is disabled; the in-place clip never moves the `Player` transform.
  - **RightArmSpray** layer — masked by
    [RightArmSpray.mask](Assets/ColorGame/Animations/Masks/RightArmSpray.mask) (right arm/fingers
    only), switching `Empty` ↔ `SprayDefend` (`Defend_SwordAndShield`, mirrored) on the `IsSpraying`
    bool, so the left arm and legs keep playing the Base Layer's idle/walk animation uninterrupted.
- `PlayerAnimationController.cs` is the only thing that writes to the Animator: `MoveSpeed` comes
  from `PlayerMovementController.NormalizedHorizontalSpeed`, and `IsSpraying` mirrors
  `PaintGunFireController.FireStarted`/`FireStopped` — so the pose always reflects the same
  movement/firing state the rest of the game already uses.

**To test:** press Play — the character should idle, blend into the walk animation while moving,
and raise the blaster with its right arm for as long as Fire is held (releasing Fire, running out
of paint, leaving the target, or completing the level all return it to idle/walk).

## Level Completion (Stage 10)

When every required region of the active target (`PaintTargetDefinition`) reaches its completion
threshold, `PaintCoverageTracker` raises `Completed` and `LevelCompleteController` (on the `Canvas`
GameObject):

1. Freezes player movement, camera look, and any in-progress Fill/Fire hold — immediately, not
   after a delay (`Time.timeScale` stays at `1`; this is an input lock, not a pause).
2. Shows the `LevelCompletePanel` — a full-screen overlay with a "Level Completed" title and two
   buttons, **Replay** and **Next Level**.
3. Both buttons call the same `ReloadCurrentScene()`, which reloads the current scene by build
   index. They intentionally do the identical thing for now — real next-level progression,
   rewards, and a win camera are later stages, not this one.

The panel is hidden by default (both in the saved scene and in code) and shows at most once per
scene load; a second `Completed` call is ignored.

**To test:** press Play, fill from a tank and spray both required regions of the heart target
until the panel appears, then click Replay (or Next Level) and confirm the scene reloads with the
panel hidden again and all paint/progress/reservoir state reset.

## Getting Started

1. Open the project in **Unity 6000.x** (Unity Hub → Add → select this folder).
2. Let Unity resolve packages (`Packages/manifest.json`) on first open.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press Play — use the on-screen joystick to move, drag to look, and hold the contextual
   Fill/Fire button near a tank or paintable surface. Fully paint the target to see the
   level-complete panel.

## Notable Packages (`Packages/manifest.json`)

- `com.unity.render-pipelines.universal` — URP
- `com.unity.ai.navigation` — NavMesh
- `com.unity.inputsystem` — new Input System
- `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.ugui`
- `com.ivanmurzak.unity.mcp` — Unity MCP plugin (AI-agent ↔ Editor bridge), served via the
  `package.openupm.com` scoped registry
