# AGENTS.md

This file defines how coding agents should work in this repository.

## 1. Core rule

Complete the requested task with the smallest coherent change that satisfies
its acceptance criteria.

Do not turn a bounded task into a repository-wide review. Do not search for
additional work after the task is complete. Optimize for correctness,
maintainability, and reviewability—not activity.

## 2. Instruction order

Follow, in order:

1. The current user prompt.
2. This `AGENTS.md`.
3. Repository documentation and conventions.
4. Existing implementation patterns.
5. General engineering judgment.

A specific instruction overrides a general one.

## 3. Product contract

Read `docs/PROJECT.md` before changing behavior.

## 4. Scope

Inspect and modify only:

- files named in the task;
- files directly required to implement it;
- directly affected validation code;
- directly affected documentation.

Do not:

- fix unrelated defects;
- modernize nearby code;
- reorganize files without necessity;
- upgrade unrelated dependencies;
- perform broad cleanup;
- rewrite working code for style alone;
- expand the task because more work is visible.

Mention unrelated findings briefly in the final report. Do not fix them unless
they block the requested task.

## 5. Before editing

1. Run `git status --short`.
2. Inspect the directly relevant source and documentation.
3. Identify existing behavior and local conventions.
4. Determine the smallest viable implementation.
5. Identify the narrowest relevant validation.
6. Start editing once the task is sufficiently understood.

Do not repeatedly inspect the same files without a concrete unresolved
question. Resolve minor ambiguity from repository evidence. Ask only when a
missing decision materially changes the outcome.

## 6. Mutating versus non-mutating work

Treat requests to inspect, review, analyze, explain, or plan as non-mutating
unless they explicitly ask for changes.

Unless the prompt says `PLAN ONLY`, an explicit request to fix, implement,
author, update, or deploy is implementation work:

1. inspect;
2. implement;
3. validate;
4. repair failures caused by the change;
5. review the final diff;
6. commit only when requested;
7. push only when explicitly requested;
8. report after required Git operations succeed.

For non-mutating or `PLAN ONLY` tasks, do not modify, commit, or push.

## 7. Repository architecture



## 8. Toolchain and authoritative game evidence

The project uses:

- C# 7.3;
- .NET Framework 4.7.2 (`net472`);
- BepInEx 5;
- UnityEngine assemblies supplied by the installed game.

Build with:

```text
build.cmd
```

or, for a nonstandard installation:

```text
build.cmd "D:\Games\Dyson Sphere Program"
```

The project resolves BepInEx and Unity references through the `GameRoot`
property. Do not copy game assemblies into the repository.

When authoritative runtime knowledge is required:

- inspect the installed `Assembly-CSharp.dll` or Unity assemblies as read-only inputs;
- distinguish confirmed fields and methods from inference;
- prefer dedicated component pools and game statistics over broad reflective
  scans or inventory deltas;
- preserve forgiving reflection behavior when members are missing or renamed;
- never redistribute, modify, or commit game or Unity assemblies;
- record uncertainty instead of presenting a proxy as fact.

Do not introduce compile-time `Assembly-CSharp.dll` coupling without a
task-specific reason. The existing plugin intentionally uses defensive
reflection for most game-state access.

## 9. Implementation discipline

Prefer:

- minimal local patches;
- existing abstractions and conventions;
- direct, readable C# 7.3;
- deterministic analysis;
- focused validation;
- documentation that matches behavior.

Avoid:

- speculative abstractions;
- premature generalization;
- broad refactors hidden inside feature work;
- frame-by-frame allocations or factory-wide scans without measured need;
- duplicate implementations;
- unnecessary compatibility layers;
- comments that merely restate code.

Preserve public and snapshot contracts unless the task explicitly changes
them.

## 10. Runtime evidence and schema discipline



## 11. Validation

Run the narrowest relevant check first:

1. build the affected project;
2. fix failures caused by the change;
3. rerun the failed check;
4. run broader validation only when justified;
5. review the final diff once.

The release build must complete with zero errors. Do not claim in-game,
performance, continuity, or presentation behavior from compilation alone.

If a required tool or game state is unavailable, report the check as skipped
or blocked. Do not call it passed. Allow at most two repair cycles for the same
failure unless explicitly authorized.

## 12. Tests and documentation

Add or update tests when behavior changes and a focused deterministic test is
practical. Do not add broad test infrastructure for a small change.

