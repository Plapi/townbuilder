# Factory Guide For Codex

This guide documents the factory subsystem. Use it when working under `Assets/Scripts/Factory` or on construction/editor tooling that feeds factory prefabs.

## Scope And Rules

- Follow the root `AGENTS.md` first.
- Do not run Unity, builds, tests, dotnet, msbuild, npm, or validation commands unless the user explicitly asks.
- Do not modify scene files unless explicitly requested.
- Prefer read-only inspection with `rg`, `sed`, `git diff`, and `git status`.
- Avoid editing generated/exported prefab assets unless the task explicitly asks for asset changes.
- Preserve Unity `.meta` files when moving or renaming assets.

## Folder Map

- `FactorySystem.cs`: high-level factory coordinator for init, placement, rotation, conveyor connection, save, and build pause/resume.
- `FactoryMap.cs`: singleton grid occupancy registry, entity lookup, entity instantiation, pathfinding entry points, debug-cell refresh.
- `FactoryUtils.cs`: world/grid conversion, placement offsets, mouse-grid lookup, adjacency helpers, input feed direction helpers.
- `Entities/Entity.cs`: base grid entity, size/rotation/grid occupancy support, input/output activation, process-loop lifecycle.
- `Entities/FactoryEntities/FactoryEntity.cs`: pooled runtime factory entity base, save-data hooks, resource passing, highlight object lifecycle.
- `Entities/FactoryEntities`: extractor, conveyor, crafter, conveyor helper/type classes.
- `Entities/Construction.cs`: construction runtime state, resource intake, progress/stage switching, save data.
- `Entities/Data`: ScriptableObject entity data types.
- `ResourceItem`: resource item runtime/data/type classes.
- `Save/FactorySave.cs`: PlayerPrefs save/load model.
- `EditorTools`: editor-only construction prop/fence placement helpers and catalogs.

## Runtime Architecture

### Placement And Grid Occupancy

- `FactorySystem.Place`, `PlaceOnCenter`, and `Rotate` are the normal entry points for moving entities on the grid.
- `FactorySystem.SetEntity` removes old occupancy, fills `FactoryMap.Map` for every occupied cell, calls `OnPlacementUpdate`, then refreshes debug cells.
- `FactoryMap.Map` maps each occupied `Vector2Int` cell to the occupying `Entity`. Multi-cell entities appear in multiple map entries.
- `Entity.Size`, `Right`, `Forward`, `GridPos`, and `GridPositions` define occupied cells.
- Use `FactoryUtils.WorldToGrid`, `FactoryUtils.GetGridPos`, and `FactoryUtils.PlaceToGrid` instead of hand-rolled grid math.
- Placement validity starts in `Entity.CheckIsCorrectlyPlaced`: all cells in the entity footprint must be occupied by that entity.

### Entity Lifecycle

- `Entity.Init(CancellationToken)` creates an internal CTS, calls `OnInit`, subscribes to `SimulationClock.OnPaused`, and starts `ProcessLoop`.
- `ProcessLoop` returns `true` to keep running and `false` to stop.
- `OnDispose` must clear runtime state, unsubscribe, release pooled children, and cancel/dispose internal tokens.
- Factory entities are pooled through `ObjectPoolingSystem`; release them with `ObjectPoolingSystem.Instance.ReleaseObject` or `FactorySystem.Release`.
- Preview/build entities usually use `LayerType.Interactable`; confirmed entities switch to `LayerType.Environment`.
- Inputs/outputs are hidden after placement confirmation through `Entity.OnConfirmPlacement`.

### Save And Load

- `FactorySaveSystem.Save` serializes unique entities from `FactoryMap.Instance.Map` to `PlayerPrefs`.
- Save key is `FactoryConstants.FACTORY_SAVE_KEY`.
- Runtime save lists: extractors, conveyors, crafters, constructions.
- `FactorySystem.InitEntities` registers static scene entities, loads saved dynamic entities, restores construction data, then initializes static entities.
- `FactoryEntity<TData, TSaveData>.ToSaveData` stores id, grid position, rotation, and held resource item.
- `SetSaveData` should only accept the expected save-data type and should not perform heavy runtime work; initialization belongs in `OnInit`.

