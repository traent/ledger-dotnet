using System;
using System.Buffers;
using System.Collections.Generic;
using Leb128;

namespace Traent.Ledger.Parser {
    public interface IBlockBuilder {
        void Write(IBufferWriter<byte> writer);
    }

    public static partial class BlockBuilder {
        public static byte[] ToBytes(this IBlockBuilder value) {
            var buffer = new ArrayBufferWriter<byte>();
            buffer.Write(value);
            return buffer.WrittenSpan.ToArray();
        }

        public static void Write(this IBufferWriter<byte> writer, IBlockBuilder value) {
            value.Write(writer);
        }

        private static void Write(this IBufferWriter<byte> writer, ulong value) {
            writer.WriteLeb128(value);
        }

        private static void Write(this IBufferWriter<byte> writer, BlockType value) {
            writer.Write((ulong)value);
        }

        private static void Write(this IBufferWriter<byte> writer, ReadOnlyMemory<byte> value) {
            writer.Write((ulong)value.Length);
            writer.Write(value.Span);
        }

        private static void Write(this IBufferWriter<byte> writer, IReadOnlyList<ReadOnlyMemory<byte>> value) {
            foreach (var x in value) {
                writer.Write(x);
            }
        }
    }
}
