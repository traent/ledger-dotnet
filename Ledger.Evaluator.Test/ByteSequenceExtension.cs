using System;
using System.Buffers;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator.Test {
    static class ByteSequenceExtension {
        public static ReadOnlyMemory<byte> ToMemory(this IBlockBuilder builder) {
            var writer = new ArrayBufferWriter<byte>();
            writer.Write(builder);
            return writer.WrittenMemory;
        }

        public static byte[] ToBytes(this object data) =>
            data switch {
                IBlockBuilder builder => builder.ToMemory().ToArray(),
                byte[] bytes => bytes,
                string s => System.Text.Encoding.UTF8.GetBytes(s),
                _ => throw new ArgumentException(null, nameof(data)),
            };

        public static ReadOnlyMemory<byte> AsReadOnlyMemory(this object data) => data.ToBytes();
    }
}
