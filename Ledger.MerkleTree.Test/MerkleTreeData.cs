using System;
using System.Collections.Generic;
using System.Linq;

namespace Traent.Ledger.MerkleTree.Test {
    public class MerkleTreeData<T> {
        private readonly List<List<T>> _storage = new() { new List<T>() };
        private List<T> _frontier = new();
        private readonly Func<T, T, T> _combiner;
        public ulong LeavesCount => (ulong)_storage[0].Count;

        public MerkleTreeData(Func<T, T, T> combiner) {
            _combiner = combiner;
        }

        public void Add(T value) {
            var builder = new MerkleBuilder<T>(_combiner);
            var (nodes, frontier) = builder.AddLeaf(_frontier, LeavesCount, value);
            _frontier = frontier;
            foreach (var node in nodes) {
                if (_storage.Count == node.Node.Height) {
                    _storage.Add(new List<T>());
                }
                if ((ulong)_storage[(int)node.Node.Height].Count != node.Node.Index) {
                    throw new InvalidOperationException();
                }
                _storage[(int)node.Node.Height].Add(node.Value);
            }
        }

        public void AddRange(IEnumerable<T> values) {
            foreach (var value in values) {
                Add(value);
            }
        }

        private T GetDataUnsafe(PerfectBinaryTree.Node node) => _storage[(int)node.Height][(int)node.Index];

        public T GetData(PerfectBinaryTree.Node node, ulong leavesCount) {
            if (leavesCount > LeavesCount) {
                // missing leaves data
                throw new IndexOutOfRangeException();
            }

            var nodeSubTree = node.SubTree;

            if (nodeSubTree.MaxLeafIndex <= (leavesCount - 1)) {
                // the node is fully contained in the tree with leavesCount leaves
                return GetDataUnsafe(node);
            }

            if (nodeSubTree.MinLeafIndex >= leavesCount) {
                // the node is fully outside of the tree with leavesCount leaves
                throw new IndexOutOfRangeException();
            }

            // a portion of the leaves of nodeSubTree are contained in the tree with leavesCount leaves
            var supportNode = PerfectBinaryTree.Node.FromLeafIndex(leavesCount - 1);
            bool firstFound = false;
            T? value = default(T);
            while (nodeSubTree.Contains(supportNode)) {
                while (supportNode.Parent.SubTree.MaxLeafIndex <= (leavesCount - 1)) {
                    supportNode = supportNode.Parent;
                }
                if (!firstFound) {
                    value = GetDataUnsafe(supportNode);
                    firstFound = true;
                } else {
                    value = _combiner(GetDataUnsafe(supportNode), value!);
                }
                supportNode = PerfectBinaryTree.Node.From(supportNode.Height, supportNode.Index - 1);
            }
            return value!;
        }

        public T GetRoot(ulong leavesCount) {
            if (leavesCount <= 0) {
                throw new IndexOutOfRangeException();
            }
            int rootHeight = System.Numerics.BitOperations.Log2((uint)leavesCount - 1) + 1;
            return GetData(PerfectBinaryTree.Node.From((uint)rootHeight, 0), leavesCount);
        }

        public T GetRoot() {
            return new MerkleBuilder<T>(_combiner).MakeRoot(_frontier);
        }

        private IEnumerable<ProofStepConcrete<T>> ConcretizeProof(IEnumerable<ProofStepAbstract> abstractProof) {
            return abstractProof.Select(abstractStep => {
                if (!abstractStep.isSubProof) {
                    return new ProofStepConcrete<T> {
                        AppendToLeft = abstractStep.AppendToLeft,
                        Value = GetDataUnsafe((PerfectBinaryTree.Node)abstractStep.Node!),
                    };
                }
                if (abstractStep.SubProof is null) {
                    throw new ArgumentException();
                }
                if (abstractStep.SubProof.Count < 1) {
                    throw new ArgumentException();
                }
                foreach (var subStep in abstractStep.SubProof) {
                    if (subStep.isSubProof) {
                        throw new ArgumentException();
                    }
                }
                T value = GetDataUnsafe((PerfectBinaryTree.Node)abstractStep.SubProof[0].Node!);
                foreach (var subStep in abstractStep.SubProof.Skip(1)) {
                    if (subStep.AppendToLeft) {
                        value = _combiner(GetDataUnsafe((PerfectBinaryTree.Node)subStep.Node!), value);
                    } else {
                        value = _combiner(value, GetDataUnsafe((PerfectBinaryTree.Node)subStep.Node!));
                    }
                }
                return new ProofStepConcrete<T> {
                    AppendToLeft = abstractStep.AppendToLeft,
                    Value = value,
                };
            });
        }

        public IEnumerable<ProofStepConcrete<T>> InclusionProof(ulong leafIndex, ulong leavesCount) {
            if (leavesCount > LeavesCount) {
                // missing leaves data
                throw new IndexOutOfRangeException();
            }
            return ConcretizeProof(Tree.InclusionProof(leafIndex, leavesCount));
        }

        public IEnumerable<ProofStepConcrete<T>> ConsistencyProof(ulong oldLeavesCount, ulong newLeavesCount) {
            if (newLeavesCount > LeavesCount) {
                // missing leaves data
                throw new IndexOutOfRangeException();
            }
            return ConcretizeProof(Tree.ConsistencyProof(oldLeavesCount, newLeavesCount));
        }
    }
}
