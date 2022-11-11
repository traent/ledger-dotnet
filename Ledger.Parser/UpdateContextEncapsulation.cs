using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public sealed class UpdateContextEncapsulation : IEncapsulation {
        #region IBlock
        BlockType IBlock.Type => BlockType.UpdateContext;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public IBlock Inner { get; private init; }

        private UpdateContextEncapsulation(
            ReadOnlyMemory<byte> raw,
            IBlock inner
        ) {
            Raw = raw;
            Inner = inner;
        }

        internal static IBlock Read(ref BlockReader reader) {
            var inner = reader.ReadBlock();
            return new UpdateContextEncapsulation(reader.Raw, inner);
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder AddUpdateContextEncapsulation(this IBlockBuilder inner) =>
            new UpdateContextEncapsulationBuilder(inner);

        sealed class UpdateContextEncapsulationBuilder : IBlockBuilder {
            IBlockBuilder Inner { get; init; }

            internal UpdateContextEncapsulationBuilder(IBlockBuilder inner) {
                Inner = inner;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.UpdateContext);
                writer.Write(Inner);
            }
        }
    }
}
