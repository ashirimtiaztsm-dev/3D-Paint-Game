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
