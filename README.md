# 3D Paint Game

A mobile-friendly 3D game built in Unity where the player drives/walks a character around an
arena, painting the ground as they move. Movement is driven by an on-screen joystick, with a
third-person follow camera and juicy hit-shake feedback via DOTween.

## Tech Stack

| Area | Choice |
|---|---|
| Engine | Unity 6000.x (Unity 6) |
| Render pipeline | Universal Render Pipeline (URP) 17.1.0, with separate PC and Mobile render assets |
| Input | Player movement via a virtual joystick ([Joystick Pack](Assets/#Paint%20Game/Joystick%20Pack)); Unity Input System package also installed with `InputSystem_Actions.inputactions` |
| Tweening / FX | [DOTween / DOTween Pro](Assets/Plugins/Demigiant) (camera hit-shake) |
| UI Text | TextMesh Pro |
| Navigation | AI Navigation package (NavMesh) |
| Editor tooling | Unity MCP plugin (`com.ivanmurzak.unity.mcp`) — lets an AI agent drive the Unity Editor directly (see `.mcp.json`) |

## Project Structure

```
Assets/
├── #Paint Game/            Game-specific code and content (the "#" prefix pins the folder to the top of the Project window)
│   ├── PlayerController.cs Joystick-driven CharacterController movement + gravity
│   ├── CameraFollow.cs     Third-person follow/look camera + DOTween hit-shake
│   ├── PlayerMaterial.mat, BlackRoad.mat, black.jpg
│   └── Joystick Pack/      Third-party on-screen joystick asset (Fixed/Floating/Dynamic/Variable joysticks)
├── Scenes/
│   └── SampleScene.unity   Main/only scene
├── Settings/                URP render pipeline assets & volume profiles (PC + Mobile variants)
├── TextMesh Pro/            TMP runtime resources (fonts, shaders, default sprite asset)
├── Plugins/
│   ├── Demigiant/           DOTween & DOTween Pro
│   └── NuGet/                Managed DLLs pulled in for the Unity MCP plugin (SignalR client, Roslyn, etc.)
├── TutorialInfo/             Unity's default "Readme" template assets (safe to remove)
└── InputSystem_Actions.inputactions
```

See [ASSET_CATALOG.md](ASSET_CATALOG.md) for a full inventory of every asset in the project.

## Core Gameplay Scripts

- **[PlayerController.cs](Assets/#Paint%20Game/PlayerController.cs)** — reads horizontal/vertical
  axes from a `VariableJoystick`, moves the player via `CharacterController`, rotates the player to
  face the movement direction, and applies simple gravity.
- **[CameraFollow.cs](Assets/#Paint%20Game/CameraFollow.cs)** — smoothly follows and looks at the
  player (`Lerp`/`Slerp`), and exposes `PlayHitShake()` which uses DOTween (`DOShakePosition`) for
  a camera shake effect on hits.

## Getting Started

1. Open the project in **Unity 6000.x** (Unity Hub → Add → select this folder).
2. Let Unity resolve packages (`Packages/manifest.json`) on first open.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press Play — use the on-screen joystick to move the player.

## Notable Packages (`Packages/manifest.json`)

- `com.unity.render-pipelines.universal` — URP
- `com.unity.ai.navigation` — NavMesh
- `com.unity.inputsystem` — new Input System
- `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.ugui`
- `com.ivanmurzak.unity.mcp` — Unity MCP plugin (AI agent ↔ Editor bridge), served via the
  `package.openupm.com` scoped registry
