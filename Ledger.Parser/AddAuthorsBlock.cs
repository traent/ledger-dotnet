using System;
using System.Buffers;
using System.Collections.Generic;

namespace Traent.Ledger.Parser {
    public sealed class AddAuthorsBlock : IBlock {
        #region IBlock
        BlockType IBlock.Type => BlockType.AddAuthors;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public IReadOnlyList<ReadOnlyMemory<byte>> AuthorKeys { get; private init; }

        private AddAuthorsBlock(
            ReadOnlyMemory<byte> raw,
            IReadOnlyList<ReadOnlyMemory<byte>> authorKeys
        ) {
            Raw = raw;
            AuthorKeys = authorKeys;
        }

        internal static IBlock Read(ref BlockReader reader) {
            var authorKeys = reader.ReadSizedBuffers();
            return new AddAuthorsBlock(reader.Raw, authorKeys);
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder MakeAddAuthorsBlock(IReadOnlyList<ReadOnlyMemory<byte>> authorKeys) =>
            new AddAuthorsBlockBuilder(authorKeys);

        sealed class AddAuthorsBlockBuilder : IBlockBuilder {
            IReadOnlyList<ReadOnlyMemory<byte>> AuthorKeys { get; init; }

            internal AddAuthorsBlockBuilder(IReadOnlyList<ReadOnlyMemory<byte>> authorKeys) {
                AuthorKeys = authorKeys;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.AddAuthors);
                writer.Write(AuthorKeys);
            }
        }
    }
}
