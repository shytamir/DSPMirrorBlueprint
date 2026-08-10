# Release-candidate in-game validation

Complete this matrix before changing `VERSION` to the stable `1.0` line. Record
the game version, mod version, result, and a short note for every row. A test
passes only when both the preview and the placed result match the expectation.

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
5. Prepare these deliberately asymmetric blueprints so a flip is unmistakable:
   - **Odd bounds:** a `5 x 5` layout with one building on the center grid line
     and a different building in only one corner.
   - **Even bounds:** a `6 x 4` layout with different buildings in the left and
     right corners and in the top and bottom corners.
   - **Connections:** two machines joined by input and output sorters, plus an
     L-shaped belt whose belt directions are known.
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
| RC-03 | Open the odd-bounds blueprint. Test `K`, cancel, reopen, then test `Shift+K`. | The building on the applicable center grid line stays on that line. The off-center marker moves to the opposite side for each axis. |
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
