using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Traent.Ledger.Crypto {
    public static class Algorithms {
        private static readonly ISignatureAlgorithm _ed25519 = new Ed25519();
        private static readonly ISealedBox _sealedBox = new SealedBox();
        private static readonly IPadding _padding = new Iso7816d4Padding();

        public static ISignatureAlgorithm CreateEd25519() => _ed25519;

        public static IIncrementalHash CreateIncrementalHmacSha512(ReadOnlySpan<byte> key) =>
            new IncrementalHmacSha512(key);

        public static IOneShotHash CreateOneShotHmacSha512(ReadOnlySpan<byte> key) =>
            new OneShotHmacSha512(key);

        public static ISealedBox CreateXChaCha20Poly1305SealedBox() => _sealedBox;
        public static IPadding CreateIso7816d4Padding() => _padding;

        public static byte[] Sha512MerkleCombiner(ReadOnlySpan<byte> leftChild, ReadOnlySpan<byte> rightChild) {
            var hashLength = 512 / 8;

            Debug.Assert(leftChild.Length == hashLength);
            Debug.Assert(rightChild.Length == hashLength);

            Span<byte> source = stackalloc byte[1 + hashLength + hashLength];
            source[0] = 1;
            leftChild.CopyTo(source.Slice(1));
            rightChild.CopyTo(source.Slice(1 + leftChild.Length));

            return SHA512.HashData(source);
        }

        public static byte[] Sha512MerkleCombiner(byte[] leftChild, byte[] rightChild) =>
            Sha512MerkleCombiner(leftChild.AsSpan(), rightChild.AsSpan());

        public static byte[] Sha512MerkleLeaf(ReadOnlySpan<byte> leafValue) {
            var hashLength = 512 / 8;

            Debug.Assert(leafValue.Length == hashLength);

            Span<byte> source = stackalloc byte[1 + hashLength];
            source[0] = 0;
            leafValue.CopyTo(source.Slice(1));

            return SHA512.HashData(source);
        }
    }
}