Documentation must match actual behavior. Do not duplicate explanations or
rewrite documentation solely to change voice. Check affected links once after
documentation changes settle.

Do not commit:

- `bin/` or `obj/`;
- DLLs, PDBs, or copied game assemblies;
- snapshots containing player save data;
- temporary diagnostics;
- editor, cache, or OS noise.

## 13. Git discipline

Do not overwrite, revert, reformat, or include unexplained user changes.

Before committing:

1. inspect `git status --short`;
2. inspect the final diff;
3. confirm only intended files changed;
4. run required validation;
5. check for secrets, snapshots, temporary files, and generated output.

Create one coherent commit per requested task unless instructed otherwise. Use
a concise commit message. Do not amend, rebase, reset, clean, stash,
force-push, or rewrite history unless explicitly instructed.

Push only when explicitly requested.

### Routine Git access recovery

This Windows checkout may be owned by the desktop or Administrators account
while an agent command runs under a sandbox identity. If Git reports
`detected dubious ownership`, do not alter global Git configuration. Scope the
exception to each command:

```powershell
$repo = (Resolve-Path '.').Path.Replace('\', '/')
git -c "safe.directory=$repo" status --short
```

Use the same `-c "safe.directory=$repo"` form for other Git commands in that
turn.

If Git cannot create `.git/index.lock`, rerun only that Git operation under the
authenticated desktop context. Do not change repository permissions.

Before editing or pushing, fetch or use `git pull --ff-only` when the clean
local branch may be behind its remote. Never resolve divergence with a force
push or history rewrite.

Treat GitHub connector, local Git, and `gh` authentication as independent.
Do not run `gh auth status` as an unconditional plugin prerequisite. Check it
only immediately before an operation that genuinely requires `gh`, such as
Actions-log inspection or thread-aware review GraphQL.

Prefer the connector for supported GitHub API operations and local `git` for
fetch, pull, and push. A stale `gh` token does not block either path. If `gh`
is genuinely required and unauthenticated, ask the user to refresh it; do not
log out, replace credentials, or start interactive login yourself. Report a
blocker only when the interface required for the requested operation fails.

## 14. GitHub and CI

Do not create branches, pull requests, releases, tags, issues, or workflow
runs unless the prompt requests them or they are necessary to the requested
publish flow.

Check GitHub Actions only when the prompt requires CI results in the final
report. Otherwise, do not poll CI or delay completion for it.

Never expose tokens, credential-helper output, or secrets in logs or reports.

## 15. Anti-churn and iteration limits

Do not:

- repeatedly reopen the same files;
- repeat successful commands;
- perform equivalent searches;
- reconsider settled decisions without new evidence;
- edit, revert, and recreate substantially the same change;
- restart from first principles after implementation begins;
- continue after acceptance criteria and checks pass;
- search for more work after completion.

Unless explicitly authorized:

- use one relevant inspection pass;
- use one implementation pass;
- allow at most two repair cycles for the same failure;
- perform one final diff review;
- run each successful validation command once.

## 16. Stop conditions

Stop and report when:

- completion requires changes outside scope;
- the task conflicts with repository architecture or explicit instructions;
- required credentials, services, dependencies, assemblies, or data are
  unavailable;
- user changes prevent safe modification;
- validation reveals an unrelated repository-wide failure;
- two repair cycles fail to resolve the same blocker;
- the outcome requires a major design decision not covered by the prompt;
- committing or pushing would include unrelated work.

Do not hide blockers by broadening scope. Do not claim completion while
acceptance criteria remain unmet.

## 17. Definition of done

A task is complete when:

- the requested behavior or artifact exists;
- the change stays within scope;
- relevant checks pass or skips are reported accurately;
- documentation is updated when required;
- the final diff contains only intentional changes;
- no known defect introduced by the change remains;
- the work is committed when requested;
- the commit is pushed when required;
- the final report is accurate.

Once complete, stop.

## 18. Final report

Report:

### Completed

A concise description of the result.

### Changed

- files created, modified, or removed;
- significant behavioral changes.

### Validation

List each command actually run and its result. Do not claim checks that were
not run.

### Git

- branch;
- commit hash, or `Not committed — explicitly not requested`;
- commit message, when committed;
- push result: successful, failed, or not requested.

### Residual issues

List only known limitations, blockers, or relevant follow-up deliberately left
out of scope. If none, say:

`None known within the requested scope.`

Keep the report factual and concise.
