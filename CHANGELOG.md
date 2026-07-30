# Changelog

All notable changes to this project are documented in this file, derived from the git history.
Format loosely follows [Keep a Changelog](https://keepachangelog.com/); this project does not yet
use version numbers, so entries are grouped by date/commit instead.

## 2026-07-30 — Liquid paint polish and visible target guide

- **Target guide**: `PaintTargetMaskProvider` now also builds a persistent `GuideTexture`
  (RGBA32, region `DisplayColor` in RGB, low alpha interior / high alpha boundary) in the same
  `RebuildMasks()` pass that builds the allowed masks — built once, never per-frame, using the same
  first-region-owns policy as `PaintCoverageTracker`, resampled onto a shared resolution so
  differently-sized region masks (`HeartLeftMask_Test` / `HeartRightMask_Test`) still combine
  correctly. Exposed as `GuideTexture`/`HasGuideTexture`.
- **`PaintableSurface.shader` rewritten** from a flat unlit blend into a lit-ish liquid-paint
  look: a 5-tap `_PaintTex` alpha gradient fakes a raised normal for a Blinn-Phong specular
  highlight (glossy only where painted) and a wet meniscus rim light at paint edges; a slow
  scrolling liquid noise tint on painted areas only (no-op with the default neutral-gray texture);
  and the new target-guide overlay, gated by an explicit `_HasTargetGuide` toggle so a missing
  guide texture never shows a black/incorrect default, fading out under paint via
  `guideAlpha * _GuideOpacity * (1 - paintAlpha)`.
- **`PaintBrush.shader`**: brush outer radius is now perturbed by a fixed noise texture sampled in
  surface UV space, concentrated near the stamp's edge only (center stays solid); `_AllowedMask`
  remains the final gate, so noise cannot leak paint across regions. Default noise strength is `0`
  (no visual change) until a noise texture is explicitly assigned.
- Generated [LiquidPaintNoise.png](Assets/ColorGame/Textures/Paint/LiquidPaintNoise.png) (256×256
  seamless grayscale value noise) once via editor script — not at runtime — and wired it into both
  the liquid surface noise and the brush edge noise on `PaintTarget_Test/PaintSurface`.
- `PaintableSurface.cs`: added serialized `liquidNoiseTexture`/`brushNoiseTexture` refs and wired
  `_TargetGuideTex`/`_HasTargetGuide`/`_LiquidNoiseTex` through the existing
  `MaterialPropertyBlock` only (no `renderer.material`, no new runtime `Material` for the surface
  renderer). Missing guide/noise textures log one warning each and disable that effect — no white
  or magenta fallback.
- `ImpactParticles` tuned for a droplet/splash feel (shorter lifetime, mild gravity, speed/size
  variance, shrinks to zero over its lifetime); `SprayParticles` now renders as a stretched
  billboard along velocity for a tighter stream look. `PaintSprayer.cs` fires one small
  `impactParticles.Emit(...)` burst the instant the stream first lands or changes color, as a
  lightweight contact pulse (no new particle system, no UI, no shake, no sound). Both particle
  systems continue to share the existing `PaintSprayParticle.mat`, unchanged.
- Did not modify `PaintCoverageTracker.cs`, `PaintTargetDefinition.cs`, or any progress/win-panel/
  movement/animation script — this stage is purely visual polish on top of the existing painting
  and target systems.

## 2026-07-30 — Fix: walk animation not playing at all (regression from LocomotionPlaybackSpeed)

- **Regression**: after the previous locomotion fix, the character stopped visibly animating
  entirely — `Idle` and `Move` both appeared as a static pose, for both slight and full joystick
  input, even though `Player` movement and `PlayerVisualRoot` rotation continued working correctly
  (confirming the bug was isolated to the Animator, not `PlayerMovementController`).
- **Static inspection findings** (Play-mode telemetry was not available this session, so live
  parameter/state values during Play could not be directly observed): every structural setting
  checked out correctly even before this fix — `Move` state's `speed` was already `1` (not `0`),
  `PlayerAnimationController.animator`/`movementController`/`fireController` all pointed at the
  live, correct component instances (no stale references survived the earlier
  `PrefabUtility.ApplyPrefabInstance` call), the Animator's `avatar`/`isHuman`/`applyRootMotion`/
  `enabled` were all correct, only one `Animator` exists on the skeleton, and no Any-State
  transition or duplicate transition was forcing the layer back to `Idle`. No definitive single
  smoking gun was found via static inspection alone.
- Per the explicit instruction for this fix, **removed the `LocomotionPlaybackSpeed` mechanism
  entirely** — the previous fix used it as the `Move` state's Speed Parameter (a runtime float
  multiplier on top of the state's base speed), which is a plausible failure point (e.g. a
  parameter read/write timing issue) that a fixed-speed state cannot exhibit. Confirmed via
  `grep -rl "LocomotionPlaybackSpeed" Assets` that nothing else referenced it before removing the
  Animator parameter.
