[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedDllPath,

    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-ZipText {
    param([System.IO.Compression.ZipArchiveEntry]$Entry)
    $stream = $Entry.Open()
    try {
        $reader = New-Object System.IO.StreamReader(
            $stream,
            (New-Object System.Text.UTF8Encoding($false, $true)),
            $true
        )
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

foreach ($requiredPath in @($PackagePath, $ExpectedDllPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required validation input was not found: $requiredPath"
    }
}

$expectedEntries = @(
    'manifest.json',
    'README.md',
    'icon.png',
    'BepInEx/plugins/DSP-Mirror-Blueprint/DSPMirrorBlueprint.dll'
)
$archive = [System.IO.Compression.ZipFile]::OpenRead(
    (Resolve-Path -LiteralPath $PackagePath)
)
try {
    $files = @($archive.Entries | Where-Object {
            -not $_.FullName.EndsWith('/')
        })
    $entryNames = @($files | ForEach-Object {
            $_.FullName.Replace('\', '/')
        })
    if ($entryNames.Count -ne $expectedEntries.Count) {
        throw "Package contains $($entryNames.Count) files; expected $($expectedEntries.Count)."
    }
    foreach ($expectedEntry in $expectedEntries) {
        if ($entryNames -cnotcontains $expectedEntry) {
            throw "Package entry is missing or incorrectly cased: $expectedEntry"
        }
    }
    if (($entryNames | Select-Object -Unique).Count -ne $entryNames.Count) {
        throw 'Package contains duplicate entries.'
    }

    $manifestEntry = $files | Where-Object FullName -CEQ 'manifest.json'
    $manifest = (Read-ZipText $manifestEntry) | ConvertFrom-Json
    if ($manifest.name -cne 'DSPMirrorBlueprint' -or
        $manifest.version_number -cne $ExpectedVersion -or
        $manifest.website_url -cne 'https://github.com/shytamir/DSPMirrorBlueprint') {
        throw 'Manifest identity or version is invalid.'
    }
    if (@($manifest.dependencies).Count -ne 1 -or
        $manifest.dependencies[0] -cne 'xiaoye97-BepInEx-5.4.17') {
        throw 'Manifest dependency list is invalid.'
    }

    $readmeEntry = $files | Where-Object FullName -CEQ 'README.md'
    $readme = Read-ZipText $readmeEntry
    if (-not $readme.StartsWith('# DSP Mirror Blueprint') -or
        $readme -notmatch 'Shift\+K') {
        throw 'Package README is missing required player-facing content.'
    }

    Add-Type -AssemblyName System.Drawing
    $iconEntry = $files | Where-Object FullName -CEQ 'icon.png'
    $iconStream = $iconEntry.Open()
    try {
        $icon = [System.Drawing.Image]::FromStream($iconStream)
        try {
            if ($icon.Width -ne 256 -or $icon.Height -ne 256 -or
                $icon.RawFormat.Guid -ne
                [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
                throw 'Package icon must be a 256 by 256 PNG.'
            }
        }
        finally {
            $icon.Dispose()
        }
    }
    finally {
        $iconStream.Dispose()
    }

    $dllEntry = $files | Where-Object {
        $_.FullName.Replace('\', '/') -ceq
            'BepInEx/plugins/DSP-Mirror-Blueprint/DSPMirrorBlueprint.dll'
    }
    $expectedHash = (
        Get-FileHash -LiteralPath $ExpectedDllPath -Algorithm SHA256
    ).Hash
    $dllStream = $dllEntry.Open()
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $actualHash = [BitConverter]::ToString(
                $sha256.ComputeHash($dllStream)
            ).Replace('-', '')
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $dllStream.Dispose()
    }
    if ($actualHash -cne $expectedHash) {
        throw 'Packaged DLL does not match the release build.'
    }
}
finally {
    $archive.Dispose()
}

Write-Output "Thunderstore package validation passed: $ExpectedVersion"
