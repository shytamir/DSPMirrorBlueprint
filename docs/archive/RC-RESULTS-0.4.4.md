# Release-candidate results: 0.4.4

> Archived validation record. Active project management is maintained in
> [PROJECT.md](../PROJECT.md).

## Test identity

- Game version: Dyson Sphere Program `0.10.34`.
- Game assembly: the same installed `Assembly-CSharp.dll` recorded in
  [ASSEMBLY_FINDINGS.md](ASSEMBLY_FINDINGS.md).
- Mod version: `0.4.4`.
- Tester: Shy Alexander Tamir.
- Test time: 2026-08-10 08:05:45 CEST.
- Timestamp source: final test-blueprint modification time from the active game
  session; message-ingestion time was not used.

## Reported results

| Test | Result | Tester note |
|---|---|---|
| RC-01 | PASS | — |
| RC-02 | PASS | — |
| RC-03 | PASS | — |
| RC-04 | PASS | — |
| RC-05 | PASS | — |
| RC-06 | PASS | — |
| RC-07 | PASS | — |
| RC-08 | PASS | — |
| RC-09 | PASS | — |
| RC-10 | PASS | — |
| RC-11 | PASS | — |
| RC-12 | PASS | See observation below. |

## Log and observation

No mirror error or warning was present in `BepInEx\LogOutput.log`. The tester
observed that `K` and `Shift+K` input was sometimes missed, so the requested
mirror did not occur on every keypress.

The result table preserves the tester's reported PASS. The observation does not
yet satisfy RC-12's stricter condition that each keypress refresh the preview
immediately. Static examination found that the implementation sampled Unity's
render-frame key-down edge from the blueprint paste game tick. After this run, a
candidate fix was implemented that registered both bindings with DSP's existing
`VFInput` capture table and read its fixed-update key-down state. The subsequent
[0.4.5 input recheck](RC-RESULTS-0.4.5.md) was accepted and closed this concern.