- **Defensive fix applied in addition** (since static inspection could not rule it out): the
  character's `Animator.cullingMode` was `CullUpdateTransforms`, which skips updating bone
  transforms when Unity considers the renderer off-screen — a known cause of "logically animating
  but visually frozen" characters if renderer bounds are ever momentarily wrong. Changed to
  `AlwaysAnimate` for this single player character (negligible cost, eliminates the whole failure
  class).
- Rebuilt the Base Layer to match exactly:
  - `Idle` (default, `Idle_Normal_SwordAndShield`, speed 1, no speed/time/mirror/cycle-offset
    parameters) ↔ `Move` (`MoveFWD_Normal_InPlace_SwordAndShield`, **speed fixed at 1, `Speed
    Parameter` disabled** — no float drives playback rate anymore).
  - Exactly one transition each way: `Idle→Move` on `IsMoving == true` (no exit time, fixed
    0.05s), `Move→Idle` on `IsMoving == false` (no exit time, fixed 0.08s); `Can Transition To
    Self` disabled on both; duplicate transitions cleared before re-adding.
  - Removed the `LocomotionPlaybackSpeed` Animator parameter. Final gameplay parameter set is
    exactly `IsMoving` (bool) and `IsSpraying` (bool) — `MoveSpeed` was already removed in the
    prior stage.
- Simplified `PlayerAnimationController.cs` to the bool-only implementation: reads
  `movementController.HorizontalSpeed` directly (world m/s, not normalized) against
  `startMovingSpeed` (0.05) / `stopMovingSpeed` (0.02) hysteresis, sets `IsMoving` only on an
  actual state change, and no longer touches any speed/playback-rate parameter. `IsSpraying` logic
  is unchanged; `OnEnable` now also re-syncs `IsSpraying` to `fireController.IsFiring` on enable
  (covers the case where firing was already in progress when this component re-enables) and always
  resets both parameters to a known state on enable/disable.
- Verified the `Move` clip's import settings (`MoveFWD_Normal_InPlace_SwordAndShield.fbx`):
  Animation Type Humanoid, Avatar Setup `Copy From Other Avatar`, and — most importantly — the
  actual runtime `AnimationClip` (not the importer's raw per-take entry, which is a repackaged FBX
  take and not directly representative) reports `isLooping = true`, `humanMotion = true`, and a
  valid positive length (0.53s). No importer changes were made; the vendor asset was not modified.
- Confirmed the `RightArmSpray` layer, its mask, `Empty`/`SprayDefend` states, and the `Mirror`
  decision are all unchanged; `Empty`'s motion is (still) explicitly `null` so it contributes no
  static pose when not spraying.
