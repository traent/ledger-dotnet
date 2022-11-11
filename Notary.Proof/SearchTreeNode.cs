using System;
using System.Linq;
using System.Security.Cryptography;

namespace Traent.Notary.Proof {
    public class SearchTreeNode {
        public enum NodeKind : byte {
            Leaf,
            Internal,
        }

        public NodeKind Kind { get; init; }
        public byte[][] Data { get; init; }
        public byte[] Hash { get; init; }

        public bool IsLeaf => Kind == NodeKind.Leaf;
        public bool IsInternal => Kind == NodeKind.Internal;

        public SearchTreeNode(NodeKind kind, byte[][] data, byte[] hash) {
            Kind = kind;
            Data = data;
            Hash = hash;
        }

        public SearchTreeNode(Span<byte> serialization) {
            int expectedSerializationLength = 1 + 8 * 64;
            if (serialization.Length != expectedSerializationLength) {
                throw new InvalidOperationException($"unexpected: serialization length {serialization.Length} instead of {expectedSerializationLength}");
            }

            Kind = (SearchTreeNode.NodeKind)serialization[0];
            Data = new byte[8][] {
                serialization.Slice(1 + 64 * 0, 64).ToArray(),
                serialization.Slice(1 + 64 * 1, 64).ToArray(),
                serialization.Slice(1 + 64 * 2, 64).ToArray(),
                serialization.Slice(1 + 64 * 3, 64).ToArray(),
                serialization.Slice(1 + 64 * 4, 64).ToArray(),
                serialization.Slice(1 + 64 * 5, 64).ToArray(),
                serialization.Slice(1 + 64 * 6, 64).ToArray(),
                serialization.Slice(1 + 64 * 7, 64).ToArray(),
            };
            Hash = SHA512.HashData(serialization.ToArray());
        }

        private int LedgerInclusionIndex(byte[] hashedLedgerId) {
            return Data[..4].ToList().FindIndex(b => b.SequenceEqual(hashedLedgerId));
        }

        public bool IsLedgerIncluded(byte[] hashedLedgerId) {
            if (!IsLeaf) {
                throw new InvalidOperationException($"IsLedgerIncluded invoked on a non-leaf node");
            }
            return LedgerInclusionIndex(hashedLedgerId) != -1;
        }

        public byte[] GetMerkleRootFor(byte[] hashedLedgerId) {
            if (!IsLeaf) {
                throw new InvalidOperationException($"GetMerkleRootFor invoked on a non-leaf node");
            }
            int index = LedgerInclusionIndex(hashedLedgerId);
            if (index == -1) {
                throw new InvalidOperationException($"hashedLedgerId is not included in the leaf");
            }
            return Data[4 + index];
        }
    }
}
