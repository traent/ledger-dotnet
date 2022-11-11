using System;

namespace Traent.Ledger.Parser {
    public interface IBlock {
        BlockType Type { get; }
        ReadOnlyMemory<byte> Raw { get; }
    }

    public interface IEncapsulation : IBlock {
        IBlock Inner { get; }
    }

    public static class IBlockExtensions {
        public static IBlock ReadBlock(this in ReadOnlyMemory<byte> source) {
            return source.ReadBlock(maxDepth: 8);
        }

        internal static IBlock ReadBlock(this in ReadOnlyMemory<byte> source, int maxDepth) {
            var reader = new BlockReader {
                Raw = source,
                Remaining = source,
                MaxDepth = maxDepth,
            };

            if (reader.MaxDepth <= 0) {
                return Fail();
            }

            // we can just reinterpret the value because C# assumes that the
            // cases of enum types are not exhaustive, i.e. it requires us to
            // handle unexpected values in the switch
            var blockType = (BlockType)reader.ReadLeb128();

            return blockType switch {
                BlockType.Policy => PolicyBlock.Read(ref reader),
                BlockType.Data => DataBlock.Read(ref reader),
                BlockType.Reference => ReferenceBlock.Read(ref reader),
                BlockType.AddAuthors => AddAuthorsBlock.Read(ref reader),
                BlockType.Ack => AckBlock.Read(ref reader),
                BlockType.AuthorSignature => AuthorSignatureEncapsulation.Read(ref reader),
                BlockType.PreviousBlock => PreviousBlockEncapsulation.Read(ref reader),
                BlockType.InContext => InContextEncapsulation.Read(ref reader),
                BlockType.UpdateContext => UpdateContextEncapsulation.Read(ref reader),
                _ => Fail(),
            };
        }

        internal static IBlock ReadBlock(this ref BlockReader source) {
            var innerRaw = source.Remaining;
            source.AdvanceToEnd();
            return ReadBlock(innerRaw, source.MaxDepth - 1);
        }

        internal static IBlock Fail() {
            throw new ArgumentException("Invalid input");
        }
    }
}
