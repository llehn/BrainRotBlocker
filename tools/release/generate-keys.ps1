<#
.SYNOPSIS
  Generates a fresh ECDSA P-256 release signing key pair for the auto-updater.

.DESCRIPTION
  Prints:
    * the public key (SubjectPublicKeyInfo, base64) to embed in
      UpdateSignature.EmbeddedPublicKeyBase64, and
    * the private key (PKCS#8, base64) to store as the GitHub Actions secret
      BRD_UPDATE_PRIVATE_KEY (Settings -> Secrets and variables -> Actions).

  The private key must never be committed. Rotating the key means updating both
  the embedded public key and the secret, then publishing a release signed with
  the new key (clients on the old public key will simply stop updating until they
  are reinstalled).
#>
$ErrorActionPreference = 'Stop'

$ec = [System.Security.Cryptography.ECDsa]::Create(
    [System.Security.Cryptography.ECCurve]::CreateFromFriendlyName('nistP256'))

$public = [Convert]::ToBase64String($ec.ExportSubjectPublicKeyInfo())
$private = [Convert]::ToBase64String($ec.ExportPkcs8PrivateKey())

Write-Output "Public key (embed in UpdateSignature.EmbeddedPublicKeyBase64):"
Write-Output $public
Write-Output ""
Write-Output "Private key (store as GitHub secret BRD_UPDATE_PRIVATE_KEY):"
Write-Output $private
