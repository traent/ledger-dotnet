using System;
using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class IncrementalHmacTests {
        [Fact]
        public static void CheckHashLengthProperty() {
            using var a = Algorithms.CreateIncrementalHmacSha512(Array.Empty<byte>());

            Assert.Equal(512 / 8, a.HashLengthInBytes);
        }

        [Theory]
        [HexColumnFileData("hmac-sha512.input")]
        public static void ImplementationMatchesTestVectors(HexString key, HexString data, HexString expected) {
            ReadOnlySpan<byte> source = data;

            using var a = Algorithms.CreateIncrementalHmacSha512(key);
            while (source.Length >= 7) {
                a.AppendData(source.Slice(0, 7));
                source = source.Slice(7);
            }
            a.AppendData(source);

            Span<byte> destination = stackalloc byte[a.HashLengthInBytes];
            var length = a.GetCurrentHash(destination);
            Assert.Equal(destination.Length, length);
            Assert.Equal(expected, HexString.Wrap(destination[..expected.Length]));
        }

        [Fact]
        public static void RejectsShortDestination() {
            var key = Array.Empty<byte>();
            using var a = Algorithms.CreateIncrementalHmacSha512(key);
            var destination = new byte[a.HashLengthInBytes - 1];
            _ = Assert.Throws<ArgumentException>(() => a.GetCurrentHash(destination));
        }

        [Fact]
        public static void CanUseOverlongDestination() {
            var key = Array.Empty<byte>();
            using var a = Algorithms.CreateIncrementalHmacSha512(key);
            Span<byte> destination = stackalloc byte[a.HashLengthInBytes + 1];
            var length = a.GetCurrentHash(destination);
            Assert.Equal(a.HashLengthInBytes, length);
        }
    }
}
