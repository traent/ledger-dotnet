using System;
using System.Buffers;
using System.Collections.Generic;

namespace Traent.Ledger.Parser {
    public sealed class ReferenceBlock : IBlock {
        #region IBlock
        BlockType IBlock.Type => BlockType.Reference;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public IReadOnlyList<ReadOnlyMemory<byte>> Hashes { get; private init; }

        private ReferenceBlock(
            ReadOnlyMemory<byte> raw,
            IReadOnlyList<ReadOnlyMemory<byte>> hashes
        ) {
            Raw = raw;
            Hashes = hashes;
        }

        internal static IBlock Read(ref BlockReader reader) {
            return new ReferenceBlock(reader.Raw, reader.ReadSizedBuffers());
        }
    }

    public static partial class BlockBuilder {
        public static IBlockBuilder MakeReferenceBlock(IReadOnlyList<ReadOnlyMemory<byte>> hashes) =>
            new ReferenceBlockBuilder(hashes);

        sealed class ReferenceBlockBuilder : IBlockBuilder {
            IReadOnlyList<ReadOnlyMemory<byte>> Hashes { get; init; }

            internal ReferenceBlockBuilder(IReadOnlyList<ReadOnlyMemory<byte>> hashes) {
                Hashes = hashes;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.Reference);
                writer.Write(Hashes);
            }
        }
    }
}
