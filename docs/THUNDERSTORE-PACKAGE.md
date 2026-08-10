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

Release automation runs on a `vM.m.p` tag or manual dispatch. The requested
version must match `Plugin.PluginVersion`; this keeps the Thunderstore manifest
and BepInEx identity synchronized. CI downloads BepInEx 5.4.17 and restores
pinned public .NET Framework and Unity compile references. It never packages
those references or any game assembly.

The workflow builds and runs the deterministic tests, creates the package,
validates its exact file set, manifest, dependency, player README, icon, and DLL
hash, then uploads the ZIP as a GitHub Actions artifact. It does not publish to
Thunderstore automatically.
