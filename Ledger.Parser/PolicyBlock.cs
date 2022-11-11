using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public sealed class PolicyBlock : IBlock {
        #region IBlock
        BlockType IBlock.Type => BlockType.Policy;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public ulong Version { get; private init; }
        public ReadOnlyMemory<byte> Policy { get; private init; }

        private PolicyBlock(
            ReadOnlyMemory<byte> raw,
            ulong version,
            ReadOnlyMemory<byte> policy
        ) {
            Raw = raw;
            Version = version;
            Policy = policy;
        }

        internal static IBlock Read(ref BlockReader reader) {
            var version = reader.ReadLeb128();
            var policy = reader.Remaining;
            reader.AdvanceToEnd();
            return new PolicyBlock(reader.Raw, version, policy);
        }
    }

    public static partial class BlockBuilder {

        public static IBlockBuilder MakePolicyBlock(ulong version, ReadOnlyMemory<byte> policy) =>
            new PolicyBlockBuilder(version, policy);

        sealed class PolicyBlockBuilder : IBlockBuilder {
            ulong Version { get; init; }
            ReadOnlyMemory<byte> Policy { get; init; }

            internal PolicyBlockBuilder(ulong version, ReadOnlyMemory<byte> policy) {
                Version = version;
                Policy = policy;
            }

            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.Policy);
                writer.Write(Version);
                // the span is intentionally written with no size header
                writer.Write(Policy.Span);
            }
        }
    }
}
