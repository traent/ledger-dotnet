using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Traent.Ledger.Crypto {
    class LedgerCryptoProvider : ICryptoProvider {
        private readonly byte[] _ledgerId;

        public LedgerCryptoProvider(byte[] ledgerId) {
            _ledgerId = ledgerId;
        }

        public ISignatureAlgorithm CreateSignatureAlgorithm(string? algorithm) => algorithm switch {
            "ed25519" => Crypto.Algorithms.CreateEd25519(),
            _ => throw new ArgumentException(nameof(algorithm)),
        };

        public IOneShotHash CreateOneShotHash(string? algorithm) => algorithm switch {
            "hmacsha512(ledgerId)" => Crypto.Algorithms.CreateOneShotHmacSha512(_ledgerId),
            _ => throw new ArgumentException(nameof(algorithm)),
        };
    }

    public static class CryptoProvider {
        public static ICryptoProvider ForLedger(byte[] ledgerId) => new LedgerCryptoProvider(ledgerId);

        public static ICryptoProvider ForPublicKey(byte[] ledgerPublicKey) =>
            ForLedger(ComputeLedgerId(ledgerPublicKey));

        public static byte[] ComputeLedgerId(ReadOnlySpan<byte> ledgerPublicKey) =>
            JwkFromEd25519PublicKey(ledgerPublicKey).ComputeJwkThumbprintSHA512();

        private static JsonWebKey JwkFromEd25519PublicKey(ReadOnlySpan<byte> publicKey) => new() {
            Crv = "Ed25519",
            Kty = "OKP",
            X = Base64UrlEncoder.Encode(publicKey.ToArray()),
        };
    }

    static class JwkExtensions {
        public static string ComputeCanonicalJwk(this JsonWebKey jwk) =>
            JsonExtensions.SerializeToJson(jwk);

        public static byte[] ComputeJwkThumbprintSHA512(this JsonWebKey jwk) =>
            SHA512.HashData(Encoding.ASCII.GetBytes(jwk.ComputeCanonicalJwk()));
    }
}
