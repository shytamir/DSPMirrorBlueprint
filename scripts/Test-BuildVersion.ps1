[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DllPath,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$ExpectedPackageVersion,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.0$')]
    [string]$ExpectedAssemblyVersion,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedReleaseLabel,

    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
    throw "Built DLL was not found: $DllPath"
}
if ($ExpectedReleaseLabel -notmatch '^\d+\.\d+\.\d+\.(local|[0-9a-f]{7})$') {
    throw "Release label is invalid: $ExpectedReleaseLabel"
}

$buildVersionSource = Get-Content -Raw -LiteralPath (
    Join-Path $RepositoryRoot 'src\DSPMirrorBlueprint\BuildVersion.cs'
)
foreach ($expectedLine in @(
        "BepInPluginVersion = `"$ExpectedPackageVersion`"",
        "PluginVersion = `"$ExpectedPackageVersion`"",
        "ReleaseLabel = `"$ExpectedReleaseLabel`""
    )) {
    if (-not $buildVersionSource.Contains($expectedLine)) {
        throw "Generated BuildVersion source is missing: $expectedLine"
    }
}

$pluginSource = Get-Content -Raw -LiteralPath (
    Join-Path $RepositoryRoot 'src\DSPMirrorBlueprint\Plugin.cs'
)
if (-not $pluginSource.Contains(
        'BuildVersion.BepInPluginVersion)]') -or
    -not $pluginSource.Contains(
        'PluginVersion = BuildVersion.PluginVersion;')) {
    throw 'Plugin.cs does not consume the generated version contract.'
}

$assemblyName = [Reflection.AssemblyName]::GetAssemblyName(
    (Resolve-Path -LiteralPath $DllPath)
)
if ($assemblyName.Version.ToString() -cne $ExpectedAssemblyVersion) {
    throw "Assembly version is $($assemblyName.Version); expected $ExpectedAssemblyVersion."
}

$fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(
    (Resolve-Path -LiteralPath $DllPath)
)
if ($fileInfo.FileVersion -cne $ExpectedAssemblyVersion) {
    throw "File version is $($fileInfo.FileVersion); expected $ExpectedAssemblyVersion."
}
if ($fileInfo.ProductVersion -cne $ExpectedReleaseLabel) {
    throw "Product version is $($fileInfo.ProductVersion); expected $ExpectedReleaseLabel."
}

Write-Output "Build version validation passed: $ExpectedReleaseLabel"
