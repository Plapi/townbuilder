# Project Instructions

- Do not run build, compile, test, Unity, dotnet, msbuild, npm, or validation commands unless the user explicitly asks.
- After editing files, only summarize what changed.
- It is okay to use read-only inspection commands like rg, sed, git diff, and git status.
- Do not modify scene files unless explicitly requested.

# Codex Project Guide

This file is the first source of truth for Codex sessions in this repo. Keep it compact so future answers can be faster, cheaper, and better aligned with the project.

## Project Snapshot

- Project type: Unity game project.
- Unity version: `6000.3.7f1`.
- Main namespace: `com.Plapamaru.TownCrafter`.
- Core gameplay: a grid-based factory/town-builder loop with placeable entities, resource items, conveyors, extractors, crafters, construction sites, UI panels, and save/load through `PlayerPrefs`.
- Render/UI stack: URP, uGUI, TextMesh Pro, DOTween, UniTask, mobile touch camera support.
- Package highlights: Unity MCP, Input System, AI Navigation, URP `17.3.0`, Shader Graph, Unity Test Framework.

## Repository Layout

- `Assets/Scripts/Factory`: factory simulation, map/grid logic, save data, entities, resource items, editor placement helpers.
- `Assets/Scripts/GameSystem`: state machine for gameplay modes such as main, build, delete, entity info, conveyor/crafter/extractor placement.
- `Assets/Scripts/UI`: uGUI panel system, UI content components, resource/crafting widgets.
- `Assets/Scripts/Editor`: custom editor tooling and debug windows.
- `Assets/Scripts/Layers`: project layer helpers.
- `Assets/Data`: ScriptableObject data for entities, resource items, resource nodes, construction props, and editor catalogs.
- `Assets/Graphic`: prefabs, materials, textures, and factory visuals.
- `Assets/Resources`: runtime-loaded singleton/config assets such as `FactoryConfig.asset` and DOTween settings.
- `Packages/manifest.json`: Unity package dependencies.
- `ProjectSettings`: Unity project settings.

## Architecture Notes

### Game State Flow

- `GameSystem` is a `MonoBehaviourSingleton<GameSystem>` that discovers child `GameStateBase` components and manages a stack of states.
- States are async and UniTask-based. Each state implements `Run(CancellationToken)` and `Exit(CancellationToken)`.
- `GameState<TContext>` stores typed context data for the active state.
- `GameStateBase.BaseRun` races wait/process tasks and uses cancellation tokens heavily.
- `GameSystem.Start()` initializes the factory and enters `GameStateMain`.
- Pressing `R` during play saves the factory and reloads the active scene.

### Factory Placement And Map

- `FactorySystem` coordinates entity initialization, placement, rotation, conveyor connections, save/load, and simulation pause during build mode.
- `FactoryMap` is a singleton grid registry: `Dictionary<Vector2Int, Entity> Map`.
- An `Entity` can occupy multiple grid cells through `GridPositions`, `Size`, `Forward`, and `Right`.
- Placement updates remove and re-add occupied cells, then call `entity.OnPlacementUpdate()` and refresh debug cells.
- Factory entities usually move from `LayerType.Interactable` while previewing/building to `LayerType.Environment` after confirmation.
- Entity prefabs are instantiated through `ObjectPoolingSystem.Instance.GetObject<T>(id, parent)`.

### Entities

- Base class: `Entity`.
- Factory base class: `FactoryEntity`.
- Typed factory base: `FactoryEntity<TData, TSaveData>`.
- Main factory entity implementations live in `Assets/Scripts/Factory/Entities/FactoryEntities`.
- Important types include `Extractor`, `Conveyor`, `ConveyorStraight`, `ConveyorCorner`, `Crafter`, and `Construction`.
- `EntityData`, `ResourceNodeData`, `ConstructionData`, and `ResourceItemData` are ScriptableObject data types.

### Save System

