using System;

namespace Traent.Ledger.Crypto {
    public interface IOneShotHash : IDisposable {
        int HashLengthInBytes { get; }
        int ComputeHash(ReadOnlySpan<byte> source, Span<byte> destination);

        byte[] ComputeHash(ReadOnlySpan<byte> source) {
            var hash = new byte[HashLengthInBytes];
            _ = ComputeHash(source, hash);
            return hash;
        }
    }
}
