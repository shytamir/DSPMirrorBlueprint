# Project contract

## Purpose

DSP Mirror Blueprint will add horizontal and vertical mirroring to Dyson Sphere
Program's blueprint deployment interface. The feature is for transforming the
blueprint selected for placement; it is not a general-purpose blueprint editor.

## Product goals

- Offer a horizontal mirror operation during blueprint deployment.
- Offer a vertical mirror operation during blueprint deployment.
- Show the transformed layout in the deployment preview before construction.
- Place the same transformed layout that the preview communicates.
- Leave the saved source blueprint unchanged.
- Integrate without replacing the game's blueprint interface.

The terms `horizontal` and `vertical` describe the two axes of the blueprint's
deployment plane. Their exact mapping to the game's coordinate system will be
confirmed from runtime evidence before implementation.

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
orientations, and connections must be transformed consistently with those
position mappings. Their exact representation is an implementation detail to be
confirmed from game evidence.

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

## Proposed architecture

The smallest useful design has three boundaries:

1. **Plugin and input layer**: owns the BepInEx entry point, lifecycle, logging,
   configuration, and mirror commands.
2. **Game integration layer**: observes the active blueprint deployment state,
   reads and updates the placement preview through narrowly scoped adapters, and
   isolates version-sensitive game members.
3. **Mirror transform**: deterministic, game-independent logic that maps
   blueprint-relative positions and orientations across one selected axis.

The mirror transform is expressed as game-independent data operations over an
explicit aggregate transform plane. It retains area metadata and topology,
reflects both endpoint positions and orientation frames, and transforms reform
rectangle origins without rounding. Focused deterministic tests cover both axes
and repeated transforms without launching the game. Runtime adapters remain
responsible for converting game Euler rotations to and from orientation vectors.
Harmony patches or other runtime hooks should use the narrow integration point
identified in the assembly findings.

## Behavioral invariants to establish

- Applying the same mirror operation twice restores the original layout.
- Horizontal and vertical mirrors have defined, deterministic effects on every
  supported building orientation and connection.
- Preview and final placement use the same transformed data.
- Cancelling placement or selecting another blueprint clears transient mirror
  state.
- Invalid placements remain invalid; mirroring does not bypass game validation.

These are design requirements, not claims about an implementation that already
exists.

## Initial implementation milestones

1. Scaffold the BepInEx plugin and local-reference build.
2. Inspect the installed game assembly to identify blueprint deployment data,
   preview generation, placement confirmation, and existing rotate/input paths.
3. Define a minimal internal blueprint transform model and focused deterministic
   tests for both axes.
4. Add deployment-time input and preview integration.
5. Verify placement behavior in game, including belts, sorters, orientations,
   connections, repeated mirroring, cancellation, and blueprint switching.

## Required implementation research

Before game integration begins, inspect the installed game assemblies and
runtime behavior to identify the authoritative blueprint deployment types,
preview generation path, placement confirmation path, and existing input or
rotation handling. Record confirmed members separately from inferred behavior;
this research is an implementation prerequisite, not a product decision.
