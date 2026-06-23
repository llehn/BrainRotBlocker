using System.IO;
using System.Security.Cryptography;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// Verifies that update artifacts came from the project's release key, not an
/// attacker (the whole point of a self-protection tool that can rewrite its own
/// binary). The manifest is signed with an ECDSA P-256 private key held only in
/// CI; the matching public key is embedded below. Signatures are DER-encoded
/// (RFC 3279) over the exact published bytes — the signing script must match.
/// </summary>
internal sealed class UpdateSignature
{
    /// <summary>
    /// Base64 SubjectPublicKeyInfo of the release verification key. The private
    /// half is a GitHub Actions secret (see tools/release/README). Replace both
    /// via tools/release/generate-keys.ps1 if the key is ever rotated.
    /// </summary>
    public const string EmbeddedPublicKeyBase64 =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEf/Nszc0+zcrA8i08q1nPe8+iffg5uJvrdvadW36LM40ImjJJPbb0voBcvdwbAW/YW6GZqKZrhqMCsUMh84N3BA==";

    private static readonly DSASignatureFormat SignatureFormat = DSASignatureFormat.Rfc3279DerSequence;

    private readonly byte[] _publicKeySpki;

    public UpdateSignature()
        : this(Convert.FromBase64String(EmbeddedPublicKeyBase64))
    {
    }

    /// <summary>Test seam: verify against a caller-supplied public key (SPKI bytes).</summary>
    internal UpdateSignature(byte[] publicKeySpki) => _publicKeySpki = publicKeySpki;

    /// <summary>True when <paramref name="signature"/> is a valid signature over <paramref name="content"/>.</summary>
    public bool Verify(byte[] content, byte[] signature)
    {
        try
        {
            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(_publicKeySpki, out _);
            return ecdsa.VerifyData(content, signature, HashAlgorithmName.SHA256, SignatureFormat);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>True when the file's SHA-256 equals <paramref name="expectedHex"/> (case-insensitive).</summary>
    public static bool VerifyFileHash(string path, string expectedHex)
    {
        string actual = ComputeFileSha256(path);
        return string.Equals(actual, expectedHex, StringComparison.OrdinalIgnoreCase);
    }

    public static string ComputeFileSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }
}
