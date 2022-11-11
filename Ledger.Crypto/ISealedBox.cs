using System;

namespace Traent.Ledger.Crypto {
    public interface ISealedBox {
        /// <summary>
        /// Compute the shared ephemeral+recipient key
        /// </summary>
        /// <param name="publicKey">the ephemeral public key</param>
        /// <param name="secretKey">the secret key of the recipient</param>
        /// <returns>the shared ephemeral+recipient key</returns>
        byte[] ComputeSharedKey(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> secretKey);

        /// <summary>
        /// Open a sealed box with the secret key of the recipient
        /// </summary>
        /// <param name="box">the sealed box, i.e. the concatenation of ephemeral public key, ciphertext and authentication tag</param>
        /// <param name="secretKey">the secret key of the recipient</param>
        /// <param name="publicKey">the public key of the recipient</param>
        /// <returns>the plaintext extracted from the box</returns>
        byte[] Open(ReadOnlySpan<byte> box, ReadOnlySpan<byte> secretKey, ReadOnlySpan<byte> publicKey);

        /// <summary>
        /// Open a sealed box with the shared ephemeral+recipient key
        /// </summary>
        /// <param name="box">the sealed box, i.e. the concatenation of ephemeral public key, ciphertext and authentication tag</param>
        /// <param name="sharedKey">the shared ephemeral+recipient key</param>
        /// <param name="publicKey">the public key of the recipient</param>
        /// <returns>the plaintext extracted from the box</returns>
        byte[] OpenFromSharedKey(ReadOnlySpan<byte> box, ReadOnlySpan<byte> sharedKey, ReadOnlySpan<byte> publicKey);

        /// <summary>
        /// Generate a sealed box for a given recipient
        /// </summary>
        /// <param name="plainText">the plaintext to seal</param>
        /// <param name="publicKey">the public key of the recipient</param>
        /// <returns>the sealed box, i.e. the concatenation of ephemeral public key, ciphertext and authentication tag</returns>
        byte[] Seal(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> publicKey);

        byte[] PublicKeyFromSignatureKey(ReadOnlySpan<byte> ed25519);
        byte[] SecretKeyFromSignatureKey(ReadOnlySpan<byte> ed25519);
        byte[] ComputeSharedKeyFromBox(ReadOnlySpan<byte> box, ReadOnlySpan<byte> secretKey);
    }
}
