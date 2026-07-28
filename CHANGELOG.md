# Changelog

All notable changes to this project are documented in this file, derived from the git history.
Format loosely follows [Keep a Changelog](https://keepachangelog.com/); this project does not yet
use version numbers, so entries are grouped by date/commit instead.

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
