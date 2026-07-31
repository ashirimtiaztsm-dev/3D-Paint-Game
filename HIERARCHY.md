# Hierarchy

This document maps two things that a flat file listing (see [ASSET_CATALOG.md](ASSET_CATALOG.md))
doesn't show: the **folder hierarchy** of `Assets/`, and the **runtime dependency/event hierarchy**
between the gameplay scripts in `Assets/ColorGame/Scripts/`.

## Folder Hierarchy

```
Assets/
├── ColorGame/                        Game-specific code & content
│   ├── Scripts/
│   │   ├── Player/                   PlayerMovementController
│   │   ├── Camera/                   ThirdPersonOrbitCamera
│   │   ├── Input/                    CameraLookInput, MobileInputReader, MobileControlLayout, HoldActionButton
│   │   ├── Interaction/              InteractionActionType, PlayerInteractionZone, PlayerInteractionDetector
│   │   ├── Paint/                    Paint gun, tanks, surfaces, coverage tracking (17 scripts)
│   │   └── UI/                       ContextualActionUI, PaintReservoirUI, PaintRegionProgressRow
│   ├── Shaders/                      PaintBrush.shader, PaintableSurface.shader
│   ├── ScriptableObjects/
│   │   ├── PaintColors/              RedPaint.asset, BluePaint.asset, YellowPaint.asset
│   │   └── Targets/                  HeartTarget_Red.asset, HeartTarget_RedBlue.asset
│   ├── Materials/                    BlackRoad.mat, PaintableSurface_Test.mat, black.jpg
│   ├── TestAssets/                   Heart mask textures used by the two heart targets
│   └── Joystick Pack/                Third-party on-screen joystick asset (unchanged from vendor)
├── Scenes/                           SampleScene.unity (main/only scene)
├── Settings/                         URP render pipeline assets & volume profiles (PC + Mobile)
├── TextMesh Pro/                     TMP runtime resources
├── Plugins/
│   ├── Demigiant/                    DOTween & DOTween Pro
│   └── NuGet/                        Managed DLLs for the Unity MCP editor plugin
├── TutorialInfo/                     Unity's default "Readme" template assets
└── InputSystem_Actions.inputactions
```

> Note: the game-specific folder was renamed from `#Paint Game/` to `ColorGame/` and restructured
> into the `Scripts/<System>/` layout above (see [CHANGELOG.md](CHANGELOG.md), 2026-07-27).
> Stage 11 (2026-07-29) added `ColorGame/Animations/{Controllers,Masks,Clips}` and
> `ColorGame/Prefabs/Player/`, plus the vendor packs `RPG Tiny Hero Duo/` and
> `Cosmic_Retro_Blasters Pack_1_FREE/` at the `Assets/` root.

## Scene Player Hierarchy (`Assets/Scenes/SampleScene.unity`) — Stage 11

`Player`'s `CharacterController` and gameplay components are unchanged from earlier stages; only
`PlayerVisualRoot`'s contents changed. Bone names below are the actual `MaleCharacterPBR` skeleton
(a UE4/Mixamo-style Humanoid rig), truncated to the path that matters:

```
Player                                (CharacterController, MobileInputReader,
                                        PlayerMovementController, PlayerInteractionDetector,
                                        PaintGunReservoir, PaintFillController,
                                        PaintGunFireController, PlayerAnimationController)
├── InteractionOrigin
├── CameraTarget
└── PlayerVisualRoot                  (rotated by PlayerMovementController; PlayerMovementController
   │                                   .characterVisual points HERE, never at a bone or mesh)
    ├── CharacterVisual               (old placeholder capsule — deactivated, kept for reference)
    └── MaleCharacterPlayer instance  (MaleCharacterPlayer.prefab variant; Animator here)
        └── root/pelvis/spine_01/spine_02/spine_03/
            ├── clavicle_l/upperarm_l/lowerarm_l/hand_l/
            │   └── weapon_l                        (deactivated — held Shield08, a static prop mesh)
            └── clavicle_r/upperarm_r/lowerarm_r/hand_r/
                ├── weapon_r                        (deactivated — held OHS03, a static prop mesh)
                └── RightHandWeaponSocket            (new — local identity relative to hand_r)
                    └── PaintGun                     (PaintSprayer — unchanged gameplay object)
                        ├── Cosmic_Retro_Blaster_1   (new — visible gun model)
                        ├── PaintContainerVisual     (PaintGunVisual — unchanged)
                        ├── GunVisual                (old placeholder box — deactivated)
                        ├── SprayOrigin              (unchanged rotation; nudged to the new muzzle)
                        └── SprayParticles           (unchanged)
```