## Main Entity Types

### Extractor

- Produces a `ResourceItem` when one of its inputs overlaps a `ResourceNode`.
- Waits for simulation to be unpaused, extraction time to complete, and an output conveyor to accept the resource.
- Saves `_currentExtractTime`.
- Placement is valid only if the base footprint is valid and at least one input touches a `ResourceNode`.

### Conveyor

- Moves a held resource along subclass-provided distribution points.
- Connects to another conveyor through `_nextConveyor`/`_prevConveyor` or to a feed target such as `Construction`/`Crafter`.
- Saves next conveyor grid position, belt direction, pillar active state, and held resource.
- `FactorySystem.MakeConveyorsConnexions` handles straight/corner replacement logic.
- Always release allowed highlight objects in `OnConfirmPlacement` and `OnDispose`.

### Crafter

- Stores input resource counts internally.
- Finds recipes from `FactoryConfig.Instance.crafterRecipes`.
- Crafts only when it has enough inputs and an output conveyor can accept the output.
- Uses `SimulationClock.DeltaTime` and pauses animators when simulation is paused.

### Construction

- Runtime state: `NotStarted`, `InProgress`, `Finished`.
- Required resources come from `ConstructionData.requiredResources`.
- Accepts incoming resources only while `InProgress` and only when that resource is still needed.
- Consumed incoming resources are released immediately, then counted in `_resourcesDict`.
- Stage selection:
  - `NotStarted`: stage `0`.
  - `InProgress`: interpolated middle stages.
  - `Finished`: last stage.
- Save data stores state and delivered resource counts; held resource is intentionally not saved.

## Data And Config

- `FactoryConfig` is a `ScriptableObjectSingleton<FactoryConfig>` stored in `Assets/Resources/FactoryConfig.asset`.
- `FactoryConfig.correctColor`, `wrongColor`, and `previewColor` drive placement/highlight visuals.
- `FactoryConfig.constructionPropsScale` is shared by construction editor prop/fence generation.
- `FactoryConfig.crafterRecipes` is the recipe source for `Crafter`.
- Resource IDs generally come from `ResourceItemType.ToString()`.
- Entity/object IDs are used by pooling and save/load; changing them can break saved data.

## Construction Editor Pipeline

The construction export pipeline is implemented in `Assets/Scripts/Editor/Factory/ConstructionEditor.cs` as a custom inspector for `Construction`.

### Source Prefab Requirements

- The `Construction` must assign `_exportFolder` in the inspector.
- The root must have direct children named exactly:
  - `Graphic`
  - `Inputs`
- `Graphic` children must be named sequentially:
  - `Stage0NotOptimized`
  - `Stage1NotOptimized`
  - `Stage2NotOptimized`
  - and so on, with no gaps or duplicates.
- Each stage may contain:
  - optional `Ground`
  - required `Environment`
- No other direct children are allowed under a stage.
- Each `Environment` must contain at least one `MeshFilter` with a `MeshRenderer` and mesh.
- Unity layers named `Environment` and `Ground` must exist.

### Export Output

Clicking `Export Construction` creates a folder under `_exportFolder` named after the construction root.

Generated structure:

```text
<ExportFolder>/<ConstructionName>/
  <ConstructionName>NotOptimized.prefab
  <ConstructionName>Optimized/
    <ConstructionName>.prefab
    Stage0/
      EnvironmentMesh.mesh
      Environment.prefab
      Stage0.prefab
    Stage1/
      EnvironmentMesh.mesh
      Environment.prefab
      Stage1.prefab
```

### What Export Does

