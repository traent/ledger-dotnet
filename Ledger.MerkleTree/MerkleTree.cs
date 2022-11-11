using System;
using System.Collections.Generic;
using System.Linq;

namespace Traent.Ledger.MerkleTree {
    // our Merkle tree is a binary tree
    // it is built from an ordered list of leaves by following these rules:
    // - all the leaves are placed, ordered, at the bottom level (Height = 0)
    // - every level of height N is build by adding a parent node for the two leftmost orphans
    //   of height N-1 or less, prioritizing the ones of height N-1
    // - levels are added untile there is a single orphan node, the root
    public class Tree {
        public static IEnumerable<PerfectBinaryTree.Node> PerfectedAncestors(ulong leafIndex) {
            var node = PerfectBinaryTree.Node.FromLeafIndex(leafIndex);
            while (node.IsRightChild) {
                node = node.Parent;
                yield return node;
            }
        }

        public static IEnumerable<ProofStepAbstract> InclusionProof(ulong leafIndex, ulong leavesCount) {
            if (leafIndex >= leavesCount) {
                throw new IndexOutOfRangeException();
            }
            var leaf = PerfectBinaryTree.Node.FromLeafIndex(leafIndex);
            return GetProofStepsFromNodeToRoot(leaf, leavesCount);
        }

        public static IEnumerable<ProofStepAbstract> ConsistencyProof(ulong oldLeavesCount, ulong newLeavesCount) {
            if (newLeavesCount < oldLeavesCount) {
                throw new IndexOutOfRangeException();
            }
            if (oldLeavesCount <= 0) {
                // there isn't an old Merkle tree root to reproduce by composing the AppendToLeft ProofStepAbstract
                throw new IndexOutOfRangeException();
            }
            var highestOldTreePerfectSubtreeRootNode = PerfectBinaryTree.Node.FromLeafIndex(oldLeavesCount - 1);
            while (highestOldTreePerfectSubtreeRootNode.IsRightChild) {
                highestOldTreePerfectSubtreeRootNode = highestOldTreePerfectSubtreeRootNode.Parent;
            }
            return GetProofStepsFromNodeToRoot(highestOldTreePerfectSubtreeRootNode, newLeavesCount);
        }

        private static IEnumerable<ProofStepAbstract> GetProofStepsFromNodeToRoot(PerfectBinaryTree.Node startingNode, ulong leavesCount) {
            return startingNode
                .Ancestors(leavesCount)
                //.SkipLast(1)
                // When describing the algorithm by looking at the whole tree structure, the root sibling is naturally not considered,
                // that's the reason of the last ancestor skip.
                // Instead, when describing the algorithm as a recursion, the procedure is unaware of the root height and simply
                // will converge to the same result when operating on an infinite tree.
                // Please note that at the filtering step "Where", the root sibling will be always filtered out: that's why we decided
                // to remove it from the logic.
                // We decided to maintain the no-op SkipLast(1) as a comment, hoping that it could help to describe the algorithm logic.
                .Prepend(startingNode) // to generate the startingNode sibling
                .Select(node => node.Sibling)
                .Prepend(startingNode) // to include the startingNode as the first step of the path
                .Select(node => node.SubTree)
                .Where(subTree => subTree.MinLeafIndex < leavesCount) // skip nodes that are root of subtrees totally outside of the Merkle tree with leavesCount leaves
                .Select(subTree => {
                    if (subTree.MaxLeafIndex < leavesCount) {
                        // this subtree is fully contained in the Merkle tree with leavesCount leaves
                        return new ProofStepAbstract(subTree.Root.IsLeftChild, subTree.Root);
                    }
                    return new ProofStepAbstract(subTree.Root.IsLeftChild, MakeSubProof(subTree, leavesCount));
                });
        }

        private static List<ProofStepAbstract> MakeSubProof(PerfectBinaryTree.SubTree subTree, ulong leavesCount) {
            var subProof = new List<ProofStepAbstract>();
            // start from the rightmost leaf
            var subProofNode = PerfectBinaryTree.Node.FromLeafIndex(leavesCount - 1);
            while (subTree.Contains(subProofNode)) {
                // find the highest perfect subtree root that:
                // - contains subProofNode
                // - all its leaves are included in the Merkle tree with leavesCount leaves
                while (subProofNode.Parent.SubTree.MaxLeafIndex < leavesCount) {
                    subProofNode = subProofNode.Parent;
                }
                subProof.Add(new ProofStepAbstract(true, subProofNode));
                // move left the index of the node
                subProofNode = PerfectBinaryTree.Node.From(subProofNode.Height, subProofNode.Index - 1);
            }
            return subProof;
        }
    }
}
