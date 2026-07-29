# Changelog

All notable changes to this project are documented in this file, derived from the git history.
Format loosely follows [Keep a Changelog](https://keepachangelog.com/); this project does not yet
use version numbers, so entries are grouped by date/commit instead.

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
