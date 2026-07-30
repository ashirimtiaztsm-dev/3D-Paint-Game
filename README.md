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
  drives the character `Animator`'s `IsMoving`/`LocomotionPlaybackSpeed`/`IsSpraying` parameters from
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
  - **Base Layer** — plain `Idle` ↔ `Move` states (**not** a blend tree, and **not** driven by a
    speed-scaling parameter — both were tried and caused problems: the blend tree made partial
    joystick input play a weak sliding-looking mix, and a `Move`-state speed parameter
    [`LocomotionPlaybackSpeed`] was later found to be a plausible cause of the walk animation not
    playing at all). `Idle` plays `Idle_Normal_SwordAndShield`; `Move` plays the **complete**
    `MoveFWD_Normal_InPlace_SwordAndShield` clip at a **fixed speed of 1** whenever the `IsMoving`
    bool is true — no parameter scales its playback rate. Root motion is disabled; the in-place
    clip never moves the `Player` transform.
  - **RightArmSpray** layer — masked by
    [RightArmSpray.mask](Assets/ColorGame/Animations/Masks/RightArmSpray.mask) (right arm/fingers
    only), switching `Empty` ↔ `SprayDefend` (`Defend_SwordAndShield`, mirrored) on the `IsSpraying`
    bool, so the left arm and legs keep playing the Base Layer's idle/walk animation uninterrupted.
- `PlayerAnimationController.cs` is the only thing that writes to the Animator, and it's
  deliberately simple: `IsMoving` is **hysteresis-gated** on `PlayerMovementController`'s actual
  world-space `HorizontalSpeed` (metres/second, not normalized) — it turns on above
  `startMovingSpeed` (0.05) and off below the lower `stopMovingSpeed` (0.02), so tiny residual
  speed during deceleration can't flicker Idle/Move back and forth, and the bool is only written
  to the Animator on an actual state change. Any input past the start threshold plays the
  complete walk clip at its normal speed — there is no partial blend and no runtime speed
  scaling. `IsSpraying` mirrors `PaintGunFireController.FireStarted`/`FireStopped` (and is
  re-synced to `fireController.IsFiring` on enable).
- The Animator's `cullingMode` is `AlwaysAnimate` (not the default `CullUpdateTransforms`) — a
  defensive choice for this single player character, since `CullUpdateTransforms` can skip bone
  updates entirely if Unity ever misjudges the renderer as off-screen.

**Runtime verification, if locomotion ever looks wrong again**: check (in this order) whether
`PlayerAnimationController.animator` still points at the *active* `Animator` on the character
instance (not a stale reference from a previous prefab apply), whether `Animator.IsMoving` is
actually flipping true/false as expected, whether the Base Layer's current state is `Move`, and
whether that state's `normalizedTime` is advancing — a state that's active but frozen in time
means something is overriding playback (culling, a stray speed parameter, etc.), while a state
that never leaves `Idle` means the `IsMoving` bool or its transition condition is the problem.

**To test:** press Play — the character should idle, and any meaningful joystick input (even
slight, in any direction) should immediately play the complete walk animation with visibly moving
legs, and raise the blaster with its right arm for as long as Fire is held (releasing Fire, running
out of paint, leaving the target, or completing the level all return it to idle/walk).

## Paint Particles

`SprayParticles` and `ImpactParticles` use a dedicated URP-compatible material,
[PaintSprayParticle.mat](Assets/ColorGame/Materials/Particles/PaintSprayParticle.mat) (`Universal
Render Pipeline/Particles/Unlit`, white Base Color, a soft circular white texture as the Base Map).
Previously both `ParticleSystemRenderer`s had **no material assigned at all**, which Unity renders
using its built-in pink/magenta "missing material" fallback — not a broken or unsupported shader.

The paint color itself still comes from `PaintGunReservoir.CurrentPaint.DisplayColor`, applied via
`PaintSprayer.ApplyParticleColor` → `ParticleSystem.MainModule.startColor` (never sampled from
`PaintContainerVisual`'s rendered color, never compared by name). Since the material's Base Color
is white, `startColor` is the only thing that tints the particles, so red paint produces red
particles and blue paint produces blue particles with no extra logic needed. `PaintSprayer` now
also stops-and-clears both particle systems whenever a new firing session starts or the active
paint color actually changes (not every frame), so switching from red to blue can't leave old red
particles lingering.

**To test:** fill red, fire, confirm red spray/impact particles; fill blue, fire, confirm blue
spray/impact particles with no leftover red particles from the previous session.

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
