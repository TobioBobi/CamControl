# Creates a test signing key in CurrentUser\My; does not change certificate trust.
$ErrorActionPreference = 'Stop'
$subject = 'CN=Tobias Heer'
$friendlyName = 'CamControl MSIX local test only'
$certificate = Get-ChildItem Cert:/CurrentUser/My | Where-Object {
    $_.Subject -eq $subject -and $_.FriendlyName -eq $friendlyName -and
    $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddDays(7)
} | Sort-Object NotAfter -Descending | Select-Object -First 1
if (!$certificate) {
    $certificate = New-SelfSignedCertificate -Type Custom -Subject $subject -FriendlyName $friendlyName `
        -CertStoreLocation Cert:/CurrentUser/My -KeyUsage DigitalSignature -KeyExportPolicy NonExportable `
        -HashAlgorithm SHA256 -NotAfter (Get-Date).AddMonths(6) `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
}
$output = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/msix/test'
$null = New-Item -ItemType Directory -Path $output -Force
$certificatePath = Join-Path $output 'CamControl-Test.cer'
$null = Export-Certificate -Cert $certificate -FilePath $certificatePath
[pscustomobject]@{ Thumbprint = $certificate.Thumbprint; CertificatePath = $certificatePath }
