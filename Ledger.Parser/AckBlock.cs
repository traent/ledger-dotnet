using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public sealed class AckBlock : IBlock {
        #region IBlock
        BlockType IBlock.Type => BlockType.Ack;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public ulong TargetIndex { get; private init; }
        public ReadOnlyMemory<byte> TargetLinkHash { get; private init; }

        private AckBlock(
            ReadOnlyMemory<byte> raw,
            ulong targetIndex,
            ReadOnlyMemory<byte> targetLinkHash
        ) {
            Raw = raw;
            TargetIndex = targetIndex;
            TargetLinkHash = targetLinkHash;
        }

        internal static IBlock Read(ref BlockReader reader) {
            var targetIndex = reader.ReadLeb128();
            var targetLinkHash = reader.ReadSizedBuffer();

            if (reader.Finished()) {
                return new AckBlock(reader.Raw, targetIndex, targetLinkHash);
            }

            return IBlockExtensions.Fail();
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder MakeAckBlock(ulong targetIndex, ReadOnlyMemory<byte> targetLinkHash) =>
            new AckBlockBuilder(targetIndex, targetLinkHash);

        sealed class AckBlockBuilder : IBlockBuilder {
            ulong TargetIndex { get; init; }
            ReadOnlyMemory<byte> TargetLinkHash { get; init; }

            internal AckBlockBuilder(ulong targetIndex, ReadOnlyMemory<byte> targetLinkHash) {
                TargetIndex = targetIndex;
                TargetLinkHash = targetLinkHash;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.Ack);
                writer.Write(TargetIndex);
                writer.Write(TargetLinkHash);
            }
        }
    }
}
