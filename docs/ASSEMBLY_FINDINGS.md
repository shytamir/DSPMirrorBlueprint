# Assembly examination: blueprint deployment

## Evidence scope

This document records static findings from the locally installed Dyson Sphere
Program assembly. It intentionally separates confirmed members and call paths
from transform behavior that still needs runtime evidence.

- Assembly: `DSPGAME_Data/Managed/Assembly-CSharp.dll`
- Assembly identity: `Assembly-CSharp, Version=0.0.0.0`
- Module MVID: `ece4a40e-5e73-43f4-a9f8-4e74970b5942`
- SHA-256: `AE0BA95F75BD879A62AA4CE253B2AB78EAA4FB3C7C595F5E1FEE75EBE0E0EF85`
- Inspection method: read-only metadata and IL examination with the installed
  BepInEx copy of Mono.Cecil

These identifiers make the findings traceable to the examined game build. They
are not a compatibility guarantee.

## Confirmed deployment path

The active paste tool can be reached through these public members:

```text
GameMain.mainPlayer
  -> Player.controller
  -> PlayerController.actionBuild
  -> PlayerAction_Build.blueprintPasteTool
```

`PlayerAction_Build.blueprintMode` reports `EBlueprintMode.Paste` while the tool
is in paste mode. `PlayerAction_Build.activeTool` also resolves the currently
active `BuildTool`.

Paste mode begins in:

```text
PlayerController.OpenBlueprintPasteMode(BlueprintData, string, bool)
```

That method calls `BlueprintData.Clone()` before assigning the result to
`BuildTool_BlueprintPaste.blueprint`. `Clone()` performs a serialize/deserialize
round trip through `ToBase64String()` and `FromBase64String()`. The deployment
copy is therefore detached from the selected saved blueprint. Mutating the
paste tool's copy will not rewrite the source blueprint.

The relevant per-tick path is:

```text
PlayerAction_Build.GameTick(long)
  -> BuildTool._GameTick(long)
  -> BuildTool_BlueprintPaste._OnTick(long)
  -> BuildTool_BlueprintPaste.OperatingPrestage()
  -> BuildTool_BlueprintPaste.DeterminRotate()
  -> BuildTool_BlueprintPaste.DeterminePreviewsPrestage(bool, bool)
```

`OperatingPrestage()` uses the Boolean returned by `DeterminRotate()` as a
"transform changed" signal. When true, it regenerates blueprint grid boxes and
calls `DeterminePreviewsPrestage(true, false)`. The forced path calls:

1. `BlueprintUtils.InitBuildPreviewByBPData(...)` to initialize the
   `BuildPreview[]` pool and its connections;
2. `BlueprintUtils.RefreshBuildPreview(...)` to derive world positions,
   rotations, conditions, and reform preview data from `BlueprintData`;
3. the existing GPU preview update methods.

`BuildTool_BlueprintPaste.CreatePrebuilds()` later consumes the same
`BuildPreview` positions, rotations, connection fields, parameters, and build
conditions when creating `PrebuildData`. Applying a mirror before the forced
preview refresh therefore lets the game's existing preview, validation, and
placement paths remain authoritative.

## Implemented integration seam

The implementation patched `BuildTool_BlueprintPaste.DeterminRotate()` with a
postfix located through reflection. On `K` or `Shift+K`, it:

1. mutates only the active tool's cloned `blueprint`;
2. sets the method result to `true`;
3. lets `OperatingPrestage()` perform its normal forced refresh.

This seam was narrower than patching the 2,412-instruction
`BlueprintUtils.RefreshBuildPreview()` method or the 922-instruction
`CreatePrebuilds()` method. It also avoids maintaining a second preview or
placement implementation. The method is called in both the ordinary valid-cursor
path and the drag-placement path.

No blueprint-specific mirror method exists in the examined assembly. The
mirror-named UI methods found by the scan belong to the mecha editor and are
unrelated.

The fixed input can be read through Unity's legacy input API inside this hook.
The paste-tool call site already scopes handling to blueprint deployment; no
global `K` action needs to be added to `VFInput`.

## Confirmed blueprint data surface

### `BlueprintData`

