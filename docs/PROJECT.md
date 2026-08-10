# Project contract

## Purpose

DSP Mirror Blueprint adds horizontal and vertical mirroring to Dyson Sphere
Program's blueprint deployment interface. The feature transforms the
blueprint selected for placement; it is not a general-purpose blueprint editor.

## Product goals

- Offer a horizontal mirror operation during blueprint deployment.
- Offer a vertical mirror operation during blueprint deployment.
- Show the transformed layout in the deployment preview before construction.
- Place the same transformed layout that the preview communicates.
- Leave the saved source blueprint unchanged.
- Integrate without replacing the game's blueprint interface.

The terms `horizontal` and `vertical` describe the two axes of the blueprint's
deployment plane. Runtime evidence confirmed that horizontal reflection changes
the local `y` coordinate and vertical reflection changes local `x`.

## Player interaction

- Pressing `K` mirrors the selected blueprint across its horizontal axis.
- Pressing `Shift+K` mirrors it across its vertical axis.
- The transformed blueprint preview is the only player-facing indication of the
  operation. No buttons, labels, notifications, or other visual elements are
  required.
- The bindings are fixed for the initial implementation; configurability is not
  currently required.

## Mirror origin

Each operation mirrors around the centerline of the selected blueprint's bounds.
Even and odd dimensions are both unambiguous under this rule:

- for an odd number of grid positions, the centerline passes through the middle
  position, which maps to itself;
- for an even number of grid positions, the centerline lies halfway between the
  two middle positions, which exchange places.

For zero-based discrete coordinates, the corresponding mappings are
`x' = width - 1 - x` and `y' = height - 1 - y`. Building footprints,
orientations, connections, reform geometry, and cursor offsets are transformed
consistently with those position mappings.

## Non-goals

- Editing or rewriting blueprint files.
- Adding arbitrary-angle rotation or other blueprint transforms.
- Managing, categorizing, importing, or exporting blueprints.
- Changing construction rules or bypassing placement validation.
- Supporting mod loaders other than BepInEx 5.
- Compatibility guarantees across game releases or with other mods.

## Compatibility and technical constraints

- Language: C# 7.3.
- Target framework: .NET Framework 4.7.2 (`net472`).
- Runtime: BepInEx 5 in Dyson Sphere Program's Unity environment.
- Game and Unity assemblies are local, read-only development dependencies and
  must not be committed or redistributed.
- Integration with game state should remain defensive where game members may
  change between releases.
- A compile-time dependency on `Assembly-CSharp.dll` requires a specific,
  documented need; reflection or a narrow adapter is preferred for unstable
  game internals.

## Architecture

The smallest useful design has three boundaries:

1. **Plugin and input layer**: owns the BepInEx entry point, lifecycle, logging,
   configuration, mirror commands, and registration in DSP's existing
   override-key capture table.
2. **Game integration layer**: observes the active blueprint deployment state,
   reads and updates the placement preview through narrowly scoped adapters, and
   isolates version-sensitive game members.
3. **Mirror transform**: deterministic, game-independent logic that maps
   blueprint-relative positions and orientations across one selected axis.

The mirror transform is expressed as game-independent data operations over an
explicit aggregate transform plane. It retains area metadata and topology,
reflects both endpoint positions and orientation frames, and transforms reform
rectangle and cursor origins without rounding. Focused deterministic tests cover
both axes, repeated transforms, and connection-slot remapping without launching
the game. The runtime adapter converts game Euler rotations and model slot poses
to and from orientation vectors. Slots are remapped by reflecting their
prefab-local poses, including orientation to disambiguate coincident positions.
A Harmony postfix on `BuildTool_BlueprintPaste.DeterminRotate()` handles the
fixed input and asks the game's existing path to refresh the preview after each
successful mirror. The input layer reads `K` and exact `Shift+K` key-down events
from DSP's `VFInput` snapshot rather than polling Unity input from the paste-tool
tick. A disabled-by-default trace logs capture, paste-hook observation, and
application outcome for focused runtime validation.

## Release acceptance invariants

- Applying the same mirror operation twice restores the original layout.
- Horizontal and vertical mirrors have defined, deterministic effects on every
  supported building orientation and connection.
- Preview and final placement use the same transformed data.
- Cancelling placement or selecting another blueprint clears transient mirror
  state.
- Invalid placements remain invalid; mirroring does not bypass game validation.

The deterministic transform tests cover these invariants where no game runtime
is required. Preview, placement, cancellation, switching, and invalid-placement
behavior are verified through [the in-game RC matrix](RC-VALIDATION.md).

## Implementation status

1. The BepInEx plugin and local-reference build were scaffolded.
2. The installed game assembly was examined and its deployment path was
   documented.
3. A minimal transform model and deterministic tests were implemented.
4. Deployment-time input and preview integration were implemented.
5. Targeted runtime fixtures exposed and confirmed the sorter slot-remapping
   fix. The 0.4.4 RC matrix was reported as passing all rows; intermittent
   missed input on both bindings remained an open 1.0 release concern. The
   direct paste-tick polling was subsequently replaced with DSP-captured
   override-key events. The candidate fix, deterministic coverage, and opt-in
   trace were implemented; focused in-game revalidation remains required.
6. Release packaging, licensing, semantic version generation, and the official
   mirrored product icon were completed for the release candidate.

## Evidence record

The installed game assemblies and targeted runtime snapshots were examined to
identify the authoritative blueprint deployment types, preview and placement
paths, input seam, reform geometry, multi-area metadata, and connection slot
poses. Confirmed members remain separated from inference in
[ASSEMBLY_FINDINGS.md](ASSEMBLY_FINDINGS.md).
