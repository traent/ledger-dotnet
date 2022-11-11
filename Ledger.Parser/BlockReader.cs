using System;
using System.Buffers;
using System.Collections.Generic;
using Leb128;

namespace Traent.Ledger.Parser {
    struct BlockReader {
        public ReadOnlyMemory<byte> Remaining;
        public ReadOnlyMemory<byte> Raw { get; init; }
        public int MaxDepth { get; init; }

        public bool Finished() => Remaining.Length == 0;

        public void AdvanceToEnd() {
            Remaining = Remaining.Slice(Remaining.Length);
        }

        private static T Fail<T>() {
            throw new InvalidOperationException();
        }

        public ulong ReadLeb128() {
            var sequence = new ReadOnlySequence<byte>(Remaining);
            var reader = new SequenceReader<byte>(sequence);
            if (reader.TryReadLeb128(out ulong value)) {
                Remaining = reader.UnreadSequence.First;
                return value;
            } else {
                return Fail<ulong>();
            }
        }

        public ReadOnlyMemory<byte> ReadBuffer(ulong length) {
            if (length <= (ulong)Remaining.Length) {
                var intLength = (int)length;
                var result = Remaining.Slice(0, intLength);
                Remaining = Remaining.Slice(intLength);
                return result;
            } else {
                return Fail<ReadOnlyMemory<byte>>();
            }
        }

        public ReadOnlyMemory<byte> ReadFromEnd(ulong length) {
            if (length <= (ulong)Remaining.Length) {
                var intLength = (int)length;
                var remaining = Remaining.Length - intLength;
                var result = Remaining.Slice(remaining, intLength);
                Remaining = Remaining.Slice(0, remaining);
                return result;
            } else {
                return Fail<ReadOnlyMemory<byte>>();
            }
        }

        public ReadOnlyMemory<byte> ReadSizedBuffer() {
            return ReadBuffer(ReadLeb128());
        }

        public IReadOnlyList<ReadOnlyMemory<byte>> ReadSizedBuffers() {
            var values = new List<ReadOnlyMemory<byte>>();

            while (!Finished()) {
                values.Add(ReadSizedBuffer());
            }

            return values;
        }
    }
}
