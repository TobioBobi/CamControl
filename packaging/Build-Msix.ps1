[CmdletBinding()]
param(
    [ValidateSet('Test', 'Store')][string]$Mode = 'Test',
    [string]$StoreIdentityPath,
    [ValidateSet('x64', 'arm64', 'x86')][string]$Architecture = 'x64',
    [string]$CertificateThumbprint
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'CamControl/CamControl.csproj'
$toolsProject = Join-Path $PSScriptRoot 'PackageTools.csproj'
$nugetConfig = Join-Path $PSScriptRoot 'NuGet.config'

function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable failed with exit code $LASTEXITCODE." }
}

$identity = [pscustomobject]@{
    Name = 'CamControl.LocalTest'; Publisher = 'CN=Tobias Heer'
    PublisherDisplayName = 'Tobias Heer'; DisplayName = 'CamControl (Test)'
}
if ($Mode -eq 'Store') {
    if (!$StoreIdentityPath) { throw 'Store builds require -StoreIdentityPath with the exact Partner Center identity.' }
    $identity = Get-Content -LiteralPath $StoreIdentityPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($field in @('Name', 'Publisher', 'PublisherDisplayName', 'DisplayName')) {
        if (!$identity.PSObject.Properties[$field] -or [string]::IsNullOrWhiteSpace($identity.$field) -or $identity.$field -match 'REPLACE_WITH') {
            throw "Store identity field $field is missing or still a placeholder."
        }
    }
    if ($identity.Name -eq 'CamControl.LocalTest') { throw 'The local test identity cannot be used for Store submission.' }
    if ($CertificateThumbprint) { throw 'Local test signing is not used for Store builds.' }
}
if ($identity.Name -notmatch '^[A-Za-z0-9][A-Za-z0-9.-]{2,49}$') { throw 'Invalid MSIX identity name.' }
$null = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new($identity.Publisher)
[xml]$projectXml = Get-Content -LiteralPath $project -Encoding UTF8
$appVersion = [version]([string]$projectXml.Project.PropertyGroup.Version)
if ($appVersion.Major -lt 1 -or $appVersion.Build -lt 0 -or
    $appVersion.Major -gt 65535 -or $appVersion.Minor -gt 65535 -or $appVersion.Build -gt 65535 -or $appVersion.Revision -gt 0) {
    throw 'App Version must be major.minor.build, with major > 0 and components <= 65535; MSIX revision is reserved as 0.'
}
$packageVersion = '{0}.{1}.{2}.0' -f $appVersion.Major, $appVersion.Minor, $appVersion.Build

Invoke-Checked 'dotnet' @('restore', $toolsProject, '--configfile', $nugetConfig)
$sdkRoot = (& dotnet msbuild $toolsProject '-getProperty:PkgMicrosoft_Windows_SDK_BuildTools' '-nologo' | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or !$sdkRoot) { throw 'Cannot locate Windows SDK BuildTools.' }
$makeAppx = Get-ChildItem -LiteralPath (Join-Path $sdkRoot 'bin') -Filter makeappx.exe -Recurse |
    Where-Object { $_.Directory.Name -eq 'x64' } | Select-Object -First 1 -ExpandProperty FullName
if (!$makeAppx) { throw 'MakeAppx.exe is missing from Windows SDK BuildTools.' }

# Fresh staging directories avoid stale publish files, without deleting a checkout.
$outputRoot = Join-Path $repoRoot ('artifacts/msix/{0}/{1}' -f $Mode.ToLowerInvariant(), $Architecture)
$staging = Join-Path $outputRoot ('staging-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $staging -Force
Invoke-Checked 'dotnet' @('publish', $project, '-c', 'Release', '-r', "win-$Architecture", '--self-contained', 'true',
    '--configfile', $nugetConfig, '-p:PublishSingleFile=false', '-p:PublishTrimmed=false', '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $staging)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Assets') -Destination $staging -Recurse
[xml]$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'AppxManifest.xml') -Encoding UTF8
$manifest.Package.Identity.SetAttribute('Name', $identity.Name)
$manifest.Package.Identity.SetAttribute('Publisher', $identity.Publisher)
$manifest.Package.Identity.SetAttribute('Version', $packageVersion)
$manifest.Package.Identity.SetAttribute('ProcessorArchitecture', $Architecture)
$manifest.Package.Properties.DisplayName = $identity.DisplayName
$manifest.Package.Properties.PublisherDisplayName = $identity.PublisherDisplayName
$namespaces = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
$namespaces.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')
$manifest.SelectSingleNode('//uap:VisualElements', $namespaces).SetAttribute('DisplayName', $identity.DisplayName)
$manifest.Save((Join-Path $staging 'AppxManifest.xml'))
$package = Join-Path $outputRoot ("CamControl-$packageVersion-$Architecture-$($Mode.ToLowerInvariant()).msix")
$packLog = Join-Path $outputRoot 'makeappx.log'
& $makeAppx pack /d $staging /p $package /o /h SHA256 *> $packLog
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed. See $packLog" }
& (Join-Path $PSScriptRoot 'Test-Msix.ps1') -PackagePath $package

if ($CertificateThumbprint) {
    $certificate = Get-Item -LiteralPath "Cert:/CurrentUser/My/$CertificateThumbprint"
    if (!$certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date) -or $certificate.Subject -ne $identity.Publisher) {
        throw 'A valid certificate with private key and matching Publisher subject is required.'
    }
    $signTool = Join-Path (Split-Path $makeAppx -Parent) 'signtool.exe'
    Invoke-Checked $signTool @('sign', '/fd', 'SHA256', '/sha1', $CertificateThumbprint, '/s', 'My', $package)
}
Write-Host "MSIX created: $package"
if (!$CertificateThumbprint) { Write-Host 'Unsigned package. Store signs Store submissions; local installation needs a trusted test signature.' }
