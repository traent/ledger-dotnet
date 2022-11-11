using System;

namespace Traent.Ledger.Crypto {
    public interface ISignatureAlgorithm {
        int PublicKeyLengthInBytes { get; }
        int SecretKeyLengthInBytes { get; }
        int SignatureLengthInBytes { get; }

        void GenerateKeyPair(Span<byte> publicKey, Span<byte> secretKey);
        void GenerateKeyPair(out byte[] publicKey, out byte[] secretKey) {
            publicKey = new byte[PublicKeyLengthInBytes];
            secretKey = new byte[SecretKeyLengthInBytes];
            GenerateKeyPair(publicKey, secretKey);
        }

        void Sign(ReadOnlySpan<byte> secretKey, ReadOnlySpan<byte> message, Span<byte> signature);
        bool IsValidSignature(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature);
    }
}