- Re-applied all of the above to the connected `MaleCharacterPlayer.prefab` via
  `PrefabUtility.ApplyPrefabInstance`, then re-read `PlayerAnimationController`'s references from
  the live scene instance afterward to confirm they were not lost by the apply (they weren't).
  Particle materials, the paint system, movement speed, joystick, camera, Fill, Fire, progress, and
  the win panel were not touched in this fix.

## 2026-07-30 — Locomotion and paint-particle color fixes

- **Root cause, partial-joystick sliding**: the Base Layer used a 1D Blend Tree driven directly by
  `NormalizedHorizontalSpeed`, so any partial joystick input produced a partial Idle/Walk *blend*
  rather than the complete walk clip — visually, the character glided with a mostly-static lower
  body instead of walking. Replaced the Blend Tree with two plain states, **Idle** and **Move**
  (`MaleCharacterPlayer.controller`, Base Layer): `Idle` (`Idle_Normal_SwordAndShield`, default
  state) transitions to `Move` (`MoveFWD_Normal_InPlace_SwordAndShield`, played at full weight —
  never blended) on the new `IsMoving` bool, both transitions with no exit time and a fixed
  duration (0.08s in, 0.12s out). The old `MoveSpeed` float parameter and its Blend Tree sub-asset
  were removed (confirmed via a project-wide search that nothing else referenced `MoveSpeed`
  before deleting it).
- Added `LocomotionPlaybackSpeed` (float) driving the **`Move` state's own Speed Parameter only**
  — not `Animator.speed`, which would have also sped up the independent `RightArmSpray` layer.
  `PlayerAnimationController` now uses start/stop **hysteresis** thresholds
  (`startMovingThreshold` 0.05, `stopMovingThreshold` 0.025) to set `IsMoving` without flicker near
  zero speed, and computes `LocomotionPlaybackSpeed` as `HorizontalSpeed / referenceWalkWorldSpeed`
  (default reference 3.5, clamped to `[0.45, 1.4]`) so the walk cycle's playback rate roughly
  tracks actual world displacement instead of always playing at a fixed rate — reducing the
  appearance of foot sliding at partial input without changing `PlayerMovementController.moveSpeed`
  or the underlying movement feel.
- **Root cause, pink/magenta particles**: inspected both `SprayParticles`' and `ImpactParticles`'
  `ParticleSystemRenderer`s and found `sharedMaterial` was `null` on both — Unity renders any
  renderer with no material assigned using its built-in magenta "missing material" fallback. This
  was not a missing/unsupported shader, broken texture reference, nor a Color-over-Lifetime/
  Color-by-Speed override (both modules were already disabled on the inspected system).
- Created `Assets/ColorGame/Textures/Particles/SoftPaintParticle.png` (a generated 128×128 white
  circle with a smooth alpha falloff to transparent, Clamp wrap, bilinear, no mipmaps, alpha-is-
  transparency) and `Assets/ColorGame/Materials/Particles/PaintSprayParticle.mat` (`Universal
  Render Pipeline/Particles/Unlit`, Transparent surface, Alpha blend, `ZWrite` off, Base Color pure
  white, Base Map = the new texture). Assigned this one shared material to both
  `SprayParticles` and `ImpactParticles` renderers — the material's white base color never tints
  `startColor`, so the paint color set by `PaintSprayer` is the only source of the visible hue.
- Updated `PaintSprayer.cs`: `BeginSprayVisual` now stops-and-clears (`StopEmittingAndClear`)
  `SprayParticles` before applying the new color and playing — this only runs once per firing
  session (not per frame), so switching paint color between sessions can no longer leave old-color
  particles lingering. `ShowImpact` tracks the last-applied impact color and only
  stops-and-clears `ImpactParticles` on an actual color change (compared every hit-frame, but only
  acts when the color differs from the previous frame), leaving the continuous same-color case
  untouched. `ApplyParticleColor`'s architecture (`ParticleSystem.MainModule.startColor =
  paint.DisplayColor`) is unchanged. The paint color source remains
  `PaintGunReservoir.CurrentPaint.DisplayColor` throughout — never sampled from
  `PaintContainerVisual`'s rendered color or compared by name.
- Applied all of the above to the connected `MaleCharacterPlayer.prefab` Prefab Variant (the
  `SprayParticles` renderer lives inside that prefab's hierarchy) via
  `PrefabUtility.ApplyPrefabInstance`; `ImpactParticles` lives on the scene-only `PaintTarget_Test`
  and needed only a scene save. Neither vendor source prefab (`MaleCharacterPBR.prefab`,
  `Cosmic_Retro_Blaster_1.prefab`) was touched.

## 2026-07-29 — Stage 11: character, blaster, and animation integration

- Replaced the placeholder capsule `CharacterVisual` (deactivated, not deleted) with an instance of
  the vendor `MaleCharacterPBR` prefab (`Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPBR.prefab`,
  Humanoid rig, valid Avatar) under `Player/PlayerVisualRoot`. Corrected its local rotation
  (`+180°` Y — the model's visual front faces `-Z` at identity rotation) and local position
  (`+0.05` Y) so it faces `PlayerVisualRoot.forward` and its feet sit exactly on the ground instead
  of clipping ~5cm below it.
- Removed the vendor character's `weapon_l` (held a `Shield08` mesh) and `weapon_r` (held an
  `OHS03` one-handed-sword mesh) by deactivating both socket transforms — they are plain prop
  attachment points (`Transform` only, not humanoid-mapped bones), so this cannot affect the Avatar
  or produce missing-transform Animator errors.
- Added `RightHandWeaponSocket` as a child of the `hand_r` bone, and reparented the existing
  `PaintGun` (with its `PaintSprayer`, `SprayOrigin`, `SprayParticles`, `PaintContainerVisual`, and
  the old placeholder `GunVisual`) onto it — replacing the old static `GunMount` anchor (now
  deleted) so the gun follows hand animation. Instantiated `Cosmic_Retro_Blaster_1` inside
  `PaintGun` as the new visible gun model and deactivated the old placeholder `GunVisual` box.
  Nudged `SprayOrigin`'s local position to the blaster's muzzle (from the prefab's own render
  bounds); its existing forward-and-down aim direction was kept unchanged.
- Created `Assets/ColorGame/Animations/Controllers/MaleCharacterPlayer.controller`:
  - Parameters `MoveSpeed` (float) and `IsSpraying` (bool).
  - Base Layer: a 1D Blend Tree on `MoveSpeed` (`Idle_Normal_SwordAndShield` at 0,
    `MoveFWD_Normal_InPlace_SwordAndShield` at 1 — both already looping, Humanoid-compatible clips
    from the same character pack, used unmodified).
  - `RightArmSpray` layer (Override, weight 1, masked by the new
    `Assets/ColorGame/Animations/Masks/RightArmSpray.mask`): `Empty` ↔ `SprayDefend`
    (`Defend_SwordAndShield`) on `IsSpraying`, no exit time, 0.08s / 0.12s transitions.
  - `SprayDefend` has **Mirror enabled** — verified by comparing the clip's humanoid muscle-curve
    ranges (no Play-mode preview was available): the left-arm curves sum to a larger range (0.416)
    than the right-arm curves (0.329), consistent with a shield-block pose authored left-dominant;
    mirroring puts the dominant motion on the right arm, which the mask then isolates.
  - Assigned to the character's existing `Animator` (already Humanoid/valid-Avatar/
    `CullUpdateTransforms`/`Normal` update mode out of the box); set `applyRootMotion` to `false`.
- Added `PlayerAnimationController.cs` (`Player` root) — drives `MoveSpeed` from
  `PlayerMovementController.NormalizedHorizontalSpeed` (damped) and `IsSpraying` from
  `PaintGunFireController.FireStarted`/`FireStopped`; resets `IsSpraying` in `OnDisable`. Reads no
  input directly and performs no firing/paint logic itself.
- Added read-only animation-facing properties to `PlayerMovementController`: `HorizontalSpeed`,
  `NormalizedHorizontalSpeed`, `IsMoving`. `SetMovementEnabled(false)` continues to zero
  `horizontalVelocity` immediately, unchanged from Stage 10.
- Saved the configured character (visual + Animator + controller + socket + blaster) as a **Prefab
  Variant** of the original: `Assets/ColorGame/Prefabs/Player/MaleCharacterPlayer.prefab`. The
  original `MaleCharacterPBR.prefab` and `Cosmic_Retro_Blaster_1.prefab` source assets were never
  modified. Gameplay components (`PlayerMovementController`, `PaintFillController`,
  `PaintGunFireController`, etc.) remain on the scene `Player` root, unchanged.
- Kept the `CharacterController` (`height` 1.8, `center` (0, 0.9, 0)) as-is — the new character's
  standing bounds (~1.6m) fit comfortably inside it; no clipping or excess extension.

## 2026-07-28 — Stage 10: basic level-complete panel

- Added `LevelCompleteController` (on the `Canvas` GameObject, alongside `MobileControlLayout` —
  it must live on a GameObject that stays active, since the panel it controls starts hidden and an
  inactive GameObject's own scripts never run `OnEnable`). It subscribes to
  `PaintCoverageTracker.Completed` and, on the first call only (`IsLevelComplete` latch):
  - Freezes gameplay input: `PlayerMovementController.SetMovementEnabled(false)`,
    `MobileInputReader.SetMovementInputEnabled(false)`, `ThirdPersonOrbitCamera.SetCameraInputEnabled(false)`.
  - Disables `PaintFillController` / `PaintGunFireController` / `ContextualActionUI` — their
    existing `OnDisable` handlers already stop active fill/fire effects and release held buttons
    cleanly, so no duplicate stop-logic was needed here.
  - Shows the new `LevelCompletePanel` (re-asserted as the last Canvas sibling at show time).
- Added the `LevelCompletePanel` UI hierarchy under `Canvas` (last sibling, so it renders above the
  joystick, camera-look area, both HUDs, and the Fill/Fire buttons): a full-screen semi-transparent
  raycast-blocking overlay, a centered 700×420 panel holder, a "Level Completed" title, and
  side-by-side **Replay** / **Next Level** buttons (240×90 each). Both buttons call the same
  `LevelCompleteController.ReloadCurrentScene()`, which reloads the active scene by build index and
  guards against double-clicks (`reloadRequested`, buttons disabled after the first accepted click).
  Replay and Next Level intentionally perform the identical action for this prototype — real
  next-level progression is out of scope for this stage.
- `Time.timeScale` is left at `1` throughout — the panel is a normal, always-clickable UI overlay,
  not a paused/frozen game state.
- **Fix**: `PlayerMovementController.SetMovementEnabled(false)` now zeroes `horizontalVelocity`
  immediately instead of only gating new input — previously the character would coast through its
  normal deceleration ramp for a few frames after being disabled. Vertical velocity (gravity) is
  untouched, so this cannot mask genuine grounding behavior.
- Explicitly out of scope for this stage (per spec): stars, coins, rewards, confetti, a win camera,
  scene transitions, level selection, save data, character/gun selection, and missions.

## 2026-07-28 — Diff color add and canvas (`0cf9c9d`)

- Added support for targets that require **different colors on different regions of the same
  canvas** (e.g. red on one side, blue on the other) via `PaintTargetDefinition.Region` +
  `PaintTargetMaskProvider`.
- Added the `HeartTarget_RedBlue` target asset (two regions, each with its own mask texture) next
  to the original single-color `HeartTarget_Red`.

## 2026-07-27 — Paint bar and target color done (`6157372`)

- Added the target-completion HUD: `PaintProgressUI` + `PaintRegionProgressRow` (overall progress
  bar plus one row per region), driven entirely by `PaintCoverageTracker` events.
- Added `PaintCoverageTracker` — tracks correct-color coverage per region on a CPU evaluation grid
  built from each region's mask texture, updated incrementally from paint stamps.
- Added the paint reservoir HUD (`PaintReservoirUI`) bound to `PaintGunReservoir`.

## 2026-07-27 — Paint Fill & Fire done (`4ae1578`)

- Added the core paint loop:
  - `PaintTank` / `PaintFillController` — hold-to-fill interaction that transfers paint from a
    tank into the player's `PaintGunReservoir`.
  - `PaintGunFireController` / `PaintSprayer` / `PaintSurfaceMarker` — hold-to-fire interaction
    that raycasts and sprays paint onto valid surfaces.
  - `PaintableSurface` + `PaintBrush.shader` / `PaintableSurface.shader` — GPU-based permanent
    paint accumulation on a surface via render-texture blits.
  - `PaintGunReservoir` / `PaintGunVisual` — carried-paint state and its visual representation.
- Added the shared interaction framework: `PlayerInteractionZone`, `PlayerInteractionDetector`,
  `ContextualActionUI`, `HoldActionButton`, `InteractionActionType`.
- Added mobile HUD layout (`MobileControlLayout`).
- Introduced the `ColorGame` asset folder (superseding the original `#Paint Game` folder).

## 2026-07-24 — Controller & camera done (`4c425c9`)

- Added `PlayerMovementController` (`CharacterController`-based, camera-relative movement) and
  `MobileInputReader` (joystick + keyboard-fallback input).
- Added `ThirdPersonOrbitCamera` and `CameraLookInput` (drag-to-look orbit camera with
  wall-collision handling).
- Removed the earlier `CameraFollow.cs` / `PlayerController.cs` prototype scripts from
  `#Paint Game/` in favor of the above.

## 2026-07-24 — Initial push (`d6bf358`) / Initial commit (`c9a2311`)

- Initial Unity 6 project scaffold: URP (PC + Mobile render assets), TextMesh Pro, DOTween /
  DOTween Pro, the Joystick Pack asset, Unity MCP editor plugin, and the first prototype
  `PlayerController.cs` / `CameraFollow.cs` under `#Paint Game/`.