- Saves the original source root as `<ConstructionName>NotOptimized.prefab`.
- For each stage:
  - Combines meshes under `StageNNotOptimized/Environment` into `EnvironmentMesh.mesh`.
  - Saves a combined `Environment.prefab`.
  - Creates `StageN.prefab` containing optional `Ground` plus optimized `Environment`.
  - Sets generated `Ground` children to layer `Ground`.
  - Sets generated `Environment` children to layer `Environment`.
- Creates the optimized root prefab:
  - Rebuilds `Graphic`.
  - Copies `Inputs`.
  - Instantiates optimized stage prefabs under `Graphic`.
  - Assigns the generated stage objects into `Construction._stages`.
  - Clears `_exportFolder` on the optimized prefab.
- Existing generated assets at the same paths are deleted/replaced before writing.

### Important Construction Editor Notes

- The source hierarchy and naming are part of the contract; changing them requires updating `ConstructionEditor` validation and export code together.
- `Construction._stages` is the runtime array used by `Construction.UpdateStages`; export is responsible for assigning it on the optimized prefab.
- The optimized root should be the runtime prefab used by the game.
- The not-optimized prefab is for source/editing, not runtime efficiency.
- The editor uses `MeshUtils.Combine`; inspect that utility before changing mesh-combine behavior.

## Construction Prop And Fence Editor Tools

These live under `Assets/Scripts/Factory/EditorTools` and are editor helpers attached to construction prefab children.

### Props

- `ConstructionPropsPlacer` randomly places props inside a construction footprint.
- Catalog type: `ConstructionPropsCatalog`.
- Default catalog lookup prefers asset named `ConstructionPropsCatalog`.
- Generated children end with ` (Generated Construction Prop)`.
- Placement uses mesh renderer footprint bounds, optional 90-degree rotation, non-overlapping rects, and `FactoryConfig.constructionPropsScale`.

### In-Progress Fences

- `ConstructionFencePlacer` places selected/randomized fence prefabs around the construction perimeter.
- Catalog type: `ConstructionFenceCatalog`.
- Generated children end with ` (Generated Fence)`.
- Fence length is measured from prefab mesh bounds, with a default fallback length.
- Uses construction `Right`, `Forward`, and `Size` to place each side.

### Not-Started Rope Fence

- `ConstructionNotStartedFencePlacer` creates corner sticks and rope segments around the construction footprint.
- Generated children end with ` (Generated Not Started Fence)`.
- Rope segments are split by `_maxRopeLength` and scaled along local Z based on measured prefab length.

## Common Changes

- New buildable entity:
  - Add/modify entity class under `Entities/FactoryEntities` or `Entities`.
  - Add data asset under `Assets/Data/EntityData`.
  - Ensure prefab ID matches pooling/save expectations.
  - Add save data if runtime state must persist.
  - Update build state/UI outside this folder if it needs a new placement flow.
- New construction:
  - Create/edit not-optimized source prefab with the required `Graphic`, `Inputs`, and `StageNNotOptimized` hierarchy.
  - Configure `ConstructionData.requiredResources`.
  - Run `Export Construction` in Unity only when explicitly asked or when the user is doing asset work interactively.
  - Use the optimized prefab as runtime output.
- New construction prop/fence catalog item:
  - Add prefab reference to the appropriate catalog asset under `Assets/Data/Editor/Catalogs`.
  - Use the placer inspector buttons in Unity to regenerate source prefab children.
- Save-affecting entity change:
  - Update the relevant save-data type in `Save/FactorySave.cs`.
  - Update `ToSaveData`, `SetSaveData`, and `OnInit`.
  - Think about old save compatibility before renaming IDs or fields.

## Safe Inspection Commands

```sh
rg --files Assets/Scripts/Factory
rg "class Construction|class FactorySystem|class FactoryMap" Assets/Scripts/Factory Assets/Scripts/Editor/Factory
sed -n '1,260p' Assets/Scripts/Factory/Entities/Construction.cs
sed -n '1,320p' Assets/Scripts/Editor/Factory/ConstructionEditor.cs
git diff -- Assets/Scripts/Factory Assets/Scripts/Editor/Factory
```
