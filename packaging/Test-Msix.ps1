[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath))
function Assert-Package([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
}
function Read-Entry([string]$Name) {
    $entry = $archive.GetEntry($Name)
    Assert-Package ($null -ne $entry) "Package is missing $Name."
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
}
try {
    $files = @($archive.Entries | ForEach-Object FullName)
    foreach ($required in @('CamControl.exe', 'CamControl.dll', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll',
        'PresentationFramework.dll', 'PresentationCore.dll', 'WindowsBase.dll', 'AppxManifest.xml', 'AppxBlockMap.xml',
        'de/CamControl.resources.dll', 'es/CamControl.resources.dll',
        'Assets/StoreLogo.png', 'Assets/Square44x44Logo.png', 'Assets/Square150x150Logo.png')) {
        Assert-Package ($files -contains $required) "Package is missing $required."
    }
    $unexpected = @($files | Where-Object {
        $_ -match '(?i)(^|/)(CamDummy|Tests|\.git)(/|\.)|\.(pfx|pem|key|cs|ps1)$|(^|/)(settings|buttons|automatic-positions|appearance|language)\.json$'
    })
    Assert-Package ($unexpected.Count -eq 0) "Unexpected files in package: $($unexpected -join ', ')"
    $runtime = (Read-Entry 'CamControl.runtimeconfig.json') | ConvertFrom-Json
    Assert-Package ($null -eq $runtime.runtimeOptions.PSObject.Properties['framework'] -and
        $null -eq $runtime.runtimeOptions.PSObject.Properties['frameworks']) 'Package requires a separately installed runtime.'
    Assert-Package (@($runtime.runtimeOptions.includedFrameworks).Count -ge 2) 'Self-contained desktop runtime metadata is missing.'
    [xml]$manifest = Read-Entry 'AppxManifest.xml'
    $languages = @($manifest.Package.Resources.Resource | ForEach-Object { $_.Language })
    Assert-Package ($languages -contains 'de-DE' -and $languages -contains 'en-US' -and $languages -contains 'es-ES') 'Package language declarations are incomplete.'
    Assert-Package ($manifest.Package.Applications.Application.Executable -eq 'CamControl.exe') 'Wrong app entry executable.'
    $application = $manifest.Package.Applications.Application
    Assert-Package ($application.GetAttribute('TrustLevel', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/10') -eq 'mediumIL') 'Unexpected app privilege level.'
    $version = [version]$manifest.Package.Identity.Version
    Assert-Package ($version.Revision -eq 0) 'MSIX revision must be 0.'
    $exe = $archive.GetEntry('CamControl.exe').Open()
    $memory = [System.IO.MemoryStream]::new()
    try { $exe.CopyTo($memory); $bytes = $memory.ToArray() } finally { $exe.Dispose(); $memory.Dispose() }
    $peOffset = [BitConverter]::ToInt32($bytes, 0x3c)
    $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
    $expectedMachine = @{ x64 = 0x8664; x86 = 0x14c; arm64 = 0xaa64 }[[string]$manifest.Package.Identity.ProcessorArchitecture]
    Assert-Package ($machine -eq $expectedMachine) 'Executable architecture differs from the manifest.'
    Write-Host "MSIX checks passed: $($manifest.Package.Identity.Name), $version, $($manifest.Package.Identity.ProcessorArchitecture), $($files.Count) files."
}
finally { $archive.Dispose() }
