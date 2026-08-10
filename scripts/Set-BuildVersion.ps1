[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [int]::MaxValue)]
    [int]$Sequence,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{7,40}$')]
    [string]$Commit,

    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [string]$BuildVersionPath = (
        Join-Path $RepositoryRoot 'src\DSPMirrorBlueprint\BuildVersion.cs'
    ),

    [string]$BuildInfoPath = (
        Join-Path $RepositoryRoot 'artifacts\BUILD-INFO.txt'
    )
)

$ErrorActionPreference = 'Stop'
$versionPath = Join-Path $RepositoryRoot 'VERSION'
if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
    throw "VERSION was not found at $versionPath."
}

$values = @{}
foreach ($line in Get-Content -LiteralPath $versionPath) {
    if ($line -match '^\s*(MAJOR|MINOR)\s*=\s*(\d+)\s*$') {
        $values[$Matches[1]] = [int]$Matches[2]
    }
    elseif (-not [string]::IsNullOrWhiteSpace($line)) {
        throw "Invalid VERSION line: '$line'."
    }
}
foreach ($requiredName in @('MAJOR', 'MINOR')) {
    if (-not $values.ContainsKey($requiredName)) {
        throw "VERSION is missing $requiredName."
    }
}

$shortCommit = $Commit.Substring(0, 7).ToLowerInvariant()
$packageVersion = '{0}.{1}.{2}' -f (
    $values.MAJOR,
    $values.MINOR,
    $Sequence
)
$assemblyVersion = "$packageVersion.0"
$releaseLabel = "$packageVersion.$shortCommit"

New-Item -ItemType Directory -Force `
    -Path (Split-Path -Parent $BuildVersionPath) | Out-Null
New-Item -ItemType Directory -Force `
    -Path (Split-Path -Parent $BuildInfoPath) | Out-Null

$source = @"
namespace DSPMirrorBlueprint
{
    internal static class BuildVersion
    {
        public const string BepInPluginVersion = "$packageVersion";
        public const string PluginVersion = "$packageVersion";
        public const string ReleaseLabel = "$releaseLabel";
    }
}
"@
Set-Content -LiteralPath $BuildVersionPath -Value $source -Encoding utf8

$buildInfo = @"
Release label: $releaseLabel
Package version: $packageVersion
Assembly version: $assemblyVersion
Source commit: $($Commit.ToLowerInvariant())
Workflow sequence: $Sequence
"@
Set-Content -LiteralPath $BuildInfoPath -Value $buildInfo -Encoding utf8

$outputs = [ordered]@{
    RELEASE_LABEL = $releaseLabel
    PACKAGE_VERSION = $packageVersion
    ASSEMBLY_VERSION = $assemblyVersion
}
if ($env:GITHUB_ENV) {
    foreach ($entry in $outputs.GetEnumerator()) {
        Add-Content -LiteralPath $env:GITHUB_ENV `
            -Value "$($entry.Key)=$($entry.Value)"
    }
}
if ($env:GITHUB_OUTPUT) {
    foreach ($entry in $outputs.GetEnumerator()) {
        Add-Content -LiteralPath $env:GITHUB_OUTPUT `
            -Value "$($entry.Key.ToLowerInvariant())=$($entry.Value)"
    }
}

[pscustomobject]$outputs
