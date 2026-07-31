# Changelog

All notable changes to this project are documented in this file, derived from the git history.
Format loosely follows [Keep a Changelog](https://keepachangelog.com/); this project does not yet
use version numbers, so entries are grouped by date/commit instead.

## 2026-07-31 — Fix spray particle velocity-curve error and movement bending

- **Problem 1 (Console error)**: `"Particle Velocity curves must all be in the same mode"`,
  spammed every frame while `SprayParticles` was playing.
- **Confirmed exact cause** by inspecting the live `VelocityOverLifetimeModule`: `SprayParticles`'
  X curve mode = `TwoConstants`, Y curve mode = `TwoConstants`, **Z curve mode = `Constant`** — a
  self-inflicted regression from an earlier session's particle-tuning pass, where X/Y were set via
  `new ParticleSystem.MinMaxCurve(min, max)` (which produces `TwoConstants` mode) and Z via `new
  ParticleSystem.MinMaxCurve(0f)` (the single-argument overload, which produces `Constant` mode).
  Unity requires all three axes of a curve module to share one mode. `ImpactParticles`' `Velocity
  over Lifetime` was confirmed already disabled and was never involved in this error.
- Fixed by disabling `Velocity over Lifetime` entirely on `SprayParticles` (rather than
  re-authoring three matching-mode curves) — cone spread, random start speed/size/rotation already
  provide sufficient droplet variation without it. Also disabled `Noise` on `SprayParticles`, which
  was independently causing visible sideways drift.
- **Problem 2 (movement bending)**: the spray stream visibly curved and trailed behind the gun
  while the character moved (confirmed via the reported gameplay video). Cause: `SprayParticles`
  used `simulationSpace = World`, so already-emitted droplets kept their original world-space
  trajectory after the emitter (and character) moved on. Fixed by setting `simulationSpace =
  Local` — droplets now move with the `SprayParticles` transform, keeping the stream visually
  attached to the muzzle through forward/backward/strafe/turning movement.
  `ImpactParticles` intentionally stays in **World** space (unchanged) — impact droplets must
  remain at the hit surface, not follow the moving gun.
- Verified the transform scale chain from `Player` down to `SprayParticles`: every ancestor is
  exactly `(1,1,1)`; `SprayParticles` itself has a uniform `1.5×` local scale (equal on all axes,
  so it doesn't distort stream direction). No non-uniform scaling found anywhere in the chain.
- `PaintSprayer.cs` reviewed and confirmed unchanged — it never sets `simulationSpace`, velocity,
  noise, or particle-transform rotation, and the raycast still uses `sprayOrigin.forward`.
- Files/scene objects changed: `SprayParticles` (`ParticleSystem` `velocityOverLifetime`, `noise`,
  `main.simulationSpace` only). `ImpactParticles`, `PaintSprayer.cs`, jelly-paint shaders/mesh,
  target masks, progress tracking, the vertical reservoir UI, win panel, character
  movement/animation, and camera were **not** touched.
- **Regression check**: zero console errors or warnings after the fix and a fresh `assets-refresh`
  (confirmed via `console-get-logs`, which also showed the error firing repeatedly during actual
  Play-mode sessions before the fix, confirming the bug was live and reproducible, not just a
  static-inspection concern).

## 2026-07-31 — Fix vertical reservoir bar not visibly filling

- **Problem**: the reservoir HUD was already converted to a vertical layout (`Fill Method =
  Vertical`, `Fill Origin = Bottom`), but the colored fill never visibly rose or fell — only the
  numeric `{current} / {max}` label changed.
- **Confirmed root cause** by inspecting the live `Image` components: `PaintBarFill` and
  `PaintBarBackground` both had **`Source Image = None`** (`m_Sprite = null`). For comparison, the
  project's other working fill bar (`PaintProgressHUD/ProgressBackground/ProgressFill`) has a real
  sprite assigned (`Horizontal_Plain`, from the Joystick Pack) — the reservoir bar didn't have an
  equivalent. `PaintReservoirUI.fillImage` itself was correctly wired to `PaintBarFill`'s `Image`
  (not stale) and `fillAmount` was being set correctly by the script every time `AmountChanged`
  fired — the assignment logic was never the bug, only the missing sprite on the target `Image`.
- Rejected reusing `Horizontal_Plain` (a tight-packed, pill-shaped joystick-background sprite, not
  a neutral rectangle — would visually distort when stretched into a tall vertical bar). Instead
  created a dedicated
  [PaintBarFillWhite.png](Assets/ColorGame/Textures/UI/PaintBarFillWhite.png) (16×16, fully white,
  imported as `Sprite (2D and UI)`, `Sprite Mode = Single`) and assigned it to both
  `PaintBarBackground` and `PaintBarFill`.
- Set `PaintBarFill`'s `RectTransform` to a proper 3px inset stretch (`offsetMin (3,3)` /
  `offsetMax (-3,-3)`) inside `PaintBarBackground`, per the required layout.
- Rewrote `PaintReservoirUI.cs`: added a `backgroundImage` field (previously the background was
  never referenced by the script at all — only visually placed in the hierarchy); added
  `ConfigureFillImage()` to explicitly set the fill `Image`'s type/fill-method/fill-origin/
  `preserveAspect`/`raycastTarget` in code on `Awake`/`OnEnable`, so a future scene
  misconfiguration can't silently reintroduce this class of bug; unified `HandlePaintColorChanged`/
  `HandleAmountChanged`/`Refresh` into one `RefreshVisuals()`; the colored `fillImage` layer is now
  explicitly `enabled = false` at zero paint (`emptyEpsilon` threshold) while `backgroundImage`
  stays `enabled = true` — the empty grey container is always visible, the colored layer only
  appears once there's paint to show. Still fully event-driven (`AmountChanged`/
  `PaintColorChanged`), no `Update()` polling.
- Reassigned all `PaintReservoirUI` serialized references on the live scene object and confirmed
  via `object-get-data` that none were stale: `reservoir` → the `Player`'s `PaintGunReservoir`,
  `backgroundImage` → `PaintBarBackground`'s `Image`, `fillImage` → `PaintBarFill`'s `Image`,
  `iconImage` → `PaintIcon`'s `Image`, `amountLabel` → `AmountLabel`'s `TextMeshProUGUI`.
- **Runtime verification** (no Play-mode tool was available in this environment, so verified by
  directly driving the live `PaintGunReservoir` instance and invoking `PaintReservoirUI`'s private
  `RefreshVisuals()` via reflection, then reading back the live `Image` state — not a simulation,
  the actual scene components): `0/100` → `fillAmount 0`, fill disabled, grey background visible;
  `45/100` → `fillAmount 0.450`, red, enabled; `100/100` → `fillAmount 1.000`; consuming down to
  `25/100` → `fillAmount 0.250` (confirms top-to-bottom draining while firing); switching to blue
  mid-reservoir at `50/100` → `fillAmount 0.500`, blue (confirms the color-replacement policy
  still resets the bar correctly); draining blue to `0/100` → fill disabled again, grey remains.
  All values matched expectations exactly.
- Files changed: `Assets/ColorGame/Scripts/UI/PaintReservoirUI.cs`, new
  `PaintBarFillWhite.png`, and the `Canvas/PaintHUD/PaintBarBackground`/`PaintBarFill`
  `Image`/`RectTransform` scene references. `PaintGunReservoir.cs` was **not** modified — its
  `AmountChanged`/`PaintColorChanged` events already fired correctly on every add/consume/clear/
  color-replace; the bug was purely a missing sprite on the UI side.
- **Regression check**: spray particles, jelly-paint shader/mesh, target-color restrictions,
  `PaintCoverageTracker`, the win panel, and character animation were not touched. Zero console
  errors after the final `assets-refresh`.

## 2026-07-31 — Liquid-droplet spray and vertical reservoir HUD

- **Spray problem**: `SprayParticles` rendered as a `Stretched Billboard` with a high length/velocity
  scale, straight `Local`-space trajectory, no gravity, no size variation, and no spread — the
  combination read as a rigid glowing laser beam rather than sprayed liquid.
- Reconfigured the existing `SprayParticles` system (no new particle object, no script changes):
  `renderMode` `Stretch` → `Billboard`; `simulationSpace` `Local` → `World`; `startLifetime`
  0.16–0.28s, `startSpeed` 3.5–5.5, `startSize` 0.035–0.085, `gravityModifier` 0.05–0.18 (subtle
  arc); `Cone` shape at 6°/0.025 radius (slight spread); Velocity-over-Lifetime X/Y jitter; Noise
  module (strength 0.12, low frequency, damped — organic, not smoke-like); Size-over-Lifetime curve
  `0.7 → 1.0 → 0` (liquid-blob swell-and-shrink); Color-over-Lifetime alpha-only fade (RGB stays
  white — color still comes exclusively from `PaintColorDefinition.DisplayColor` via `startColor`);
  a 5–7 particle burst at the start of every `Play()` cycle (fires once per `BeginSprayVisual()`
  call — no script change needed).
- Replaced the base particle texture with
  [PaintDropletParticle.png](Assets/ColorGame/Textures/Particles/PaintDropletParticle.png) (128×128,
  white, irregular/asymmetrical rounded blob, no glow halo, generated once via editor script) —
  the previous [SoftPaintParticle.png](Assets/ColorGame/Textures/Particles/SoftPaintParticle.png)
  was a perfectly soft circular glow that read as "magical" rather than liquid.
  `PaintSprayParticle.mat` required no changes: already Alpha-blended (not Additive), white Base
  Color, `ZWrite`/`Cull` off.
- Retuned `ImpactParticles` to match: `Billboard`, shorter lifetime (0.18–0.35s), lower speed
  (0.3–1.2), smaller size (0.04–0.10), mild gravity (0.15–0.35), a wider 30° cone, a 5–8 particle
  burst on contact, and a light continuous rate (18) while actively hitting — reads as a small wet
  splash, not a smoke cloud.
- `PaintSprayer.cs` reviewed and left unchanged — it already satisfied every requirement: color
  sourced exclusively from `paint.DisplayColor` → `ParticleSystem.MainModule.startColor`,
  clear-on-color-change via `BeginSprayVisual`/`ShowImpact`, impact repositioning to hit
  point/normal, no runtime materials, no `renderer.material`, color only reapplied on an actual
  change (never per-frame).
- **HUD problem**: the paint reservoir bar (`Canvas/PaintHUD/PaintBarBackground/PaintBarFill`) was
  a horizontal fill bar.
- Converted to a **vertical, bottom-to-top** fill by changing `PaintBarFill`'s `Image` component
  (`Fill Method` `Horizontal` → `Vertical`, `Fill Origin` → `Bottom`) and resizing/repositioning
  `PaintHUD` (110×300), `PaintIcon` (moved above the bar), `PaintBarBackground` (50×200, vertical),
  and `AmountLabel` (moved below the bar) — same top-left corner as before, kept clear of
  `PaintProgressHUD` (top-right) and the joystick/action-button area (bottom).
- **`PaintReservoirUI.cs` required zero code changes** — `fillImage.fillAmount =
  reservoir.NormalizedAmount` and the `"{current} / {max}"` label already worked identically
  regardless of the `Image`'s fill method/origin; the existing event-driven
  (`AmountChanged`/`PaintColorChanged`), non-polling architecture and fill-color-from-`DisplayColor`
  logic were already exactly what this task required.
- Files/scene objects changed: `SprayParticles` and `ImpactParticles` (`ParticleSystem`/
  `ParticleSystemRenderer` settings), `PaintSprayParticle.mat` (`_BaseMap` texture reassigned only),
  new `PaintDropletParticle.png`, and the `Canvas/PaintHUD` RectTransform/`Image` hierarchy. No
  script files were modified this pass.
- **Regression check**: `PaintCoverageTracker`, `PaintTargetMaskProvider`, the jelly-paint shader
  pipeline, `PaintGunReservoir` gameplay behavior, `LevelCompleteController`, character animation,
  and Replay/Next Level were not touched. Zero console errors after the final `assets-refresh`.

## 2026-07-30 — Jelly-paint volume polish

- **Problem**: the accepted liquid-paint pass (glossy highlight, target guide, brush noise) still
  read as a flat painted texture rather than a poured layer of liquid — no visible thickness, no
  rounded blob edges, no merged puddle shape, and no internal motion. Target: a reference
  screenshot showing thick, rounded, jelly-like red paint with bright wet highlights and soft
  internal motion (**note**: no image data was attached to this session's conversation context —
  this pass was implemented from the request's detailed textual description of the reference,
  not a direct visual comparison; a human visual check against the actual reference is still
  needed).
- **`PaintBrush.shader` — domed thickness deposit + smooth blob merging**: replaced the flat
  soft-circle stamp with a rounded dome profile (`pow(saturate(1 - distance01^2), _JellyDomePower)`,
  no flat plateau, no hard edge, always solid exactly at the stamp centre). Repeated/overlapping
  stamps now merge via a polynomial smooth-max (`SmoothMax`, IQ-style, two scalar ops, no loop) of
  existing vs. incoming thickness plus a small `_ThicknessBuildRate` overlap bonus, clamped to
  `_MaximumThickness` — two touching stamps join into one smooth puddle instead of showing separate
  circular borders, and continuous spraying reads as one connected, slowly-thickening jelly layer.
  `_AllowedMask` is still the final per-pixel gate; wrong-color/out-of-region paint is still
  completely rejected.
- **Paint alpha reinterpreted as thickness, not visible opacity**: `PaintableSurface.shader` now
  derives a separate `coverage = smoothstep(_PaintCoverageStart, _PaintCoverageFull, thickness)`
  curve for all visible blending/lighting, instead of using the raw alpha channel directly. This is
  what lets thin edges stay rounded/translucent while thick centers read as solid.
- **Jelly height/normal**: combined a fine 1-texel gradient (meniscus edge detail) with a wider
  `_JellySmoothingRadius`-texel gradient (broad puddle mound) of paint thickness into one fake
  raised normal — no extra render target, no blur pass, fixed small tap count.
- **Meniscus**: replaced the previous single wet-edge rim
  (`_PaintEdgeHighlightStrength`/`_PaintEdgeWidth`) with a dedicated rounded-meniscus system
  (`_MeniscusWidth`/`_MeniscusStrength`/`_MeniscusSmoothness`/`_MeniscusTint`), gated by `coverage`
  so it can never show outside actual paint.
- **Jelly lighting**: added broad + sharp specular highlights, a Fresnel rim, and slight
  thickness-based depth darkening (`_JellyBroadSpecularPower`/`_JellySharpSpecularPower`/
  `_JellySpecularStrength`/`_JellyFresnelStrength`/`_JellyFresnelPower`/`_JellyDepthDarkening`).
  Removed the now-superseded `_PaintSmoothness`/`_PaintSpecularStrength`/`_PaintNormalStrength`
  properties from the previous stage.
- **Internal moving highlights**: the existing `LiquidPaintNoise.png` texture (unchanged, reused —
  not regenerated) is now sampled twice at different scales/slow scroll speeds and combined into a
  moving normal/highlight perturbation plus a soft pale internal cloudy pattern
  (`_JellyNoiseScaleA/B`, `_JellyNoiseSpeedA/B`, `_JellyNoiseStrength`, `_JellyHighlightVariation`,
  `_JellyInternalGlow`) — visible only inside painted coverage; paint boundaries, the target guide,
  and progress masks never move.
- **Impact ripple**: `PaintableSurface.cs` now records the latest accepted spray hit's UV,
  `Time.time`, and strength into the existing `MaterialPropertyBlock`
  (`_ImpactUV`/`_ImpactStartTime`/`_ImpactStrength`) on every accepted hit — no texture write, no
  per-frame update. The shader derives a localized, decaying ripple from `_Time.y` and applies it
  only to the jelly normal and specular strength, never to the mask/color/progress. `ClearPaint()`
  resets `_ImpactStrength` to `0` so no ripple lingers after a reset.
- Files changed: `Assets/ColorGame/Shaders/PaintBrush.shader`,
  `Assets/ColorGame/Shaders/PaintableSurface.shader`,
  `Assets/ColorGame/Scripts/Paint/PaintableSurface.cs`,
  `Assets/ColorGame/Materials/PaintableSurface_Test.mat` (full jelly tuning profile applied — see
  README for values). `PaintTargetMaskProvider.cs`, `PaintCoverageTracker.cs`, and
  `PaintTargetDefinition.cs` were **not** modified. `LiquidPaintNoise.png` was reused as-is, not
  regenerated.
- **Regression check** (structural/code-level — see the note on visual verification above):
  red/blue target-restricted painting, the visible heart guide, ping-pong RenderTexture painting,
  `PaintCoverageTracker` region/overall progress, the win panel, fill/fire controls, paint
  particles, character animation, and Replay/Next Level all reviewed unchanged in this pass — no
  edits touched `PaintCoverageTracker.cs`, `PaintTargetDefinition.cs`, `LevelCompleteController.cs`,
  `PlayerMovementController.cs`, `PlayerAnimationController.cs`, `PaintFillController.cs`, or
  `PaintGunFireController.cs`. Console showed zero compile errors after the final `assets-refresh`.

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
