[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DllPath,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$VersionNumber,

    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [string]$OutputDirectory = (
        Join-Path $RepositoryRoot 'artifacts\packages'
    )
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$manifestTemplatePath = Join-Path $RepositoryRoot `
    'packaging\manifest.template.json'
$readmePath = Join-Path $RepositoryRoot 'packaging\README.md'
$iconPath = Join-Path $RepositoryRoot 'packaging\icon.png'

foreach ($requiredPath in @(
        $DllPath,
        $manifestTemplatePath,
        $readmePath,
        $iconPath
    )) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required package input was not found: $requiredPath"
    }
}

$template = Get-Content -Raw -LiteralPath $manifestTemplatePath
$placeholder = '{{VERSION_NUMBER}}'
if (([regex]::Matches(
            $template,
            [regex]::Escape($placeholder)
        )).Count -ne 1) {
    throw "Manifest template must contain exactly one $placeholder placeholder."
}

$manifestText = $template.Replace($placeholder, $VersionNumber)
$manifest = $manifestText | ConvertFrom-Json
if ($manifest.version_number -cne $VersionNumber) {
    throw 'Manifest version replacement failed.'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$packagePath = Join-Path $OutputDirectory `
    "DSPMirrorBlueprint-$VersionNumber.zip"
if (Test-Path -LiteralPath $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

$archive = [System.IO.Compression.ZipFile]::Open(
    $packagePath,
    [System.IO.Compression.ZipArchiveMode]::Create
)
try {
    $manifestEntry = $archive.CreateEntry(
        'manifest.json',
        [System.IO.Compression.CompressionLevel]::Optimal
    )
    $stream = $manifestEntry.Open()
    try {
        $writer = New-Object System.IO.StreamWriter(
            $stream,
            (New-Object System.Text.UTF8Encoding($false))
        )
        try {
            $writer.Write($manifestText)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive,
        $readmePath,
        'README.md',
        [System.IO.Compression.CompressionLevel]::Optimal
    ) | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive,
        $iconPath,
        'icon.png',
        [System.IO.Compression.CompressionLevel]::Optimal
    ) | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive,
        $DllPath,
        'BepInEx/plugins/DSP-Mirror-Blueprint/DSPMirrorBlueprint.dll',
        [System.IO.Compression.CompressionLevel]::Optimal
    ) | Out-Null
}
finally {
    $archive.Dispose()
}

Write-Output $packagePath
