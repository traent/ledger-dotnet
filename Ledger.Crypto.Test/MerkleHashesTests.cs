using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class MerkleHashesTests {
        [Theory]
        [HexColumnFileData("sha512-merkle-leaves.input")]
        public static void CanHashLeaves(HexString leafValue, HexString expectedHash) {
            var hash = Algorithms.Sha512MerkleLeaf(leafValue);
            Assert.Equal(expectedHash, HexString.Wrap(hash));
        }

        [Theory]
        [HexColumnFileData("sha512-merkle-nodes.input")]
        public static void CanHashNodes(HexString leftValue, HexString rightValue, HexString expectedHash) {
            var hash = Algorithms.Sha512MerkleCombiner(leftValue.ToBytes(), rightValue.ToBytes());
            Assert.Equal(expectedHash, HexString.Wrap(hash));
        }
    }
}
