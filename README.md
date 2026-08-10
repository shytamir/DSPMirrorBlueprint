# DSP Mirror Blueprint

DSP Mirror Blueprint is a planned BepInEx 5 mod for Dyson Sphere Program. Its
single purpose is to let a player mirror the blueprint currently selected for
deployment across either of its two planar axes.

## Status

The initial BepInEx plugin and local-reference build are scaffolded. The plugin
currently logs that it loaded; blueprint mirroring is not implemented yet. The
product contract and architecture direction are recorded in
[docs/PROJECT.md](docs/PROJECT.md).

## Intended behavior

While placing a selected blueprint, the mod will provide two mirror operations:

- mirror across the blueprint's horizontal axis;
- mirror across the blueprint's vertical axis.

Mirroring is intended to affect the deployment preview and the resulting
placement without modifying the saved source blueprint. The deployment data and
integration path are documented in
[docs/ASSEMBLY_FINDINGS.md](docs/ASSEMBLY_FINDINGS.md).

## Geometry diagnostics

The development build includes an opt-in geometry dump for establishing mirror
test fixtures. It is disabled by default. To use it:

1. Set `EnableGeometryDump = true` under `Diagnostics` in
   `BepInEx\config\com.shytamir.dspmirrorblueprint.cfg`.
2. Open a blueprint for deployment in game.
3. Press `F9`, or change `GeometryDumpKey` in the same configuration file.

JSON files are written beneath
`BepInEx\DSP-Mirror-Blueprint\Diagnostics`. They contain only the active cloned
blueprint's area geometry, building item/model IDs, offsets, angles, connection
indices and slots, and reform rectangles. Blueprint names, paths, descriptions,
authors, building content and parameters, save identifiers, and planet names are
not exported. The result is reported in `BepInEx\LogOutput.log`; no in-game
visual element is added.

## Target environment

- Dyson Sphere Program
- BepInEx 5
- C# 7.3
- .NET Framework 4.7.2 (`net472`)

Development uses the locally installed game and BepInEx assemblies as reference
inputs. Those assemblies must not be copied into or distributed with this
repository.

## Building

With Dyson Sphere Program installed in its default Steam location, run:

```text
build.cmd
```

with an optional nonstandard game installation path:

```text
build.cmd "D:\Games\Dyson Sphere Program"
```

The build runs the deterministic mirror-transform tests and writes the release
DLL to `bin\Release\DSPMirrorBlueprint.dll`. It references BepInEx, Unity, and
framework assemblies from the selected game installation; it does not copy
those dependencies into the output.

## Scope

This project is deliberately narrow. General blueprint editing, rotation,
sharing, storage, and unrelated quality-of-life features are outside its current
scope.
