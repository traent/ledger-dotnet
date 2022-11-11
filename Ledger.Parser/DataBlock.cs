using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public sealed class DataBlock : IBlock {
        #region IBlock
        BlockType IBlock.Type => BlockType.Data;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public ReadOnlyMemory<byte> Data { get; private init; }

        private DataBlock(
            ReadOnlyMemory<byte> raw,
            ReadOnlyMemory<byte> data
        ) {
            Raw = raw;
            Data = data;
        }

        internal static IBlock Read(ref BlockReader reader) {
            // the buffer is intentionally unsized to enable streaming
            var data = reader.Remaining;
            reader.AdvanceToEnd();
            return new DataBlock(reader.Raw, data);
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder MakeDataBlock(ReadOnlyMemory<byte> data) =>
            new DataBlockBuilder(data);

        sealed class DataBlockBuilder : IBlockBuilder {
            ReadOnlyMemory<byte> Data { get; init; }

            internal DataBlockBuilder(ReadOnlyMemory<byte> data) {
                Data = data;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.Data);
                // the span is intentionally written with no size header to enable streaming
                writer.Write(Data.Span);
            }
        }
    }
}