`ImpactParticles` intentionally stays on `PaintTarget_Test` (not moved under `PaintGun`) — it marks
where paint lands on the *target*, not where it leaves the gun, and moving it would have altered
working impact-effect placement outside this stage's scope.

`MaleCharacterPlayer` instance corrections applied on top of the vendor prefab (all on the
instance/variant, never on `MaleCharacterPBR.prefab` itself): local rotation `(0, 180, 0)` — the
model's visual front faces `-Z` at identity rotation, opposite `PlayerVisualRoot.forward` — and
local position `(0, 0.05, 0)` to lift the feet exactly onto the ground (bounds showed a ~5cm clip).

## Scene Canvas Hierarchy (`Assets/Scenes/SampleScene.unity`)

`LevelCompletePanel` is the **last** sibling under `Canvas`, so it renders above every other
gameplay UI element listed here (Stage 10, see [CHANGELOG.md](CHANGELOG.md)):

```
Canvas                              (Canvas, CanvasScaler, GraphicRaycaster, MobileControlLayout,
                                      LevelCompleteController — must stay active: see below)
├── MoveJoystick
├── CameraLookArea
├── PaintHUD
├── PaintProgressHUD                (PaintProgressUI)
├── ActionButtons                   (ContextualActionUI)
└── LevelCompletePanel              (Image: full-screen dark overlay, raycastTarget=true; starts
                                      inactive — hidden until PaintCoverageTracker.Completed fires)
    ├── PanelBackground             (Image: centered 700×420 panel holder)
    ├── TitleText                   (TextMeshProUGUI: "Level Completed")
    └── Buttons                     (RectTransform container, 540×90, centered)
        ├── ReplayButton            (Image, Button → LevelCompleteController.ReloadCurrentScene)
        │   └── Label               (TextMeshProUGUI: "Replay")
        └── NextLevelButton         (Image, Button → LevelCompleteController.ReloadCurrentScene)
            └── Label               (TextMeshProUGUI: "Next Level")
```

`LevelCompleteController` lives on `Canvas`, **not** on `LevelCompletePanel` itself — a GameObject
that starts inactive never runs its own components' `OnEnable`/`Awake`, so a controller sitting on
the panel it's supposed to reveal could never receive `PaintCoverageTracker.Completed` in the first
place. This mirrors the existing `ContextualActionUI`-on-`ActionButtons` pattern: the controller
sits on a permanently-active parent and toggles its child's `activeSelf`.

## Runtime Dependency Hierarchy

Scripts are grouped by system; arrows show "raises event → consumed by" or "reads/calls →".
Leaf systems (bottom) have no gameplay dependents of their own.

### Movement & Camera

```
MobileInputReader (Input/)         CameraLookInput (Input/)
  │ MovementInput                    │ ConsumeLookDelta()
  ▼                                  ▼
PlayerMovementController (Player/) ThirdPersonOrbitCamera (Camera/)
```

### Interaction & Contextual UI

```
PlayerInteractionZone (Interaction/)   ← placed on PaintTank / target objects
  │ read by
  ▼
PlayerInteractionDetector (Interaction/)
  │ SelectedZoneChanged
  ▼
ContextualActionUI (UI/)  ──uses──▶ HoldActionButton (Input/)
  │ IsCurrentActionHeld
  ├──────────────┬───────────────┐
  ▼              ▼
PaintFillController (Paint/)   PaintGunFireController (Paint/)
```

### Paint Loop

```
PaintTank (Paint/) ──TakePaint()──▶ PaintFillController ──AddPaint()──▶ PaintGunReservoir (Paint/)
                                                                            │ PaintColorChanged / AmountChanged
                                                                ┌───────────┴───────────┐
                                                                ▼                       ▼
                                                     PaintGunVisual (Paint/)   PaintReservoirUI (UI/)

PaintGunFireController ──ConsumePaint()──▶ PaintGunReservoir
      │ raycast via
      ▼
PaintSprayer (Paint/) ──requires CanReceivePaint──▶ PaintSurfaceMarker (Paint/)
      │ SprayHitReceived (PaintSprayHit)
      ▼
PaintableSurface (Paint/) ──Graphics.Blit──▶ PaintBrush.shader (+ _BrushNoiseTex edge noise)
      │ masked by
      ▼
PaintTargetMaskProvider (Paint/) ──reads──▶ PaintTargetDefinition (ScriptableObject)
      │
      │ StampApplied (PaintStampData)
      ▼
PaintCoverageTracker (Paint/)
      │ OverallProgressChanged / RegionProgressChanged / Completed
      ▼
PaintProgressUI (UI/) ──instantiates──▶ PaintRegionProgressRow (UI/)
```

