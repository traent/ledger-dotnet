using Xunit;

namespace Traent.Ledger.Evaluator.Test {

    public class ByteArrayComparerTest {
        [Theory]
        [InlineData(null)]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0, 1, 2 })]
        public void IdenticalAreEqual(byte[]? testCase) {
            Assert.True(ByteArrayComparer.Instance.Equals(testCase, testCase));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(new byte[] { }, new byte[] { })]
        [InlineData(new byte[] { 0, 1, 2 }, new byte[] { 0, 1, 2 })]
        public void EqualAreEqual(byte[]? a, byte[]? b) {
            Assert.True(ByteArrayComparer.Instance.Equals(a, b));
        }

        [Theory]
        [InlineData(null, new byte[] { })]
        [InlineData(new byte[] { }, null)]
        [InlineData(null, new byte[] { 0, 1, 2 })]
        [InlineData(new byte[] { 0, 1, 2 }, null)]
        [InlineData(new byte[] { }, new byte[] { 0, 1, 2 })]
        [InlineData(new byte[] { 0, 1, 2 }, new byte[] { })]
        public void DifferentAreNotEqual(byte[]? a, byte[]? b) {
            Assert.False(ByteArrayComparer.Instance.Equals(a, b));
        }
    }
}
