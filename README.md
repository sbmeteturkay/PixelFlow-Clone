# PixelFlow Clone

A Unity puzzle game clone built as a portfolio project. Colored shooters orbit a pixel art grid and fire at matching colored cells to reveal the image.

https://github.com/user-attachments/assets/aa0ba6c5-7388-4db1-8962-d090d69c62e3

> ⚠️ This repository contains only the code.
> Art assets, levels, and third-party packages are not included.
> The project will not compile as-is.
---

## Gameplay

- Click a shooter in the waiting area to launch it onto the spline
- Shooters automatically fire at matching colored cells as they orbit the grid
- Clear all cells to complete the level
- Shooters that complete a full orbit move to the slot area
- Game over if the slot area fills up

---

## Architecture

### Grid System

`PixelGrid` is the core of the game. It builds the cell grid from a `PixelArtData` ScriptableObject, manages cell state, and exposes a clean query API to the shooter system.

Shooters never access cells directly. They ask the grid: *"what is the frontmost alive cell on this edge at this line index?"* — and the grid answers. This keeps the shooter system completely decoupled from grid internals.

Hit detection is also centralized in `PixelGrid.HandleHit`, which tracks destroyed cell counts and fires `OnLevelComplete` when all cells are cleared.

### Shooter System

Three areas manage shooter lifecycle: **Waiting → Spline → Slot**.

`ShooterManager` owns capacity rules and state transitions. Shooters call into it to request entry; the manager decides whether to allow it. This means shooters have no knowledge of each other or of capacity limits.

Movement is handled via **PrimeTween** — the spline animation runs as a `Tween.Custom` over spline length, which gives frame-rate-independent, smooth movement without a physics or Update loop.

### Firing — Line Sweep

The firing system guarantees no cell is ever skipped, regardless of frame rate or shooter speed.

Each frame, the shooter checks which grid line it currently occupies. If it has moved across multiple lines since the last frame, it sweeps through every line in between and fires at each one. This makes the mechanic completely independent of physics, Update timing, and spline speed.

```
prev frame: lineIndex = 3
this frame: lineIndex = 6
→ fires at lines 4, 5, 6
```

Corner detection uses a geometric approach: if the shooter is between the left/right boundaries it must be on the top or bottom edge, and vice versa. No threshold tuning needed.

### Color Clustering

Pixel art assets often contain near-identical colors. `LevelCreationExtensions.BuildColorClusters` groups palette colors within a tolerance into clusters. Each cluster gets an average color used both for the shooter visual and the cell visual — ensuring they always match.

### Object Pooling

Both shooters and bullets use `UnityEngine.Pool.ObjectPool<T>`. Pools are owned by dedicated singleton managers (`ShooterPool`, `BulletPool`). Objects signal their own release via `Action<T>` callbacks, keeping pool logic out of the pooled objects themselves.

### Level Creation Pipeline

`PixelFlowLevelCreator` is an editor tool window (`Tools > PixelFlow > Level Creator`) that automates the full pipeline:

1. Import one or more textures via drag & drop
2. Convert each texture to a `PixelArtData` asset (palette extraction, pixel indexing)
3. Generate a `LevelData` asset with shooter data derived from color cluster analysis
4. Save all assets to configured paths

This makes creating hundreds of levels from pixel art assets a matter of seconds.

---

## Technical Stack

- **Unity** 6.3+
- **PrimeTween** — tween engine for all animations
- **Unity Splines** — shooter orbit path

---

## Project Structure

```
Assets/_Project/
  _Scripts/
    Core
      Systems/       LivesSystem, GoldSystem
    Feature
      Grid/          PixelGrid, PixelCell, PixelArtData
      Shooter/       Shooter, ShooterManager, ShooterPool, BulletPool
      Level/         LevelData, LevelManager
        Editor/        PixelFlowLevelCreator, PixelArtDataEditor, LevelDataEditor
    
```

---

## Key Design Decisions

**Query-based targeting over physics.** No raycasts, no colliders. Shooters ask the grid for their target mathematically. This is faster, deterministic, and easier to reason about.

**Line sweep over interval firing.** Timer-based firing always risks skipping cells at high speeds. The sweep approach makes it mathematically impossible to miss a cell.

**ScriptableObject-driven levels.** All level data lives in assets, not scenes. Levels can be created, modified, and batch-generated entirely in the editor without entering play mode.

**Flat architecture over deep abstractions.** For a project of this scope, a flat folder structure with clear singleton managers is more readable and maintainable than layered abstractions.

Assets: Pixel Landscape Library by TOM WOOD — CC BY 4.0
