# Input reliability recheck: 0.4.5

> Archived validation record. Active project management is maintained in
> [PROJECT.md](../PROJECT.md).

## Test identity

- Game version: Dyson Sphere Program `0.10.34`.
- Game assembly: the same installed `Assembly-CSharp.dll` recorded in
  [ASSEMBLY_FINDINGS.md](ASSEMBLY_FINDINGS.md).
- Mod version: `0.4.5`.
- Tester: Shy Alexander Tamir.
- Test time: 2026-08-10 08:42:03 CEST.
- Timestamp source: final modification time of `BepInEx\LogOutput.log` from the
  test session; message-ingestion time was not used.

## Recorded evidence

Input diagnostics were enabled. The log recorded:

- 40 `K` captures and 40 `Shift+K` captures from DSP's `VFInput`;
- an initial 20 events for each binding that were observed by the blueprint
  paste hook and applied successfully;
- zero failed mirror applications;
- zero mirror warnings or errors.

The remaining 20 events for each binding were captured while a placement
preview was active and prompting for automatic foundation placement. Those
events did not enter the blueprint paste hook, and no mirror-result line was
emitted during the prompt.

## Result

The tester reported that both bindings felt reliable and explicitly accepted
the input fix as complete. This follow-up closed the intermittent-input concern
recorded by the [0.4.4 validation run](RC-RESULTS-0.4.4.md).
