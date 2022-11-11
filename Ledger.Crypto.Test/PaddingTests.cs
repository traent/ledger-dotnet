using System;
using System.Security.Cryptography;
using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class PaddingTests {
        [Fact]
        public void ComputeLengthRejectsInvalidDataLength() {
            var padding = Algorithms.CreateIso7816d4Padding();
            _ = Assert.Throws<ArgumentException>(() => padding.ComputePaddedLength(-3, 128));
        }

        [Fact]
        public void ComputeLengthRejectsInvalidBlockLength() {
            var padding = Algorithms.CreateIso7816d4Padding();
            _ = Assert.Throws<ArgumentException>(() => padding.ComputePaddedLength(123, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(129)]
        public void ComputeLengthReturnsValidBlockLength(int dataLength) {
            const int blockSize = 128;
            var padding = Algorithms.CreateIso7816d4Padding();
            var paddedLength = padding.ComputePaddedLength(dataLength, blockSize);
            Assert.Equal(0, paddedLength % blockSize);
            Assert.True(paddedLength > dataLength);
            Assert.True(paddedLength <= dataLength + blockSize);
        }

        [Fact]
        public void PadRejectsInvalidDataLength() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[128];
            _ = Assert.Throws<ArgumentException>(() => padding.Pad(data, -3, 128));
        }

        [Fact]
        public void PadRejectsInvalidBlockLength() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[128];
            _ = Assert.Throws<ArgumentException>(() => padding.Pad(data, 123, 0));
        }

        [Fact]
        public void PadRejectsInvalidBuffer() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[123];
            _ = Assert.Throws<ArgumentException>(() => padding.Pad(data, 123, 128));
        }

        [Fact]
        public void CopyAndPadRejectsInvalidBlockLength() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[123];
            _ = Assert.Throws<ArgumentException>(() => padding.CopyAndPad(data, 0));
        }

        [Fact]
        public void CopyAndPadWorks() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[123];
            var padded = padding.CopyAndPad(data, 128);

            Assert.Equal(data, padded[..data.Length]);
            Assert.Equal(0, padded.Length % 128);
        }

        [Fact]
        public void UnpadRejectsInvalidBlockLength() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[123];
            var padded = padding.CopyAndPad(data, 128);

            _ = Assert.Throws<ArgumentException>(() => padding.Unpad(padded, 0));
        }

        [Fact]
        public void UnpadWorks() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[123];
            var padded = padding.CopyAndPad(data, 128);
            var unpadded = padding.Unpad(padded, 128);

            Assert.Equal(data, unpadded.ToArray());
        }

        [Fact]
        public void UnpadDetectsAnomalies() {
            var padding = Algorithms.CreateIso7816d4Padding();
            var data = new byte[123];
            var padded = padding.CopyAndPad(data, 128);
            padded[padded.Length - 1]++;

            _ = Assert.Throws<CryptographicException>(() => padding.Unpad(padded, 128));
        }
    }
}
