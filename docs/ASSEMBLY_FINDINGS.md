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

## Recommended integration seam

Patch `BuildTool_BlueprintPaste.DeterminRotate()` with a postfix located through
reflection. On `K` or `Shift+K`:

1. mutate only the active tool's cloned `blueprint`;
2. set the method result to `true`;
3. let `OperatingPrestage()` perform its normal forced refresh.

This seam is narrower than patching the 2,412-instruction
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

## Transform responsibilities inferred from the data flow

The following are implementation conclusions, not game-provided mirror rules:

- Horizontal mirroring should reflect the `y` coordinate; vertical mirroring
  should reflect `x`, each around the applicable area bounds.
- Both endpoint coordinate sets must be transformed. Vertical height offsets
  should remain unchanged.
- `yaw/yaw2`, and potentially inserter `pitch/tilt` components, must be adjusted
  so the reflected preview preserves the intended facing and endpoint geometry.
- Cursor offsets, area anchor offsets, and reform rectangles must remain
  consistent with the reflected area geometry.
- Connection object references preserve graph identity, but the behavior of port
  slot numbers and belt/inserter offsets under reflection requires validation.
- Width and height do not exchange when reflecting across an axis; reform
  rectangle origins do move within those unchanged bounds.

These rules should be implemented as a deterministic transform over the cloned
`BlueprintData`, followed by the game's forced refresh path.

## Runtime evidence still required

Static IL confirms which values participate, but it does not prove the semantic
meaning of every orientation and port index. Before fixing the transform
formulas, collect compact structural dumps from deliberately small blueprints:

1. an asymmetric set of directional buildings facing the four cardinal
   directions;
2. a belt with a sorter so both endpoints, connection references, slots, and
   offsets are present;
3. one multi-cell or asymmetric-footprint building;
4. a blueprint with foundation reform data;
5. a blueprint crossing a tropic boundary, producing multiple areas.

The quickest safe dump is an opt-in diagnostic in the mod that logs only the
active cloned `BlueprintData` geometry: area fields, building item/model IDs,
offsets, angles, connection indices/slots, and reform rectangles. It should omit
blueprint paths, descriptions, authorship, and unrelated game or save data.
Comparing those small dumps before and after the game's existing rotations will
establish angle conventions and provide fixtures for deterministic mirror tests.
