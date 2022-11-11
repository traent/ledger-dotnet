using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Traent.Ledger.MerkleTree {
    public class MerkleBuilder<T> {
        private readonly Func<T, T, T> _combiner;

        public MerkleBuilder(Func<T, T, T> combiner) {
            _combiner = combiner;
        }

        public T MakeRoot(IEnumerable<T> frontier) {
            // the frontier is expressed from the smallest to the biggest
            // subtree, so the values are all accumulated on the left, i.e.:
            return frontier.Aggregate((acc, curr) => _combiner(curr, acc));
        }

        public (List<NodeConcrete<T>> Perfected, List<T> Frontier) AddLeaf(IEnumerable<T> frontier, ulong oldLeavesCount, T newLeaf) {
            if (frontier.Count() != BitOperations.PopCount(oldLeavesCount)) {
                throw new ArgumentException(nameof(frontier));
            }

            var leafIndex = oldLeavesCount;
            var leaf = PerfectBinaryTree.Node.FromLeafIndex(leafIndex);
            var perfected = new List<NodeConcrete<T>> {
                new(leaf, newLeaf),
            };

            var lastNewValue = newLeaf;
            foreach (var (node, leftValue) in Tree.PerfectedAncestors(leafIndex).Zip(frontier)) {
                var nodeValue = _combiner(leftValue, lastNewValue);
                perfected.Add(new(node, nodeValue));

                lastNewValue = nodeValue;
            }

            var newFrontier = frontier
                .Skip(perfected.Count - 1)
                .Prepend(lastNewValue)
                .ToList();

            return (perfected, newFrontier);
        }
    }
}
