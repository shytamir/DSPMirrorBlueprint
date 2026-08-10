# Thunderstore package contract

The release workflow produces one installable ZIP with this exact layout:

```text
manifest.json
README.md
icon.png
BepInEx/
  plugins/
    DSP-Mirror-Blueprint/
      DSPMirrorBlueprint.dll
```

The three required Thunderstore files are at the ZIP root. The icon is a
temporary 256 by 256 PNG and should be replaced before a polished public
release. The package declares `xiaoye97-BepInEx-5.4.17` as its only dependency.

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
