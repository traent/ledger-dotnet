using System;
using System.Buffers;
using Traent.Ledger.Crypto;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator.Test {
    class TeeWriter<T> : IBufferWriter<T> {
        private readonly ArrayBufferWriter<T> _buffer = new();
        private readonly IBufferWriter<T> _writer;

        public ReadOnlySpan<T> WrittenSpan => _buffer.WrittenSpan;

        public TeeWriter(IBufferWriter<T> writer) {
            _writer = writer;
        }

        public void Advance(int count) {
            _buffer.Advance(count);
            _writer.Write(WrittenSpan[^count..]);
        }

        public Memory<T> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);
        public Span<T> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);
    }

    class Signer : ISigner {
        private readonly byte[] _secretKey;
        private readonly TeeWriter<byte> _teeWriter;
        private readonly ISignatureAlgorithm _algo = Crypto.Algorithms.CreateEd25519();

        public IBufferWriter<byte> Writer => _teeWriter;
        public int SignatureLengthInBytes => _algo.SignatureLengthInBytes;

        public Signer(byte[] secretKey, IBufferWriter<byte> writer) {
            _teeWriter = new(writer);
            _secretKey = secretKey;
        }

        public void CreateSignature(Span<byte> signature) {
            _algo.Sign(_secretKey, _teeWriter.WrittenSpan, signature);
        }
    }
}
