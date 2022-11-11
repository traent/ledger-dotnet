using System;
using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class OneShotHmacTests {
        [Fact]
        public static void CheckHashLengthProperty() {
            using var a = Algorithms.CreateOneShotHmacSha512(Array.Empty<byte>());

            Assert.Equal(512 / 8, a.HashLengthInBytes);
        }

        [Theory]
        [HexColumnFileData("hmac-sha512.input")]
        public static void ImplementationMatchesTestVectors(HexString key, HexString data, HexString expected) {
            using var a = Algorithms.CreateOneShotHmacSha512(key);

            Span<byte> destination = stackalloc byte[a.HashLengthInBytes];
            var length = a.ComputeHash(data, destination);
            Assert.Equal(destination.Length, length);
            // one of the reference vectors only specifies the first 128 bits,
            // so we only check that the result matches the defined bits
            Assert.Equal(expected, HexString.Wrap(destination[..expected.Length]));

            var hash = a.ComputeHash(data);
            Assert.Equal(a.HashLengthInBytes, hash.Length);
            // only compare the prefix, same as above
            Assert.Equal(expected, HexString.Wrap(hash[..expected.Length]));
        }

        [Fact]
        public static void RejectsShortDestination() {
            var key = Array.Empty<byte>();
            var source = Array.Empty<byte>();
            using var a = Algorithms.CreateOneShotHmacSha512(key);
            var destination = new byte[a.HashLengthInBytes - 1];
            _ = Assert.Throws<ArgumentException>(() => a.ComputeHash(source, destination));
        }

        [Fact]
        public static void CanUseOverlongDestination() {
            var key = Array.Empty<byte>();
            var source = Array.Empty<byte>();
            using var a = Algorithms.CreateOneShotHmacSha512(key);
            Span<byte> destination = stackalloc byte[a.HashLengthInBytes + 1];
            var length = a.ComputeHash(source, destination);
            Assert.Equal(a.HashLengthInBytes, length);
        }
    }
}
