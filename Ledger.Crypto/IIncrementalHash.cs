using System;

namespace Traent.Ledger.Crypto {
    // based on System.Security.Cryptography.IncrementalHash
    public interface IIncrementalHash : IDisposable {
        int HashLengthInBytes { get; }
        void AppendData(ReadOnlySpan<byte> data);
        int GetCurrentHash(Span<byte> destination);
    }
}
