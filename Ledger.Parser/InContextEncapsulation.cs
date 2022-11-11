using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public sealed class InContextEncapsulation : IEncapsulation {
        #region IBlock
        BlockType IBlock.Type => BlockType.InContext;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public ReadOnlyMemory<byte> ContextLinkHash { get; private init; }
        public IBlock Inner { get; private init; }

        private InContextEncapsulation(
            ReadOnlyMemory<byte> raw,
            ReadOnlyMemory<byte> contextLinkHash,
            IBlock inner
        ) {
            Raw = raw;
            ContextLinkHash = contextLinkHash;
            Inner = inner;
        }

        internal static IBlock Read(ref BlockReader reader) {
            var contextLinkHash = reader.ReadSizedBuffer();
            var inner = reader.ReadBlock();
            return new InContextEncapsulation(reader.Raw, contextLinkHash, inner);
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder AddInContextEncapsulation(this IBlockBuilder inner, ReadOnlyMemory<byte> contextLinkHash) =>
            new InContextEncapsulationBuilder(contextLinkHash, inner);

        sealed class InContextEncapsulationBuilder : IBlockBuilder {
            ReadOnlyMemory<byte> ContextLinkHash { get; init; }
            IBlockBuilder Inner { get; init; }

            internal InContextEncapsulationBuilder(ReadOnlyMemory<byte> contextLinkHash, IBlockBuilder inner) {
                ContextLinkHash = contextLinkHash;
                Inner = inner;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.InContext);
                writer.Write(ContextLinkHash);
                writer.Write(Inner);
            }
        }
    }
}