The fields that define deployment geometry and anchoring are:

- `cursorOffset_x`, `cursorOffset_y`, and `cursorTargetArea`;
- `dragBoxSize_x` and `dragBoxSize_y`;
- `primaryAreaIdx`;
- `BlueprintArea[] areas`;
- `BlueprintBuilding[] buildings`;
- `BPReformData reformData`.

### `BlueprintArea`

Each area contains:

- identity and hierarchy: `index`, `parentIndex`;
- planetary-grid context: `tropicAnchor`, `areaSegments`;
- parent anchoring: `anchorLocalOffsetX`, `anchorLocalOffsetY`;
- bounds: `width`, `height`.

The area hierarchy and planetary-grid context mean a multi-area mirror cannot be
implemented safely as a single factory-world coordinate negation.

### `BlueprintBuilding`

Each building stores two possible endpoints:

- planar offsets: `localOffset_x`, `localOffset_y`, `localOffset_x2`,
  `localOffset_y2`;
- vertical offsets: `localOffset_z`, `localOffset_z2`;
- orientation: `pitch`, `yaw`, `tilt`, `pitch2`, `yaw2`, `tilt2`;
- area ownership: `areaIndex`.

It also stores connection topology and configuration:

- `outputObj`, `inputObj` and their temporary serialized indices;
- `outputToSlot`, `inputFromSlot`, `outputFromSlot`, `inputToSlot`;
- `outputOffset`, `inputOffset`;
- `recipeId`, `filterId`, `parameters`, and `content`.

`BlueprintUtils.RefreshBuildPreview(...)` reads all planar offsets and angles.
It transforms `localOffset_x/localOffset_y` and the second endpoint through
`TransitionWidthAndHeight(yaw, x, y)`, then derives `BuildPreview.lpos`, `lpos2`,
`lrot`, and `lrot2`. Inserters receive a distinct rotation path that uses pitch,
yaw, and tilt for both endpoints.

### Reform data

`BPReformData.rects` contains `BPReformRect` values with:

- `short x`, `short y`;
- `byte w`, `byte h`;
- `byte data`, `byte areaIndex`.

Foundation/reform geometry therefore needs the same per-area reflection as
buildings if it is enabled for the selected blueprint.

## Confirmed runtime fixture

An opt-in geometry snapshot from plugin version `0.2.0` was examined without
copying it into the repository. Its `assemblyMvid` matches the assembly recorded
above. The fixture contains:

- one area with width `20`, height `24`, and centerline `(9.5, 11.5)`;
- cursor offset `(10, 12)` targeting area `0`;
- 137 buildings and no reform rectangles;
- item/model populations of `2001/35` (106), `2011/41` (18), `2308/63`
  (6), `2201/44` (3), `2106/121` (3), and `2307/61` (1);
- 123 buildings with at least one resolved input or output connection;
- 18 two-endpoint buildings, all item/model `2011/41`.

All resolved connection indices pointed to buildings in the snapshot. Every live
connection index also matched its temporary serialized index. This confirmed
that the diagnostic preserved enough topology to test graph identity after a
mirror. At that stage, it did not prove whether any slot or offset value needed
remapping.

The building coordinates occupy approximately `x = 0..19` and `y = 0..23`.
Small deviations from integers are present, including epsilon-sized negative
and positive values. A mirror must therefore transform the stored floating-point
values around `width - 1` or `height - 1`; it must not globally round or snap
building positions.

For the 18 two-endpoint buildings, `yaw` follows the bearing from the first
endpoint to the second. The mean difference is about `0.14` degrees and the
maximum is about `0.44` degrees. Yaw `0` aligns approximately with increasing
`y`, and yaw `90` with increasing `x`. This supports the normalized mappings:

- vertical mirror (`x' = width - 1 - x`):
  `yaw' = (360 - yaw) mod 360`;
- horizontal mirror (`y' = height - 1 - y`):
  `yaw' = (180 - yaw) mod 360`.

The two-endpoint records also contained small, nonzero pitch and tilt values
caused by planetary curvature. Mirroring yaw alone would discard meaningful
orientation data. This established the requirement to reflect the orientation's
forward and up vectors in the corresponding local plane, reconstruct a proper
rotation, and write the resulting Euler components for both endpoints.

