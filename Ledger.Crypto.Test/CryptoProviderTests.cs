using System;
using Xunit;

namespace Traent.Ledger.Crypto.Test {
    public class CryptoProviderTests {
        [Theory]
        [InlineData("ed25519")]
        public static void CanCreateSignatureAlgorithm(string algorithm) {
            var cryptoProvider = CryptoProvider.ForPublicKey(new byte[] { });
            var algo = cryptoProvider.CreateSignatureAlgorithm(algorithm);

            Assert.NotNull(algo);
        }

        [Theory]
        [InlineData("hmacsha512(ledgerId)")]
        public static void CanCreateOneShotHash(string algorithm) {
            var cryptoProvider = CryptoProvider.ForPublicKey(new byte[] { });
            using var algo = cryptoProvider.CreateOneShotHash(algorithm);

            Assert.NotNull(algo);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("invalid-algorithm-identifier")]
        public static void RejectsInvalidSignatureAlgorithmIdentifier(string? algorithm) {
            var cryptoProvider = CryptoProvider.ForPublicKey(new byte[] { });
            _ = Assert.Throws<ArgumentException>(() => cryptoProvider.CreateSignatureAlgorithm(algorithm));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("invalid-algorithm-identifier")]
        public static void RejectsInvalidOneShotHashIdentifier(string? algorithm) {
            var cryptoProvider = CryptoProvider.ForPublicKey(new byte[] { });
            _ = Assert.Throws<ArgumentException>(() => cryptoProvider.CreateOneShotHash(algorithm));
        }

        [Fact]
        public static void CanComputeLedgerId() {
            // input from https://datatracker.ietf.org/doc/html/rfc8037#appendix-A
            var publicLedgerKey = HexString.Wrap("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");

            // output from echo -n '{"crv":"Ed25519","kty":"OKP","x":"11qYAYKxCrfVS_7TyWQHOg7hcvPapiMlrwIaaPcHURo"}' | sha512sum
            var expected = HexString.Wrap("49f4aa0207e63d8be9b8dcdf1c28905e2e8caf9d461bbf211e8a6782869bb15f" +
                "71bcb47485c51f542a092dfcb38b4f036e776cea499c02ddb70a9355fbca8b4e");

            var ledgerId = CryptoProvider.ComputeLedgerId(publicLedgerKey);
            Assert.Equal(expected, HexString.Wrap(ledgerId));
        }
    }
}
