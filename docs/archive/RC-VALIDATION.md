# Release-candidate in-game validation

> Archived validation procedure. Active project management is maintained in
> [PROJECT.md](../PROJECT.md).

This matrix defined the in-game acceptance work required before changing
`VERSION` to the stable `1.0` line. Record the game version, mod version, result,
and a short note for every row when reusing it for regression testing. A test
passes only when both the preview and the placed result match the expectation.

The complete matrix was recorded for [version 0.4.4](RC-RESULTS-0.4.4.md), and
the accepted input follow-up was recorded for
[version 0.4.5](RC-RESULTS-0.4.5.md).

## Preparation

1. Back up the save that will be used for testing.
2. Extract the nested `DSPMirrorBlueprint-M.m.p.zip` from the downloaded
   `DSPMirrorBlueprint-M.m.p-full.zip`, then install the nested ZIP with a mod
   manager. For a manual installation, copy its `BepInEx` folder into the game
   directory.
3. Start the game with mods enabled. Open `BepInEx\LogOutput.log` and confirm it
   contains `DSP Mirror Blueprint M.m.p loaded` with no message saying blueprint
   mirroring is unavailable.
4. Use a clear construction area with enough inventory to place every test
   blueprint. Keep `EnableGeometryDump` disabled unless a failed case needs a
   diagnostic snapshot.
5. Prepare these deliberately asymmetric blueprints so a flip is unmistakable.
   The dimensions below describe the complete drag-selection rectangle, not an
   individual structure footprint. Leave empty space around large structures
   and use single belt segments as one-cell position markers.
   - **Odd bounds:** use a `21 x 21` selection rectangle, or another comfortably
     sized odd-by-odd rectangle. The center is the 11th row and 11th column.
     Put one belt marker on each centerline, one structure in the upper-right
     quarter, and a different structure in the lower-left quarter. Keep every
     structure fully inside the selection.
   - **Even bounds:** use a `20 x 16` selection rectangle, or another comfortably
     sized even-by-even rectangle. Its centerlines fall between columns 10 and
     11 and between rows 8 and 9. Put different belt markers near all four
     corners and place two different structures in non-symmetric positions well
     inside the selection.
   - **Connections:** use at least a `24 x 18` selection rectangle. Place two
     machines with enough clearance for an input and output sorter between each
     machine and its belts. Add an L-shaped belt whose flow direction is known.
   - **Reform:** a non-symmetric foundation pattern, such as a `3 x 2` block on
     one side and a single foundation tile on the other.
   - **Multi-area:** a blueprint crossing a tropic boundary, containing the
     connection and reform patterns above.

`Horizontal` means `K`: top and bottom exchange while left and right stay on
their original sides. `Vertical` means `Shift+K`: left and right exchange while
top and bottom stay on their original sides.

## Validation matrix

| ID | Blueprint and action | Pass condition |
|---|---|---|
| RC-01 | Open the even-bounds blueprint. Press `K` once and place it. | Top and bottom exchange in the preview and placed result. Left and right do not exchange. |
| RC-02 | Reopen the original even-bounds blueprint. Press `Shift+K` once and place it. | Left and right exchange in the preview and placed result. Top and bottom do not exchange. |
| RC-03 | Open the odd-bounds blueprint. Test `K`, cancel, reopen, then test `Shift+K`. | The belt marker on the applicable center grid line stays on that line. Each off-center structure moves to the opposite side for the selected axis. |
| RC-04 | Open either simple blueprint. Press `K` twice. Cancel, reopen it, and press `Shift+K` twice. | After the second press, the preview exactly matches the unmirrored blueprint for that axis. |
| RC-05 | Open the same blueprint twice. First test `K` then `Shift+K`; after cancelling and reopening, test `Shift+K` then `K`. | Both key orders produce the same 180-degree-reflected preview and placed layout. |
| RC-06 | Open the connections blueprint. Test and place each axis separately. Run both machines. | Sorters remain attached to the intended machine ports and transfer items in the original input/output direction. Every belt segment remains connected and moves items in the original flow direction. |
| RC-07 | Open the reform blueprint. Test and place each axis separately. | The exact foundation shapes and counts appear on the opposite side for the selected axis; rectangle dimensions and foundation types do not change. |
| RC-08 | Open the multi-area blueprint at the same latitude where it was captured. Test and place each axis separately. | All areas form one coherent preview across the tropic. Buildings, sorters, belts, and reform geometry mirror together and the placed result matches the preview. |
| RC-09 | Mirror a blueprint, cancel placement, then reopen the same saved blueprint. Mirror it again, cancel, and select a different blueprint. | Reopened and newly selected blueprints start in their saved, unmirrored state. No transform carries across cancellation or selection. |
| RC-10 | Mirror and place a blueprint, then open the saved source blueprint again. | The saved source still opens in its original orientation; only the deployed copy was mirrored. |
| RC-11 | Move a mirrored preview onto an obstruction large enough to overlap the entire blueprint. | The game continues to show its normal invalid-placement state and refuses placement. Mirroring does not bypass validation. |
| RC-12 | Exercise both axes once in ordinary cursor placement and once while using a drag-placement blueprint. | Each key press refreshes the preview immediately and the game remains responsive; no mirror warning or exception appears in `LogOutput.log`. |

An unplaceable preview is not automatically a mirror failure: planetary grid or
collision rules can reject geometrically correct transforms. Verify the shape
first, then move the preview to a compatible location. RC-01, RC-02, RC-06,
RC-07, and RC-08 still require at least one successful placement.

## Input reliability recheck

Use this focused check after installing a build containing the DSP input-capture
fix:

1. Exit the game. In
   `BepInEx\config\com.shytamir.dspmirrorblueprint.cfg`, set
   `EnableInputDiagnostics = true`, then start the game.
2. Open the `21 x 21` odd-bounds blueprint for placement. Do not place it.
3. Press and release `K` 20 times. Pause after each press long enough to confirm
   that the preview flips before pressing again. Count keypresses and flips.
4. Cancel placement, reopen the same blueprint, and press and release
   `Shift+K` 20 times using the same procedure.
5. Repeat steps 3 and 4 with the drag-placement blueprint while its drag preview
   is active.
6. Exit the game and inspect `BepInEx\LogOutput.log`. Each accepted press must
   have a `captured by VFInput` line, an `observed by blueprint paste` line, and
   an `applied=True` result. There must be 40 successful results for ordinary
   placement and 40 for drag placement, with no mirror warning or exception.
7. Record the four press/flip counts and attach the contiguous input-diagnostic
   log excerpt if any count differs. A capture line without a matching observed
   line distinguishes a paste-hook timing failure; no capture line means DSP did
   not accept that binding in the active input context.
8. Restore `EnableInputDiagnostics = false` after the check.

## Result record

Record results in this form outside the repository unless they contain no save
or player data:

```text
Game version:
Mod version:
Tester/date:
RC-01 PASS/FAIL - note
...
RC-12 PASS/FAIL - note
Log errors or warnings:
```

Any crash, corrupted source blueprint, incorrect connection, preview/placement
mismatch, or failed required placement blocks 1.0. If a geometry failure cannot
be explained from the log, enable the opt-in diagnostic, reproduce only that
active blueprint, and retain the JSON privately for analysis.
