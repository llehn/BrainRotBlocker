<#
.SYNOPSIS
  Signs an update manifest with the ECDSA P-256 release private key.

.DESCRIPTION
  Produces "<manifest>.sig": a DER-encoded (RFC 3279) ECDSA/SHA-256 signature
  over the exact bytes of the manifest file. This is the format
  UpdateSignature.Verify expects in the app. Used by the release workflow.

.PARAMETER ManifestPath
  Path to update.json.

.PARAMETER PrivateKeyBase64
  Base64 PKCS#8 private key (from the BRD_UPDATE_PRIVATE_KEY secret). If omitted,
  the BRD_UPDATE_PRIVATE_KEY environment variable is used.

.PARAMETER OutPath
  Output signature path. Defaults to "<ManifestPath>.sig".
#>
param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [string]$PrivateKeyBase64 = $env:BRD_UPDATE_PRIVATE_KEY,
    [string]$OutPath
)
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PrivateKeyBase64)) {
    throw "No private key provided. Pass -PrivateKeyBase64 or set BRD_UPDATE_PRIVATE_KEY."
}
if (-not $OutPath) {
    $OutPath = "$ManifestPath.sig"
}

$content = [System.IO.File]::ReadAllBytes((Resolve-Path $ManifestPath))
$keyBytes = [Convert]::FromBase64String($PrivateKeyBase64)

$ec = [System.Security.Cryptography.ECDsa]::Create()
$bytesRead = 0
$ec.ImportPkcs8PrivateKey($keyBytes, [ref]$bytesRead)

$signature = $ec.SignData(
    $content,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.DSASignatureFormat]::Rfc3279DerSequence)

[System.IO.File]::WriteAllBytes($OutPath, $signature)
Write-Output "Wrote signature: $OutPath ($($signature.Length) bytes)"
