# Thunderstore package contract

The release workflow produces one installable ZIP with this exact layout:

```text
manifest.json
README.md
icon.png
LICENSE
BepInEx/
  plugins/
    DSP-Mirror-Blueprint/
      DSPMirrorBlueprint.dll
```

The three required Thunderstore files and the Apache 2.0 `LICENSE` are at the ZIP
root. The official 256 by 256 product icon depicts the blueprint mirror axis and
uses horizontally mirrored red trademark glyphs. The package declares
`xiaoye97-BepInEx-5.4.17` as its only dependency.

Release automation runs on each push to `main` or manual dispatch. `VERSION`
stores `MAJOR` and `MINOR`; the GitHub Actions run number supplies `PATCH`.
CI generates the BepInEx version, assembly/file version, commit-bearing release
label, and Thunderstore manifest version from that same `M.m.p` identity. Local
builds use the tracked `M.m.0.local` fallback. CI downloads BepInEx 5.4.17 and
restores pinned public .NET Framework and Unity compile references. It never
packages those references or any game assembly.

The workflow builds and runs the deterministic tests, verifies the generated
BepInEx and assembly identity, creates the package, validates its exact file set,
manifest, dependency, player README, icon, and DLL hash, then uploads the ZIP as
a GitHub Actions artifact named `DSPMirrorBlueprint-M.m.p-full`. The nested,
installable Thunderstore ZIP remains `DSPMirrorBlueprint-M.m.p.zip`. It does not
publish to Thunderstore automatically.

## Manual publication

Publishing is intentionally manual:

1. Complete every required row in [RC-VALIDATION.md](RC-VALIDATION.md).
2. In a dedicated release commit, change `VERSION` to `MAJOR=1` and `MINOR=0`,
   then push it to `main`. Do not change the patch number manually.
3. Wait for the release workflow to pass. Its single-digit run number `N`
   produces the accepted first stable version `1.0.N`.
4. Download `DSPMirrorBlueprint-1.0.N-full.zip` from that workflow run and
   extract it once.
5. Confirm the extracted file is `DSPMirrorBlueprint-1.0.N.zip`; this nested ZIP
   is the installable package validated by CI.
6. Upload the nested ZIP manually to Thunderstore. Do not upload the outer
   `-full` GitHub Actions artifact.

If the workflow run number reaches 10 before the stable release, revisit the
version policy instead of publishing an unapproved `1.0.N` version.