### Liquid Paint Polish & Target Guide

```
PaintTargetMaskProvider.RebuildMasks() (Awake / target change — never per-frame)
      │ same first-region-owns pass that builds the allowed masks
      ├──▶ allowedMasksByColorId[] (unchanged — clips PaintBrush.shader stamps)
      └──▶ GuideTexture (RGBA32: RGB = region DisplayColor, alpha = interior/boundary)
                │ MasksRebuilt event
                ▼
PaintableSurface.ApplyGuideTexture() ──MaterialPropertyBlock──▶ _TargetGuideTex / _HasTargetGuide
      │ (also sets _PaintTex, _BaseMap, _LiquidNoiseTex, _ImpactUV/_ImpactStartTime/_ImpactStrength
      │  — property block only, no runtime Material for the surface renderer)
      ▼
PaintableSurface.shader (per-pixel, Frag) — see "Jelly Paint Volume" below for the full pipeline
      └─ _TargetGuideTex, gated by _HasTargetGuide, faded by (1 - coverage) ──▶ pre-paint guide overlay

PaintSprayer.ShowImpact() ──on color change/first contact──▶ ImpactParticles.Emit(contactPulseParticleCount)
      (reuses the existing ImpactParticles system — droplet/splash tuning, shrinks over lifetime)
SprayParticles renderer.renderMode = Billboard, Local-space, Cone shape + Size-over-Lifetime —
      liquid-droplet stream (Velocity over Lifetime and Noise disabled — see "Particle & Reservoir
      HUD Flow" below for why; superseded the earlier Stretched-Billboard setup, which read as a
      laser beam)
```

### Particle & Reservoir HUD Flow

```
PaintGunReservoir.CurrentPaint
      │ (PaintColorChanged)
      ▼
PaintColorDefinition.DisplayColor
      │
      ├──▶ PaintSprayer.ApplyParticleColor() ──▶ SprayParticles.MainModule.startColor
      │        (Billboard, LOCAL simulation space, Cone shape, Size/Color-over-Lifetime —
      │         liquid droplets, not a laser; PaintDropletParticle.png + PaintSprayParticle.mat,
      │         alpha-blended, never additive. Velocity over Lifetime and Noise are DISABLED —
      │         Velocity's X/Y/Z curve modes were mismatched (TwoConstants/TwoConstants/Constant),
      │         which raised "Particle Velocity curves must all be in the same mode" every frame;
      │         Noise caused visible sideways drift. Local space — not World — is what keeps the
      │         stream visually attached to the muzzle while the character moves; already-emitted
      │         droplets in World space kept their old trajectory and appeared to bend/trail
      │         behind the moving gun.)
      │
      └──▶ (on a valid spray hit) PaintSprayer.ShowImpact() ──▶ ImpactParticles.MainModule.startColor
               (Billboard, WORLD simulation space — unchanged, must stay independent of the moving
               gun — wider cone, burst on contact, Velocity over Lifetime disabled, no mixed-mode
               curves — small wet paint splash)

PaintSprayer.TryGetValidHit() ──▶ RaycastHit (point, normal)
      │        (sprayOrigin.forward — unchanged by any particle-visual fix; PaintSprayer.cs never
      │         sets simulationSpace, velocity, noise, or particle-transform rotation)
      ▼
ImpactParticles.transform.SetPositionAndRotation(hit.point + normal*offset, LookRotation(normal))
      (ray misses ──▶ HideImpactVisual(), stream keeps emitting from the muzzle)

PaintGunReservoir.CurrentAmount / CurrentPaint
      │ (AmountChanged / PaintColorChanged)
      ▼
PaintReservoirUI.RefreshVisuals()
      ├──▶ PaintBarFill.fillAmount = reservoir.NormalizedAmount
      │        (Image: Type=Filled, Fill Method=Vertical, Fill Origin=Bottom, configured in code
      │         via ConfigureFillImage() on Awake/OnEnable — bottom-to-top fill, top-to-bottom
      │         drain while firing)
      ├──▶ PaintBarFill.color = reservoir.CurrentPaint.DisplayColor (or emptyColor if empty)
      ├──▶ PaintBarFill.enabled = hasVisiblePaint
      │        (false at ~0 amount — hides only the colored layer; the grey PaintBarBackground
      │         Image stays enabled=true always, so the empty container never disappears)
      ├──▶ PaintIcon.color = same color logic
      └──▶ AmountLabel.text = "{current} / {max}"

Canvas/PaintHUD
      ├── PaintIcon                              (Image, tinted by current paint)
      ├── PaintBarBackground                     (Image: Simple, grey, Source Image =
      │   │                                        PaintBarFillWhite.png, always enabled)
      │   └── PaintBarFill                       (Image: Filled/Vertical/Bottom, SAME
      │                                            PaintBarFillWhite.png sprite, tinted by
      │                                            DisplayColor — a separate Image component
      │                                            from the background, never shared)
      └── AmountLabel                            (TextMeshProUGUI)

Both PaintBarBackground and PaintBarFill require a real Source Image sprite for Image.Type=Filled
to render a visible clipped fill — a null sprite was the confirmed root cause of the bar not
visibly filling despite fillAmount being set correctly every frame.
```

