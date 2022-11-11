using System;
using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class SignatureTests {
        [Fact]
        public static void CheckHashLengthProperty() {
            var a = Algorithms.CreateEd25519();

            Assert.Equal(256 / 8, a.PublicKeyLengthInBytes);
            Assert.Equal(512 / 8, a.SecretKeyLengthInBytes);
            Assert.Equal(512 / 8, a.SignatureLengthInBytes);
        }

        [Theory]
        [HexColumnFileData("ed25519.input")]
        public static void ImplementationMatchesTestVectors(HexString secretKey, HexString publicKey, HexString message, HexString signature) {
            Assert.Equal(secretKey[32..], publicKey);

            var a = Algorithms.CreateEd25519();
            Span<byte> newSignature = stackalloc byte[a.SignatureLengthInBytes];
            a.Sign(secretKey, message, newSignature);
            Assert.Equal(signature, HexString.Wrap(newSignature));

            // using a different message would result in a different signature
            a.Sign(secretKey, publicKey, newSignature);
            Assert.NotEqual(signature, HexString.Wrap(newSignature));
        }

        [Theory]
        [HexColumnFileData("ed25519.input")]
        public static void CanVerifyTestVectors(HexString secretKey, HexString publicKey, HexString message, HexString signature) {
            Assert.Equal(secretKey[32..], publicKey);

            var a = Algorithms.CreateEd25519();
            Assert.True(a.IsValidSignature(publicKey, message, signature));

            // using a different message would result in a signature mismatch
            Assert.False(a.IsValidSignature(publicKey, secretKey, signature));
        }

        [Fact]
        public static void CanGenerateKeys() {
            var a = Algorithms.CreateEd25519();
            Span<byte> secretKey = stackalloc byte[a.SecretKeyLengthInBytes];
            Span<byte> publicKey = stackalloc byte[a.PublicKeyLengthInBytes];
            a.GenerateKeyPair(publicKey, secretKey);
            Assert.Equal(HexString.Wrap(secretKey[32..]), HexString.Wrap(publicKey));
        }

        [Fact]
        public static void CanAllocateAndGenerateKeys() {
            var a = Algorithms.CreateEd25519();
            a.GenerateKeyPair(out var publicKey, out var secretKey);
            Assert.Equal(a.PublicKeyLengthInBytes, publicKey.Length);
            Assert.Equal(a.SecretKeyLengthInBytes, secretKey.Length);
            Assert.Equal(HexString.Wrap(secretKey[32..]), HexString.Wrap(publicKey));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(-1, 0)]
        [InlineData(0, 1)]
        [InlineData(0, -1)]
        public static void GenerateRejectsWrongSizes(int secretKeySizeOffset, int publicKeySizeOffset) {
            var a = Algorithms.CreateEd25519();
            var secretKey = new byte[a.SecretKeyLengthInBytes + secretKeySizeOffset];
            var publicKey = new byte[a.PublicKeyLengthInBytes + publicKeySizeOffset];

            _ = Assert.Throws<ArgumentException>(() => a.GenerateKeyPair(publicKey, secretKey));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(-1, 0)]
        [InlineData(0, 1)]
        [InlineData(0, -1)]
        public static void SignRejectsWrongSizes(int secretKeySizeOffset, int signatureSizeOffset) {
            var a = Algorithms.CreateEd25519();
            var secretKey = new byte[a.SecretKeyLengthInBytes + secretKeySizeOffset];
            var signature = new byte[a.SignatureLengthInBytes + signatureSizeOffset];

            _ = Assert.Throws<ArgumentException>(() => a.Sign(secretKey, Array.Empty<byte>(), signature));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(-1, 0)]
        [InlineData(0, 1)]
        [InlineData(0, -1)]
        public static void IsValidRejectsWrongSizes(int publicKeySizeOffset, int signatureSizeOffset) {
            var a = Algorithms.CreateEd25519();
            var publicKey = new byte[a.PublicKeyLengthInBytes + publicKeySizeOffset];
            var signature = new byte[a.SignatureLengthInBytes + signatureSizeOffset];

            _ = Assert.Throws<ArgumentException>(() => a.IsValidSignature(publicKey, Array.Empty<byte>(), signature));
        }
    }
}
