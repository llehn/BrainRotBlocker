# Release signing

The auto-updater only applies updates whose manifest is signed by the project's
ECDSA P-256 release key. The public half is embedded in the app
(`UpdateSignature.EmbeddedPublicKeyBase64`); the private half signs `update.json`
in CI and must never be committed.

## One-time setup

1. The private key for the current embedded public key is in
   `tools/release/private-key.b64` (gitignored). Add it as a GitHub Actions
   secret named **`BRD_UPDATE_PRIVATE_KEY`**:
   *Repo → Settings → Secrets and variables → Actions → New repository secret.*
2. Once the secret is set, you can delete the local `private-key.b64`.

## Cutting a release

There is no manual step. Every push to `master` runs the `release` workflow:
test → build → hash → write `update.json` → sign → publish. The version is
`1.0.<commit-count>` (always increasing). A failing test blocks the release.

```sh
git push origin master
```

Installed clients check GitHub Releases periodically, verify the signature and
the exe's SHA-256, and silently swap to the new build (forward only — never a
downgrade or reinstall).

## Rotating the key

```sh
pwsh tools/release/generate-keys.ps1
```

Paste the new public key into `UpdateSignature.EmbeddedPublicKeyBase64`, update
the `BRD_UPDATE_PRIVATE_KEY` secret, and ship a release signed with the new key.
Clients still on the old embedded key will stop updating until reinstalled, so
rotate only when necessary.

## Signing manually (debugging)

```sh
pwsh tools/release/sign-manifest.ps1 -ManifestPath update.json -PrivateKeyBase64 (Get-Content tools/release/private-key.b64 -Raw)
```