### Jelly Paint Volume

```
PaintSprayHit (accepted, matching-colour)
      │
      ▼
PaintableSurface.HandleSprayHitReceived()
      ├──RegisterImpact(uv, opacity)──MaterialPropertyBlock──▶ _ImpactUV / _ImpactStartTime(Time.time) / _ImpactStrength
      └──StampAt(uv, paint, opacity, allowedMask)──Graphics.Blit──▶ PaintBrush.shader
                                                                          │
                                                                          ▼
                                                          dome = pow(saturate(1-distance01²), _JellyDomePower)
                                                          incomingThickness = dome * opacity * _AllowedMask (final gate)
                                                          newAlpha = SmoothMax(previous.a, incomingThickness, _BlobMergeSoftness)
                                                                     + previous.a*incomingThickness*_ThicknessBuildRate,
                                                                     clamped to _MaximumThickness
                                                                          │
                                                                          ▼
                                                     Paint RenderTexture (ping-ponged): RGB = paint colour,
                                                     Alpha = merged jelly thickness (NOT visible opacity)

Paint RenderTexture (per pixel, PaintableSurface.shader Frag)
      │
      ├─ coverage = smoothstep(_PaintCoverageStart, _PaintCoverageFull, thickness)   ← visible opacity curve
      ├─ local 4-tap gradient (1 texel)  ──▶ fine meniscus edge detail
      ├─ wide  4-tap gradient (_JellySmoothingRadius texels) ──▶ broad puddle mound
      │        combined gradient ──▶ jellyNormal (fake raised normal)
      ├─ _LiquidNoiseTex sampled twice (different scale/speed/direction)
      │        noiseA, noiseB ──▶ jellyNormal perturbation + highlight variation
      │        abs(noiseA-noiseB) ──▶ soft internal cloudy pattern (coverage-gated)
      ├─ _ImpactUV/_ImpactStartTime/_ImpactStrength + _Time.y
      │        ──▶ localized decaying ripple ──▶ jellyNormal + specular only (never mask/colour/progress)
      ├─ jellyNormal ──▶ broad + sharp specular, Fresnel rim, depth darkening (GetMainLight)
      ├─ local gradient + coverage ──▶ meniscus rim (_MeniscusWidth/Strength/Smoothness/Tint)
      └─ final = lerp(baseColor, paint.rgb, coverage) + specular + fresnel + meniscus + internal glow
                 then blended with the target-guide overlay (above)

ClearPaint() ──▶ clears both RenderTextures (thickness→0), ResetImpactRipple() (_ImpactStrength=0),
                 PaintCleared event ──▶ PaintCoverageTracker.ResetProgress() (unchanged)
```

### Material Configuration

