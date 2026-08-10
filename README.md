# DSP Mirror Blueprint

DSP Mirror Blueprint is a BepInEx 5 mod for Dyson Sphere Program. Its
single purpose is to let a player mirror the blueprint currently selected for
deployment across either of its two planar axes.

## Status

Deployment-time preview integration is implemented and ready for in-game
verification. While a blueprint is open for placement, press `K` to mirror it
horizontally or `Shift+K` to mirror it vertically. The integration mutates only
the detached deployment copy and requests the game's normal preview refresh;
the saved source blueprint remains unchanged. The product contract and
architecture are recorded in [docs/PROJECT.md](docs/PROJECT.md).

## Intended behavior

While placing a selected blueprint, the mod handles two mirror operations:

- mirror across the blueprint's horizontal axis;
- mirror across the blueprint's vertical axis.

The implementation is designed so mirroring affects the deployment preview and
the resulting placement without modifying the saved source blueprint. The
deployment data and integration path are documented in
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
indices and slots, reform rectangles, and runtime slot poses for only the models
used by that blueprint. Blueprint names, paths, descriptions, authors, building
content and parameters, prefab paths, save identifiers, and planet names are not
exported. The result is reported in `BepInEx\LogOutput.log`; no in-game visual
element is added.

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

## Release package

Each push to `main`, or a manual release-workflow run, builds and tests the mod
using pinned public compile references and uploads a Thunderstore-compatible ZIP
as a GitHub Actions artifact. `VERSION` supplies the semantic major and minor
numbers; the workflow run number supplies the patch. BepInEx, assembly, and
Thunderstore identities are generated from that single release version. Package
layout and validation are documented in
[docs/THUNDERSTORE-PACKAGE.md](docs/THUNDERSTORE-PACKAGE.md).

## Scope

This project is deliberately narrow. General blueprint editing, rotation,
sharing, storage, and unrelated quality-of-life features are outside its current
scope.
