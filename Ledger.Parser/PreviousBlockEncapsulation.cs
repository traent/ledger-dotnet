using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public sealed class PreviousBlockEncapsulation : IEncapsulation {
        #region IBlock
        BlockType IBlock.Type => BlockType.PreviousBlock;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public ReadOnlyMemory<byte> PreviousBlockHash { get; private init; }
        public IBlock Inner { get; private init; }

        private PreviousBlockEncapsulation(
            ReadOnlyMemory<byte> raw,
            ReadOnlyMemory<byte> previousBlockHash,
            IBlock inner
        ) {
            Raw = raw;
            PreviousBlockHash = previousBlockHash;
            Inner = inner;
        }

        internal static IBlock Read(ref BlockReader reader) {
            var previousBlockHash = reader.ReadSizedBuffer();
            var inner = reader.ReadBlock();
            return new PreviousBlockEncapsulation(reader.Raw, previousBlockHash, inner);
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder AddPreviousBlockEncapsulation(this IBlockBuilder inner, ReadOnlyMemory<byte> previousBlockHash) =>
            new PreviousBlockEncapsulationBuilder(previousBlockHash, inner);

        sealed class PreviousBlockEncapsulationBuilder : IBlockBuilder {
            ReadOnlyMemory<byte> PreviousBlockHash { get; init; }
            IBlockBuilder Inner { get; init; }

            internal PreviousBlockEncapsulationBuilder(ReadOnlyMemory<byte> previousBlockHash, IBlockBuilder inner) {
                PreviousBlockHash = previousBlockHash;
                Inner = inner;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.PreviousBlock);
                writer.Write(PreviousBlockHash);
                writer.Write(Inner);
            }
        }
    }
}
