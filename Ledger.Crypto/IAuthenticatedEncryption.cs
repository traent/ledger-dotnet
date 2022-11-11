using System;

namespace Traent.Ledger.Crypto {
    public interface IAuthenticatedEncryption : IDisposable {
        void Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext);
        void Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag);
    }
}
