# DSP Mirror Blueprint

DSP Mirror Blueprint is a planned BepInEx 5 mod for Dyson Sphere Program. Its
single purpose is to let a player mirror the blueprint currently selected for
deployment across either of its two planar axes.

## Status

The project is at the design and scaffolding stage. No playable plugin has been
implemented yet. The initial product contract and architecture direction are
recorded in [docs/PROJECT.md](docs/PROJECT.md).

## Intended behavior

While placing a selected blueprint, the mod will provide two mirror operations:

- mirror across the blueprint's horizontal axis;
- mirror across the blueprint's vertical axis.

Mirroring is intended to affect the deployment preview and the resulting
placement without modifying the saved source blueprint. Exact controls and the
game integration points still need to be confirmed against the installed game.

## Target environment

- Dyson Sphere Program
- BepInEx 5
- C# 7.3
- .NET Framework 4.7.2 (`net472`)

Development uses the locally installed game and BepInEx assemblies as reference
inputs. Those assemblies must not be copied into or distributed with this
repository.

## Building

The build is not scaffolded yet. The intended entry point will be:

```text
build.cmd
```

with an optional nonstandard game installation path:

```text
build.cmd "D:\Games\Dyson Sphere Program"
```

## Scope

This project is deliberately narrow. General blueprint editing, rotation,
sharing, storage, and unrelated quality-of-life features are outside its current
scope.
