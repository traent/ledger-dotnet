using System;
using System.Security.Cryptography;

namespace Traent.Ledger.Crypto {
    sealed class OneShotHmacSha512 : IOneShotHash {
        public int HashLengthInBytes => _inner.HashLengthInBytes;

        private readonly IncrementalHash _inner;

        internal OneShotHmacSha512(ReadOnlySpan<byte> key) {
            _inner = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA512, key);
        }

        public void Dispose() => _inner.Dispose();

        public int ComputeHash(ReadOnlySpan<byte> source, Span<byte> destination) {
            _inner.AppendData(source);
            return _inner.GetHashAndReset(destination);
        }
    }

    sealed class IncrementalHmacSha512 : IIncrementalHash {
        public int HashLengthInBytes => _inner.HashLengthInBytes;

        private readonly IncrementalHash _inner;

        internal IncrementalHmacSha512(ReadOnlySpan<byte> key) {
            _inner = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA512, key);
        }

        public void Dispose() => _inner.Dispose();

        public void AppendData(ReadOnlySpan<byte> data) => _inner.AppendData(data);
        public int GetCurrentHash(Span<byte> destination) => _inner.GetCurrentHash(destination);
    }
}
