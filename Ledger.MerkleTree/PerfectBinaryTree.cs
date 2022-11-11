using System.Collections.Generic;

namespace Traent.Ledger.MerkleTree {
    // in a perfect binary tree, all interior nodes have exactly two childrens,
    // and all leaves have the same depth
    public class PerfectBinaryTree {
        public record Node {
            // distance in levels between the node level and the leaves level
            public uint Height { get; init; }
            // position inside the node level, starting from 0
            public ulong Index { get; init; }

            public bool IsLeftChild => (Index & 1) == 0;
            public bool IsRightChild => (Index & 1) == 1;

            public static Node From(uint height, ulong index) => new Node {
                Height = height,
                Index = index,
            };

            public static Node FromLeafIndex(ulong leafIndex) => From(0, leafIndex);

            public Node Parent => new Node {
                Height = Height + 1,
                Index = Index >> 1,
            };

            public Node Sibling => new Node {
                Height = Height,
                Index = Index ^ 1,
            };

            public SubTree SubTree => new SubTree(this);

            public IEnumerable<Node> Ancestors(ulong leavesCount) {
                Node node = this;
                while (leavesCount > 1) {
                    node = node.Parent;
                    leavesCount = (leavesCount + 1) / 2;
                    yield return node;
                }
            }
        }

        public record SubTree {
            public Node Root { get; init; }

            public ulong LeavesCount => 1ul << (int)Root.Height;
            public ulong MinLeafIndex => Root.Index * LeavesCount;
            public ulong MaxLeafIndex => MinLeafIndex + LeavesCount - 1;

            public SubTree(Node root) {
                Root = root;
            }

            public bool Contains(Node node) {
                var nodeSubtree = node.SubTree;
                return (nodeSubtree.MinLeafIndex >= MinLeafIndex) && (MaxLeafIndex >= nodeSubtree.MaxLeafIndex);
            }
        }
    }
}
