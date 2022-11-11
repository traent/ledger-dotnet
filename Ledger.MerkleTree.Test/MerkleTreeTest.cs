using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Traent.Ledger.MerkleTree.Test {
    public class MerkleTreeTest {
        static string Combiner(string a, string b) => $"({a} {b})";
        static bool EqualityComparer(string a, string b) => a == b;

        [Fact]
        public void MerkleTreeBuild() {
            int leavesCount = 70;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            string expected = "(((((((0 1) (2 3)) ((4 5) (6 7))) (((8 9) (10 11)) ((12 13) (14 15)))) ((((16 17) (18 19)) ((20 21) (22 23))) (((24 25) (26 27)) ((28 29) (30 31))))) (((((32 33) (34 35)) ((36 37) (38 39))) (((40 41) (42 43)) ((44 45) (46 47)))) ((((48 49) (50 51)) ((52 53) (54 55))) (((56 57) (58 59)) ((60 61) (62 63)))))) (((64 65) (66 67)) (68 69)))";
            Assert.Equal(expected, tree.GetRoot(70));
        }

        [Fact]
        public void TestMakeTree() {
            var tree = new MerkleTreeData<string>(Combiner);
            for (ulong i = 0; i < 128; i++) {
                Assert.Equal(tree.LeavesCount, i);
                tree.Add($"{i}");
                Assert.Equal(tree.LeavesCount, i + 1);
            }
        }

        [Fact]
        public void TestMakeRoot() {
            var tree = new MerkleTreeData<string>(Combiner);
            for (ulong i = 0; i < 128; i++) {
                tree.Add($"{i}");
                Assert.Equal(tree.GetRoot(), tree.GetRoot(i + 1));
            }
        }

        [Fact]
        public void TestAddLeafStartAtIndex0() {
            var builder = new MerkleBuilder<string>(Combiner);
            var (nodes, frontier) = builder.AddLeaf(Enumerable.Empty<string>(), 0, "0");
            Assert.Equal(new[] { "0" }, frontier);
        }

        [Fact]
        public void TestAddLeafDoesNotStartAtIndex1() {
            var builder = new MerkleBuilder<string>(Combiner);
            Assert.Throws<ArgumentException>(() => builder.AddLeaf(Enumerable.Empty<string>(), 1, "0"));
        }

        [Fact]
        public void InclusionTest() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            for (ulong treeSize = 1; treeSize < (ulong)leavesCount; treeSize++) {
                string treeRootValue = tree.GetRoot(treeSize);
                for (ulong i = 0; i < treeSize; i++) {
                    string leafValue = i.ToString();
                    IEnumerable<ProofStepConcrete<string>> inclusionProof = tree.InclusionProof(i, treeSize);
                    Assert.True(proofChecker.IsInclusionProofValid(leafValue, treeRootValue, inclusionProof));
                }
            }
        }

        [Fact]
        public void InclusionTestIncrementalAdding() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            for (ulong treeSize = 1; treeSize < (ulong)leavesCount; treeSize++) {
                tree.Add($"{treeSize - 1}");
                string treeRootValue = tree.GetRoot(treeSize);
                for (ulong i = 0; i < treeSize; i++) {
                    string leafValue = i.ToString();
                    IEnumerable<ProofStepConcrete<string>> inclusionProof = tree.InclusionProof(i, treeSize);
                    Assert.True(proofChecker.IsInclusionProofValid(leafValue, treeRootValue, inclusionProof));
                }
            }
        }

        [Fact]
        public void EmptyInclusionProof() {
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            var inclusionProof = Enumerable.Empty<ProofStepConcrete<string>>();
            Assert.False(proofChecker.IsInclusionProofValid("0", "0", inclusionProof));
        }

        [Fact]
        public void InclusionOfDifferentLeaf() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            string treeRootValue = tree.GetRoot((ulong)leavesCount);
            IEnumerable<ProofStepConcrete<string>> inclusionProof = tree.InclusionProof(10, (ulong)leavesCount);
            Assert.False(proofChecker.IsInclusionProofValid("20", treeRootValue, inclusionProof));
        }

        [Fact]
        public void InclusionOfNotContainedLeaf() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            string smallerTreeRootValue = tree.GetRoot(100);
            _ = Assert.ThrowsAny<IndexOutOfRangeException>(() => tree.InclusionProof(200, 100));
        }

        [Fact]
        public void ConsistencyTest() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            for (ulong treeSize = 1; treeSize < (ulong)leavesCount; treeSize++) {
                string newTreeRootValue = tree.GetRoot(treeSize);
                for (ulong i = 1; i <= treeSize; i++) {
                    string oldTreeRootValue = tree.GetRoot(i);
                    IEnumerable<ProofStepConcrete<string>> consistencyProof = tree.ConsistencyProof(i, treeSize);
                    Assert.True(proofChecker.IsConsistencyProofValid(oldTreeRootValue, newTreeRootValue, consistencyProof));
                }
            }
        }

        [Fact]
        public void ConsistencyTestIncrementalAdding() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            for (ulong treeSize = 1; treeSize < (ulong)leavesCount; treeSize++) {
                tree.Add($"{treeSize - 1}");
                string newTreeRootValue = tree.GetRoot(treeSize);
                for (ulong i = 1; i <= treeSize; i++) {
                    string oldTreeRootValue = tree.GetRoot(i);
                    IEnumerable<ProofStepConcrete<string>> consistencyProof = tree.ConsistencyProof(i, treeSize);
                    Assert.True(proofChecker.IsConsistencyProofValid(oldTreeRootValue, newTreeRootValue, consistencyProof));
                }
            }
        }

        [Fact]
        public void EmptyConsistencyProof() {
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            var inclusionProof = Enumerable.Empty<ProofStepConcrete<string>>();
            Assert.False(proofChecker.IsConsistencyProofValid("0", "0", inclusionProof));
        }

        [Fact]
        public void ConsistencyWrongOldRootValue() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            ulong oldLeavesCount = 100;
            ulong newLeavesCount = 200;
            string oldTreeRootValue = tree.GetRoot(oldLeavesCount);
            string newTreeRootValue = tree.GetRoot(newLeavesCount);
            IEnumerable<ProofStepConcrete<string>> consistencyProof = tree.ConsistencyProof(oldLeavesCount, newLeavesCount);
            int wrongOldLeavesCount = 150;
            string wrongOldTreeRootValue = tree.GetRoot((ulong)wrongOldLeavesCount);
            Assert.False(proofChecker.IsConsistencyProofValid(wrongOldTreeRootValue, newTreeRootValue, consistencyProof));
        }

        [Fact]
        public void ConsistencyWrongNewRootValue() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            ulong oldLeavesCount = 100;
            ulong newLeavesCount = 200;
            string oldTreeRootValue = tree.GetRoot(oldLeavesCount);
            string newTreeRootValue = tree.GetRoot(newLeavesCount);
            IEnumerable<ProofStepConcrete<string>> consistencyProof = tree.ConsistencyProof(oldLeavesCount, newLeavesCount);
            int wrongNewLeavesCount = 150;
            string wrongNewTreeRootValue = tree.GetRoot((ulong)wrongNewLeavesCount);
            Assert.False(proofChecker.IsConsistencyProofValid(oldTreeRootValue, wrongNewTreeRootValue, consistencyProof));
        }

        [Fact]
        public void ConsistencyOfNotIncludedTree() {
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            ulong oldLeavesCount = 200;
            ulong newLeavesCount = 100;
            string oldTreeRootValue = tree.GetRoot(oldLeavesCount);
            string newTreeRootValue = tree.GetRoot(newLeavesCount);
            _ = Assert.ThrowsAny<IndexOutOfRangeException>(() => tree.ConsistencyProof(oldLeavesCount, newLeavesCount));
        }

        [Fact]
        public void ConsistencyFromEmptyTree() {
            // CHECKME: should we we always reject or always accept the
            // consistency from an empty tree
            int leavesCount = 300;
            var tree = new MerkleTreeData<string>(Combiner);
            tree.AddRange(Enumerable.Range(0, leavesCount).Select(i => $"{i}"));
            var proofChecker = new ProofChecker<string>(Combiner, EqualityComparer);
            ulong oldLeavesCount = 0;
            ulong newLeavesCount = 100;
            string newTreeRootValue = tree.GetRoot(newLeavesCount);
            _ = Assert.ThrowsAny<IndexOutOfRangeException>(() => tree.ConsistencyProof(oldLeavesCount, newLeavesCount));
        }
    }
}