- `FactorySaveSystem` serializes `SaveData` to `PlayerPrefs` using key `FactoryConstants.FACTORY_SAVE_KEY`.
- Saved entity lists include extractors, conveyors, crafters, and constructions.
- Runtime resource items are saved through `ResourceItemSaveData`.
- `FactorySystem.InitEntities` first registers static scene entities, then instantiates saved dynamic entities, then applies construction save data.

### UI

- `UISystem` is a singleton that discovers child `UIPanelBase` components, stores them by type, and initially deactivates them.
- Panels live under `Assets/Scripts/UI/Panels`.
- Build UI uses `UIBuildPanel` and `UIButtonType` for confirm, rotate, and close actions.
- DOTween is used for UI fades; UniTask cancellation tokens are used for async UI operations.

### Rendering Layers And Cameras

- Custom render layers include `Terrain`, `Ground`, `Grid`, `Environment`, `Interactable`, and `Highlight`.
- `Terrain` is Unity layer index `10` and is represented in `LayerType`.
- `Assets/Scenes/Game/Game.unity` uses URP camera stacking for gameplay layers. Current world render order is `Terrain -> Ground -> Grid -> Environment -> Interactable -> Highlight`.
- Overlay cameras are children of `MainCamera` and use `OverlayCameraSync` to copy camera lens settings from the base camera.
- Keep scene terrain objects such as `Terrain`, `FlatTerrain0`, `FlatTerrain1`, and `River` on the `Terrain` layer so `OverlayCamera_Terrain` renders them.

### Data And Configuration

- `FactoryConfig` is a `ScriptableObjectSingleton<FactoryConfig>` and is stored in `Assets/Resources/FactoryConfig.asset`.
- Entity/resource data assets live under `Assets/Data`.
- Object IDs matter because pooling and save/load refer to IDs such as entity/resource IDs. Preserve IDs unless a migration is part of the task.

## Coding Conventions

- Use the existing namespace style: `com.Plapamaru.TownCrafter.<Area>`.
- Keep Unity serialized fields private with `[SerializeField]`.
- Favor UniTask and cancellation tokens for async gameplay/UI flows.
- Use existing singleton and utility APIs from `com.Plapamaru.Singletons`, `com.Plapamaru.Utilities`, and related project utilities.
- Use `Vector2Int` grid coordinates for factory map logic and convert world/grid positions through `FactoryUtils`.
- Keep placement, map occupancy, and highlight behavior in sync by using `FactorySystem`/`FactoryMap` APIs instead of directly mutating transforms where possible.
- For pooled objects, release through `ObjectPoolingSystem.Instance.ReleaseObject`.
- Avoid broad refactors in gameplay systems unless specifically requested.

## Common Task Hints

- Adding a new buildable entity usually touches the entity class, data asset, prefab/visual, UI/build state, and save data if it has runtime state.
- Non-optimized construction editor creation is in `Assets/Scripts/Editor/Factory/ConstructionEditor.cs`; it creates typed constructions from existing numbered `ConstructionData` assets. Current prefixes include `House`, `Shop` for commercial, `Road`, `Church`, and `Park`, and construction inputs should be `Inputs/Input0/UIArrow` and `Inputs/Input1/UIArrow`.
- Adding a new resource item usually touches `ResourceItemType`, a `ResourceItemData` asset, and any recipes or construction requirements that consume it.
- Build-mode behavior usually belongs in `GameStateBuild*` classes and should use `FactorySystem.OnBuildEnter/OnBuildExit`.
- Entity inspection or state transitions usually belong in `GameSystem` states, not directly in UI components.
- UI panel changes usually belong in `Assets/Scripts/UI/Panels` plus related content widgets.

## Safe Inspection Commands

Use these for context gathering:

```sh
rg --files Assets/Scripts
rg "class FactorySystem|class GameSystem|class UISystem" Assets/Scripts
sed -n '1,220p' Assets/Scripts/Factory/FactorySystem.cs
sed -n '1,220p' Assets/Scripts/GameSystem/GameSystem.cs
git status --short
git diff --stat
```

Do not run validation/build/test commands unless the user explicitly asks.
