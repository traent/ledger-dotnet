using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class SealedBoxTests {
        const int TagLength = 16;
        const int EphemeralPublicKeyLength = 32;

        private readonly byte[] _signatureKey;
        private readonly byte[] _signatureSecretKey;

        private readonly byte[] _recipientKey;
        private readonly byte[] _recipientSecretKey;

        public SealedBoxTests() {
            var signatureAlgorithm = Algorithms.CreateEd25519();
            signatureAlgorithm.GenerateKeyPair(out var publicKey, out var secretKey);
            _signatureKey = publicKey;
            _signatureSecretKey = secretKey;

            var sealedBox = Algorithms.CreateXChaCha20Poly1305SealedBox();
            _recipientKey = sealedBox.PublicKeyFromSignatureKey(publicKey);
            _recipientSecretKey = sealedBox.SecretKeyFromSignatureKey(secretKey);
        }

        [Fact]
        public void PublicKeyConversionRejectsBadKeys() {
            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();
            _ = Assert.Throws<CryptographicException>(() =>
                a.PublicKeyFromSignatureKey(new byte[_signatureKey.Length])
            );
        }

        [Fact]
        public void PublicKeyConversionRejectsWrongKeyLength() {
            var signatureAlgorithm = Algorithms.CreateEd25519();

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();
            _ = Assert.Throws<ArgumentException>(() =>
                a.PublicKeyFromSignatureKey(_signatureKey[1..])
            );
        }

        [Fact]
        public void SecretKeyConversionRejectsWrongKeyLength() {
            var signatureAlgorithm = Algorithms.CreateEd25519();

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();
            _ = Assert.Throws<ArgumentException>(() =>
                a.SecretKeyFromSignatureKey(_signatureSecretKey[1..])
            );
        }

        [Fact]
        public void ComputeSharedKeyRejectsBadKeys() {
            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();
            _ = Assert.Throws<CryptographicException>(() =>
                a.ComputeSharedKey(new byte[32], new byte[_recipientSecretKey.Length])
            );
        }

        [Fact]
        public void EncryptRejectsWrongKeyLength() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            _ = Assert.Throws<ArgumentException>(() => a.Seal(plainText, _recipientKey[1..]));
        }

        [Fact]
        public void DecryptRejectsShortInput() {
            var plainText = Encoding.UTF8.GetBytes("");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();
            var encrypted = a.Seal(plainText, _recipientKey);

            _ = Assert.Throws<ArgumentException>(() => a.Open(encrypted[1..], _recipientSecretKey, _recipientKey));
        }

        [Fact]
        public void DecryptRejectsWrongKeyLength() {
            var plainText = Encoding.UTF8.GetBytes("");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();
            var encrypted = a.Seal(plainText, _recipientKey);

            _ = Assert.Throws<ArgumentException>(() => a.Open(encrypted, _recipientSecretKey[1..], _recipientKey));
        }

        [Fact]
        public void CheckBoxLength() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            var encrypted = a.Seal(plainText, _recipientKey);
            Assert.Equal(EphemeralPublicKeyLength + plainText.Length + TagLength, encrypted.Length);
        }

        [Fact]
        public void CanOpenBox() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            var encrypted = a.Seal(plainText, _recipientKey);
            var decrypted = a.Open(encrypted, _recipientSecretKey, _recipientKey);
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void CannotOpenDamagedBox() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            var encrypted = a.Seal(plainText, _recipientKey);
            encrypted[42]++;
            _ = Assert.Throws<CryptographicException>(() => a.Open(encrypted, _recipientSecretKey, _recipientKey));
        }

        [Fact]
        public void CannotOpenUsingWrongSecret() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");
            var wrongSecretKey = new byte[_recipientSecretKey.Length];
            new Random().NextBytes(wrongSecretKey);

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            var encrypted = a.Seal(plainText, _recipientKey);
            _ = Assert.Throws<CryptographicException>(() => a.Open(encrypted, wrongSecretKey, _recipientKey));
        }

        [Fact]
        public void CanOpenUsingSharedKey() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            var encrypted = a.Seal(plainText, _recipientKey);
            var sharedKey = a.ComputeSharedKey(encrypted[..EphemeralPublicKeyLength], _recipientSecretKey);
            var decrypted = a.OpenFromSharedKey(encrypted, sharedKey, _recipientKey);
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void CanOpenUsingSharedKeyFromBox() {
            var plainText = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");

            var a = Algorithms.CreateXChaCha20Poly1305SealedBox();

            var encrypted = a.Seal(plainText, _recipientKey);
            var sharedKey = a.ComputeSharedKeyFromBox(encrypted, _recipientSecretKey);
            var decrypted = a.OpenFromSharedKey(encrypted, sharedKey, _recipientKey);
            Assert.Equal(plainText, decrypted);
        }
    }
}