```
Assets/ColorGame/Materials/PaintableSurface_Test.mat
      │ shader = ColorGame/PaintableSurface
      ├─ stores the shared jelly baseline parameters (see README "Current production baseline
      │  values") — these are shared, static tuning knobs, not per-instance runtime state
      ├─ _PaintTex / _TargetGuideTex left as safe empty/default slots on the asset itself —
      │  never a painted image, never a manually-assigned guide texture
      └─ assigned to PaintTarget_Test/PaintSurface's MeshRenderer

Runtime (per PaintSurface instance, MaterialPropertyBlock only — no renderer.material, no
material instance):
      PaintableSurface.cs ──▶ _PaintTex (generated Paint RenderTexture)
      PaintableSurface.cs ──▶ _TargetGuideTex / _HasTargetGuide (from PaintTargetMaskProvider)
      PaintableSurface.cs ──▶ _LiquidNoiseTex (assigned once, Awake)
      PaintableSurface.cs ──▶ _ImpactUV / _ImpactStartTime / _ImpactStrength (per accepted hit)

Manual edits to the material's baseline values (dome/jelly/meniscus/lighting/noise/ripple tuning)
never touch PaintCoverageTracker, PaintTargetMaskProvider, or target-region ownership — those stay
entirely on the C# side and are unaffected by shader/material tuning.
```

### Level Completion (Stage 10)

```
PaintCoverageTracker.Completed
      │ (fires once — latched internally by overallCompletionRaised)
      ▼
LevelCompleteController (on Canvas)
      │ IsLevelComplete guard — ignores any further Completed calls
      ├──SetMovementEnabled(false)──────────▶ PlayerMovementController (Player/)
      ├──SetMovementInputEnabled(false)─────▶ MobileInputReader (Input/)
      ├──SetCameraInputEnabled(false)───────▶ ThirdPersonOrbitCamera (Camera/)
      ├──enabled = false────────────────────▶ PaintFillController   (OnDisable stops fill safely)
      ├──enabled = false────────────────────▶ PaintGunFireController (OnDisable stops spray/impact)
      ├──enabled = false────────────────────▶ ContextualActionUI    (OnDisable force-releases buttons)
      │
      ▼
LevelCompletePanel.SetActive(true)  (re-asserted as last Canvas sibling)
      │
      ├──ReplayButton.onClick─────┐
      └──NextLevelButton.onClick──┴──▶ LevelCompleteController.ReloadCurrentScene()
                                              │ reloadRequested guard — ignores a second click
                                              ▼
                                   SceneManager.LoadScene(activeScene.buildIndex)
```

`LevelCompleteController`'s serialized references: `tracker` (`PaintCoverageTracker` on
`PaintTarget_Test/PaintSurface`), `levelCompletePanelRoot` (`LevelCompletePanel`), `replayButton` /
`nextLevelButton` (their `Button` components), and the optional gameplay-lock references
(`playerMovementController`, `mobileInputReader`, `orbitCamera`, `fillController`,
`fireController`, `contextualActionUI`) — all on `Player`, `Main Camera`, or `Canvas/ActionButtons`
as listed elsewhere in this document.

### Character Animation (Stage 11; locomotion simplified 2026-07-30 after a frozen-animation regression)

> The Base Layer went through three designs: a Blend Tree (made partial input play a weak blend),
> then a fixed set of states with a `Move`-state Speed Parameter called `LocomotionPlaybackSpeed`
> (a plausible cause of the walk animation freezing entirely — static inspection could not find a
> definitive single cause, but this mechanism was the most likely suspect and was removed), and
> now this final bool-only, fixed-speed design. `MoveSpeed` and `LocomotionPlaybackSpeed` no longer
> exist anywhere in the controller or scripts.

