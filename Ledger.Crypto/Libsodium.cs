using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Traent.Ledger.Crypto {
    static class Libsodium {
#if IOS
        private const string DllName = "__Internal";
#else
        private const string DllName = "libsodium";
#endif
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sodium_init();

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_sign_keypair(byte* publicKey, byte* secretKey);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_sign_detached(byte* buffer, out ulong bufferLength, byte* message, ulong messageLength, byte* key);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_sign_verify_detached(byte* signature, byte* message, ulong messageLength, byte* key);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_sign_ed25519_pk_to_curve25519(byte* curve25519PublicKey, byte* ed25519PublicKey);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_sign_ed25519_sk_to_curve25519(byte* curve25519OSecretKey, byte* ed25519SecretKey);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_aead_chacha20poly1305_ietf_encrypt_detached(
            byte* c, byte* mac, ulong* maclen_p, byte* m, ulong mlen, byte* ad, ulong adlen, byte* nsec, byte* npub, byte* k);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_aead_chacha20poly1305_ietf_decrypt_detached(
            byte* m, byte* nsec, byte* c, ulong clen, byte* mac, byte* ad, ulong adlen, byte* npub, byte* k);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_box_curve25519xchacha20poly1305_keypair(byte* pk, byte* sk);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int crypto_box_curve25519xchacha20poly1305_beforenm(byte* k, byte* pk, byte* sk);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int sodium_pad(
            out ulong padded_buflen_p, byte* buf, ulong unpadded_buflen, ulong blocksize, ulong max_buflen);

        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal static extern unsafe int sodium_unpad(
            out ulong unpadded_buflen_p, byte* buf, ulong padded_buflen, ulong blocksize);

        static Libsodium() {
            _ = sodium_init();
        }
    }

    sealed class Ed25519 : ISignatureAlgorithm {
        public Ed25519() {
        }

        public int PublicKeyLengthInBytes => 32;
        public int SecretKeyLengthInBytes => 64;
        public int SignatureLengthInBytes => 64;

        [DoesNotReturn]
        private void BadLength(string paramName) {
            throw new ArgumentException("Invalid size", paramName);
        }

        public void GenerateKeyPair(Span<byte> publicKey, Span<byte> secretKey) {
            if (publicKey.Length != PublicKeyLengthInBytes) {
                BadLength(nameof(publicKey));
            }

            if (secretKey.Length != SecretKeyLengthInBytes) {
                BadLength(nameof(secretKey));
            }

            unsafe {
                fixed (byte* pk = publicKey, sk = secretKey) {
                    _ = Libsodium.crypto_sign_keypair(pk, sk);
                }
            }
        }

        public void Sign(ReadOnlySpan<byte> secretKey, ReadOnlySpan<byte> message, Span<byte> signature) {
            if (secretKey.Length != SecretKeyLengthInBytes) {
                BadLength(nameof(secretKey));
            }

            if (signature.Length != SignatureLengthInBytes) {
                BadLength(nameof(signature));
            }

            unsafe {
                fixed (byte* sig = signature, msg = message, sk = secretKey) {
                    _ = Libsodium.crypto_sign_detached(sig, out var len, msg, (ulong)message.Length, sk);
                }
            }
        }

        public bool IsValidSignature(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) {
            if (publicKey.Length != PublicKeyLengthInBytes) {
                BadLength(nameof(publicKey));
            }

            if (signature.Length != SignatureLengthInBytes) {
                BadLength(nameof(signature));
            }

            unsafe {
                fixed (byte* sig = signature, msg = message, pk = publicKey) {
                    return 0 == Libsodium.crypto_sign_verify_detached(sig, msg, (ulong)message.Length, pk);
                }
            }
        }
    }

    sealed class SealedBox : ISealedBox {
        public SealedBox() {
        }

        public int SignaturePublicKeyLengthInBytes => 32;
        public int SignatureSecretKeyLengthInBytes => 64;

        public int PublicKeyLengthInBytes => 32;
        public int SecretKeyLengthInBytes => 32;

        /// <summary>
        /// size of the key derived from the composition of ephemeral and recipient key
        /// </summary>
        public int SharedKeyLengthInBytes => 32;

        public int TagLengthInBytes => 16;
        public int NonceLengthInBytes => 12;

        [DoesNotReturn]
        private void BadLength(string paramName) {
            throw new ArgumentException("Invalid size", paramName);
        }

        public byte[] PublicKeyFromSignatureKey(ReadOnlySpan<byte> ed25519) {
            var curve25519 = new byte[PublicKeyLengthInBytes];
            PublicKeyFromSignatureKey(curve25519, ed25519);
            return curve25519;
        }

        private void PublicKeyFromSignatureKey(Span<byte> curve25519, ReadOnlySpan<byte> ed25519) {
            if (ed25519.Length != SignaturePublicKeyLengthInBytes) {
                BadLength(nameof(ed25519));
            }

            if (curve25519.Length != PublicKeyLengthInBytes) {
                BadLength(nameof(curve25519));
            }

            unsafe {
                fixed (byte* curveKey = curve25519, edKey = ed25519) {
                    var ret = Libsodium.crypto_sign_ed25519_pk_to_curve25519(curveKey, edKey);
                    if (ret != 0) {
                        throw new CryptographicException();
                    }
                }
            }
        }

        public byte[] SecretKeyFromSignatureKey(ReadOnlySpan<byte> ed25519) {
            var curve25519 = new byte[SecretKeyLengthInBytes];
            SecretKeyFromSignatureKey(curve25519, ed25519);
            return curve25519;
        }

        private void SecretKeyFromSignatureKey(Span<byte> curve25519, ReadOnlySpan<byte> ed25519) {
            if (ed25519.Length != SignatureSecretKeyLengthInBytes) {
                BadLength(nameof(ed25519));
            }

            if (curve25519.Length != SecretKeyLengthInBytes) {
                BadLength(nameof(curve25519));
            }

            unsafe {
                fixed (byte* curveKey = curve25519, edKey = ed25519) {
                    var ret = Libsodium.crypto_sign_ed25519_sk_to_curve25519(curveKey, edKey);
                    if (ret != 0) {
                        throw new CryptographicException();
                    }
                }
            }
        }

        private void GenerateKey(Span<byte> publicKey, Span<byte> secretKey) {
            if (publicKey.Length != PublicKeyLengthInBytes) {
                BadLength(nameof(publicKey));
            }

            if (secretKey.Length != SecretKeyLengthInBytes) {
                BadLength(nameof(secretKey));
            }

            unsafe {
                fixed (byte* pk = publicKey, sk = secretKey) {
                    var ret = Libsodium.crypto_box_curve25519xchacha20poly1305_keypair(pk, sk);
                    if (ret != 0) {
                        throw new CryptographicException();
                    }
                }
            }
        }

        public byte[] ComputeSharedKeyFromBox(ReadOnlySpan<byte> box, ReadOnlySpan<byte> secretKey) {
            return ComputeSharedKey(box[..PublicKeyLengthInBytes], secretKey);
        }

        public byte[] ComputeSharedKey(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> secretKey) {
            var sharedKey = new byte[SharedKeyLengthInBytes];
            ComputeSharedKey(sharedKey, publicKey, secretKey);
            return sharedKey;
        }

        private void ComputeSharedKey(Span<byte> sharedKey, ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> secretKey) {
            if (sharedKey.Length != SharedKeyLengthInBytes) {
                BadLength(nameof(sharedKey));
            }

            if (publicKey.Length != PublicKeyLengthInBytes) {
                BadLength(nameof(publicKey));
            }

            if (secretKey.Length != SecretKeyLengthInBytes) {
                BadLength(nameof(secretKey));
            }

            unsafe {
                fixed (byte* k = sharedKey, pk = publicKey, sk = secretKey) {
                    var ret = Libsodium.crypto_box_curve25519xchacha20poly1305_beforenm(k, pk, sk);
                    if (ret != 0) {
                        throw new CryptographicException();
                    }
                }
            }
        }

        private void ComputeNonce(Span<byte> nonce, ReadOnlySpan<byte> publicEphemeralKey, ReadOnlySpan<byte> publicRecipientKey) {
            Span<byte> buffer = stackalloc byte[publicEphemeralKey.Length + publicRecipientKey.Length];
            Span<byte> hash = stackalloc byte[512 / 8];

            publicEphemeralKey.CopyTo(buffer);
            publicRecipientKey.CopyTo(buffer.Slice(publicEphemeralKey.Length));
            _ = SHA512.HashData(buffer, hash);
            hash.Slice(0, NonceLengthInBytes).CopyTo(nonce);
        }

        public byte[] Seal(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> publicKey) {
            var box = new byte[PublicKeyLengthInBytes + plainText.Length + TagLengthInBytes];
            Seal(box, plainText, publicKey);
            return box;
        }

        private void Seal(Span<byte> box, ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> publicKey) {
            var ephemeralPublicKey = box[..PublicKeyLengthInBytes];
            var cipherText = box[PublicKeyLengthInBytes..^TagLengthInBytes];
            var tag = box[^TagLengthInBytes..];

            Span<byte> ephemeralSecretKey = stackalloc byte[SecretKeyLengthInBytes];
            Span<byte> sharedKey = stackalloc byte[SharedKeyLengthInBytes];
            Span<byte> nonce = stackalloc byte[NonceLengthInBytes];

            GenerateKey(ephemeralPublicKey, ephemeralSecretKey);
            ComputeSharedKey(sharedKey, publicKey, ephemeralSecretKey);
            ComputeNonce(nonce, ephemeralPublicKey, publicKey);
            using var ae = new ChaCha20Poly1305(sharedKey);
            ae.Encrypt(nonce, plainText, cipherText, tag);
        }

        public byte[] Open(ReadOnlySpan<byte> box, ReadOnlySpan<byte> secretKey, ReadOnlySpan<byte> publicKey) {
            var plainTextLength = box.Length - PublicKeyLengthInBytes - TagLengthInBytes;
            if (plainTextLength < 0) {
                BadLength(nameof(box));
            }

            Span<byte> sharedKey = stackalloc byte[SharedKeyLengthInBytes];
            var ephemeralPublicKey = box[..PublicKeyLengthInBytes];
            ComputeSharedKey(sharedKey, ephemeralPublicKey, secretKey);

            return OpenFromSharedKey(box, sharedKey, publicKey);
        }

        public byte[] OpenFromSharedKey(ReadOnlySpan<byte> box, ReadOnlySpan<byte> sharedKey, ReadOnlySpan<byte> publicKey) {
            var plainTextLength = box.Length - PublicKeyLengthInBytes - TagLengthInBytes;
            if (plainTextLength < 0) {
                BadLength(nameof(box));
            }

            var plainText = new byte[plainTextLength];
            OpenFromSharedKey(plainText, box, sharedKey, publicKey);
            return plainText;
        }

        private void OpenFromSharedKey(Span<byte> plainText, ReadOnlySpan<byte> box, ReadOnlySpan<byte> sharedKey, ReadOnlySpan<byte> publicKey) {
            var ephemeralPublicKey = box[..PublicKeyLengthInBytes];
            var cipherText = box[PublicKeyLengthInBytes..^TagLengthInBytes];
            var tag = box[^TagLengthInBytes..];

            Span<byte> nonce = stackalloc byte[NonceLengthInBytes];

            ComputeNonce(nonce, ephemeralPublicKey, publicKey);
            using var ae = new ChaCha20Poly1305(sharedKey);
            ae.Decrypt(nonce, cipherText, tag, plainText);
        }
    }

    sealed class ChaCha20Poly1305 : IAuthenticatedEncryption {
        private readonly byte[] _key;

        public int KeyLengthInBytes => 32;
        public int TagLengthInBytes => 16;
        public int NonceLengthInBytes => 12;

        internal ChaCha20Poly1305(ReadOnlySpan<byte> key) {
            if (key.Length != KeyLengthInBytes) {
                throw new ArgumentException(nameof(key));
            }

            _key = key.ToArray();
        }

        [DoesNotReturn]
        private void BadLength(string paramName) {
            throw new ArgumentException("Invalid size", paramName);
        }

        public void Dispose() {
        }

        public void Decrypt(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext
        ) {
            if (nonce.Length != NonceLengthInBytes) {
                BadLength(nameof(nonce));
            }

            if (tag.Length != TagLengthInBytes) {
                BadLength(nameof(tag));
            }

            if (plaintext.Length != ciphertext.Length) {
                BadLength(nameof(plaintext));
            }

            unsafe {
                fixed (byte* m = plaintext, c = ciphertext, mac = tag, npub = nonce, k = _key) {
                    var ret = Libsodium.crypto_aead_chacha20poly1305_ietf_decrypt_detached(
                        m, null, c, (ulong)ciphertext.Length, mac, null, 0, npub, k);

                    if (ret != 0) {
                        throw new CryptographicException();
                    }
                }
            }
        }

        public void Encrypt(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag
        ) {
            if (nonce.Length != NonceLengthInBytes) {
                BadLength(nameof(nonce));
            }

            if (tag.Length != TagLengthInBytes) {
                BadLength(nameof(tag));
            }

            if (plaintext.Length != ciphertext.Length) {
                BadLength(nameof(ciphertext));
            }

            unsafe {
                fixed (byte* c = ciphertext, mac = tag, m = plaintext, npub = nonce, k = _key) {
                    var ret = Libsodium.crypto_aead_chacha20poly1305_ietf_encrypt_detached(
                        c, mac, null, m, (ulong)plaintext.Length, null, 0, null, npub, k);

                    if (ret != 0) {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
    }

    sealed class Iso7816d4Padding : IPadding {
        [DoesNotReturn]
        private void BadLength(string paramName) {
            throw new ArgumentException("Invalid size", paramName);
        }

        public int ComputePaddedLength(int dataLength, int blockSize) {
            if (dataLength < 0) {
                BadLength(nameof(dataLength));
            }

            if (blockSize <= 0) {
                BadLength(nameof(blockSize));
            }

            return dataLength + (blockSize - (dataLength % blockSize));
        }

        public void Pad(Span<byte> buffer, int dataLength, int blockSize) {
            if (dataLength < 0) {
                BadLength(nameof(dataLength));
            }

            if (blockSize <= 0) {
                BadLength(nameof(blockSize));
            }

            unsafe {
                fixed (byte* buf = buffer) {
                    var ret = Libsodium.sodium_pad(out var paddedLength, buf,
                        (ulong)dataLength, (ulong)blockSize, (ulong)buffer.Length);

                    if (ret != 0 || paddedLength != (ulong)buffer.Length) {
                        throw new ArgumentException(nameof(buffer));
                    }
                }
            }
        }

        public ReadOnlySpan<byte> Unpad(ReadOnlySpan<byte> buffer, int blockSize) {
            if (blockSize <= 0) {
                BadLength(nameof(blockSize));
            }

            unsafe {
                fixed (byte* buf = buffer) {
                    var ret = Libsodium.sodium_unpad(out var unpaddedLength, buf,
                        (ulong)buffer.Length, (ulong)blockSize);

                    if (ret != 0) {
                        throw new CryptographicException();
                    }

                    return buffer.Slice(0, (int)unpaddedLength);
                }
            }
        }
    }
}
