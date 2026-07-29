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
PaintableSurface (Paint/) ──Graphics.Blit──▶ PaintBrush.shader
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

### Character Animation (Stage 11)

```
PlayerMovementController.horizontalVelocity
      │ NormalizedHorizontalSpeed (read-only property, damped by PlayerAnimationController)
      ▼
PlayerAnimationController (Player/)
      │ animator.SetFloat("MoveSpeed", ..., movementDampTime, Time.deltaTime)
      ▼
Animator (MaleCharacterPlayer instance) ── Base Layer: 1D Blend Tree on MoveSpeed
      │                                     0 → Idle_Normal_SwordAndShield
      │                                     1 → MoveFWD_Normal_InPlace_SwordAndShield
      ▼
Character skeleton (all bones, Base Layer weight 1, Override)

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
`GameObject.Find`/`FindObjectOfType` anywhere in the chain.

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