The snapshot records the cloned `BlueprintData`, while the game's deployment
rotation changes `BuildTool_BlueprintPaste.yaw`. Capturing the same active
blueprint after ordinary deployment rotation would therefore produce the same
blueprint geometry and would not add orientation evidence.

A second snapshot from the same plugin and assembly versions contains 146
buildings, two areas, and two reform rectangles. Its top-level drag bounds are
`27 x 15`. The areas are:

- area `0`: root, `27 x 5`, 160 segments, anchor `(0, 0)`;
- area `1`: child of area `0`, `21 x 10`, 120 segments, anchor `(0, 5)`.

This confirms that a tropic-crossing blueprint can join areas with different
longitude segment counts through `parentIndex` and local anchor offsets. It also
shows why area metadata must be retained by the internal model even if the first
transform implementation operates in the combined drag bounds.

Both reform rectangles belong to area `0`:

- `(x=19, y=3, width=3, height=2, data=32)`;
- `(x=25, y=3, width=1, height=2, data=32)`.

This confirms the reform rectangle schema needed by the transform. For a
rectangle within bounds, vertical reflection moves its origin to
`width - x - rectangleWidth`, while horizontal reflection uses
`height - y - rectangleHeight`; rectangle dimensions and `data` remain intact.

All buildings and both reform rectangles report `areaIndex = 0`, while building
`y` coordinates span approximately `0..14`, beyond area `0`'s height of `5` and
across the combined drag height of `15`. The fixture therefore proves the
multi-area hierarchy but does not support treating every element coordinate as
strictly local to the dimensions of its referenced area. The internal transform
consequently preserves area metadata and element area indices while using an
explicit aggregate transform plane. Area-anchor rewriting was deferred unless
an in-game mirrored preview showed that it was necessary.

## Transform rules derived from the data flow

The following implementation rules were derived from the evidence; they are not
game-provided mirror rules:

- Horizontal mirroring reflects the `y` coordinate; vertical mirroring reflects
  `x`, each around the applicable area bounds.
- Both endpoint coordinate sets are transformed. Vertical height offsets remain
  unchanged.
- Both endpoint orientations are reflected. This includes pitch and tilt,
  not only `yaw/yaw2`.
- Cursor offsets, area anchor offsets, and reform rectangles must remain
  consistent with the reflected area geometry.
- Connection object references preserve graph identity. Runtime slot-pose
  evidence confirms that attachment slot numbers must be remapped by reflecting
  prefab-local X; full slot orientation disambiguates coincident corner poses.
- Width and height do not exchange when reflecting across an axis; reform
  rectangle origins do move within those unchanged bounds.

These rules were implemented as a deterministic transform over the cloned
`BlueprintData`, followed by the game's forced refresh path.

## Runtime evidence status

Static IL confirmed which values participate, but did not prove the semantic
meaning of every port index or all multi-area anchor behavior. The captured
fixtures covered cardinal directions, connected belts, two-endpoint sorters,
several other building models, reform rectangles, and a two-area tropic
crossing. Initial multi-area runtime testing exposed incorrect sorter port
mapping. Slot-pose diagnostics then identified the missing transform, and a
follow-up check confirmed geometrically correct mirroring after slot remapping;
the observed preview was unplaceable under the game's normal placement rules.

The comprehensive preview and placement checks still required for 1.0 are
defined in [RC-VALIDATION.md](RC-VALIDATION.md).

The mod includes a disabled-by-default `F9` diagnostic that writes only the
active cloned `BlueprintData` geometry: area fields, building item/model IDs,
offsets, angles, connection indices/slots, reform rectangles, and runtime slot
poses for models referenced by the active blueprint. The slot poses are resolved
through `LDB.models.Select(modelIndex).prefabDesc.slotPoses` and contain only
numeric model indices plus pose vectors; prefab paths and unrelated model data
are excluded. The diagnostic also omits blueprint paths, descriptions,
authorship, content, parameters, planet names, and unrelated game or save data.
Usage and output location are documented in the repository README. Diagnostic
JSON remains runtime evidence and must not be committed.