```
PlayerMovementController.HorizontalSpeed
      │ (read-only property, world m/s — NOT normalized, no speed/playback-rate use anymore)
      ▼
PlayerAnimationController (Player/)
      │ hysteresis: startMovingSpeed (0.05) to begin moving, stopMovingSpeed (0.02) to stop —
      │ prevents Idle/Move flicker near zero speed
      ▼
      │ animator.SetBool("IsMoving", shouldMove)     [only on an actual state change]
      ▼
Animator (MaleCharacterPlayer instance) ── Base Layer: plain Idle/Move states (no blend tree,
      │                                     no speed parameter)
      │   Idle (Idle_Normal_SwordAndShield, default, speed=1) ──IsMoving==true, no exit time,
      │         0.05s fixed──▶ Move
      │   Move (MoveFWD_Normal_InPlace_SwordAndShield, speed=1 FIXED, Speed Parameter DISABLED)
      │         ──IsMoving==false, no exit time, 0.08s fixed──▶ Idle
      ▼
Character skeleton (all bones, Base Layer — implicitly full weight; Unity ignores/doesn't expose
a weight control for layer 0). Animator.cullingMode = AlwaysAnimate (changed from
CullUpdateTransforms as a defensive measure against off-screen-culling freezing bone transforms).

PaintGunFireController.FireStarted / FireStopped
      │
      ▼
PlayerAnimationController
      │ animator.SetBool("IsSpraying", true/false)
      ▼
Animator ── RightArmSpray Layer (Override, weight 1, masked by RightArmSpray.mask)
      │        Empty ──IsSpraying==true, 0.08s──▶ SprayDefend (Defend_SwordAndShield, mirrored)
      │        SprayDefend ──IsSpraying==false, 0.12s──▶ Empty
      ▼
Right arm/fingers bones only (mask excludes everything else — left arm and legs keep
following the Base Layer's idle/walk pose uninterrupted) ──▶ RightHandWeaponSocket ──▶
PaintGun (+ Cosmic_Retro_Blaster_1) follows the animated hand every frame
```

`PlayerAnimationController`'s serialized references: `animator` (on the `MaleCharacterPlayer`
instance), `movementController` (`Player`'s `PlayerMovementController`), `fireController`
(`Player`'s `PaintGunFireController`) — all on `Player` or its `PlayerVisualRoot` subtree, no
`GameObject.Find`/`FindObjectOfType` anywhere in the chain. Final gameplay Animator parameter set:
`IsMoving` (bool) and `IsSpraying` (bool) only — `MoveSpeed` (the original Blend Tree parameter)
and `LocomotionPlaybackSpeed` (the since-removed `Move`-state speed parameter) no longer exist
anywhere in the controller or scripts (see [CHANGELOG.md](CHANGELOG.md), 2026-07-30).

### Paint Particle Color (fixed 2026-07-30)

```
PaintGunReservoir.CurrentPaint
      │ (PaintColorDefinition)
      ▼
PaintColorDefinition.DisplayColor ──┬──▶ PaintGunVisual (PaintContainerVisual's tint/fill, unchanged)
                                     │
                                     └──▶ PaintSprayer.ApplyParticleColor
                                              │ ParticleSystem.MainModule.startColor = DisplayColor
                                              ▼
                                  SprayParticles / ImpactParticles
                                  (ParticleSystemRenderer.sharedMaterial =
                                   Assets/ColorGame/Materials/Particles/PaintSprayParticle.mat —
                                   Universal Render Pipeline/Particles/Unlit, Base Color WHITE,
                                   Base Map = Assets/ColorGame/Textures/Particles/SoftPaintParticle.png)
```

The container and both particle systems all read the same `PaintColorDefinition.DisplayColor` —
none of them sample each other's rendered color or compare by name. The particle material's white
Base Color means it never tints `startColor`; before this fix, both renderers had **no material
assigned at all**, which Unity renders with its built-in magenta "missing material" fallback (not
a shader/texture problem). `PaintSprayer.BeginSprayVisual` stops-and-clears `SprayParticles` once
per firing-session start (not every frame); `ShowImpact` stops-and-clears `ImpactParticles` only
when the active paint color actually differs from the previous hit-frame's — so a color switch
between red and blue can't leave stale-color particles on screen.

### Shared Data Types

- `PaintColorId` (enum) — used by `PaintColorDefinition`, `PaintTargetDefinition.Region`,
  `PaintCoverageTracker`, `PaintTargetMaskProvider`.
- `InteractionActionType` (enum) — used by `PlayerInteractionZone`, `ContextualActionUI`,
  `PaintFillController`, `PaintGunFireController`.
- `PaintSprayHit` / `PaintStampData` / `PaintRegionProgress` (readonly structs) — data carried
  across the arrows above without exposing extra `UnityEngine.Object` references.

### Designer Data (ScriptableObjects)

```
PaintColorDefinition (ScriptableObject)   PaintTargetDefinition (ScriptableObject)
  RedPaint / BluePaint / YellowPaint         HeartTarget_Red / HeartTarget_RedBlue
        │                                          │
        └──────────────┬───────────────────────────┘
                        ▼
        Referenced by PaintTank, PaintTargetMaskProvider, PaintCoverageTracker
```
