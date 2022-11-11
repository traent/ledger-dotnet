using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Traent.Notary.Proof;

namespace Ledger.Wasm.Container {
    public class Program {
        public static async Task Main(string[] args) {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
        }
    }

    public static class LedgerProxyApi {
        private static readonly SealedBox _sealedBox = new();
        private static readonly Iso7816d4Padding _padding = new();

        [JSInvokable]
        public static byte[] ComputeLedgerId(byte[] publicKey) {
            return CryptoProvider.ComputeLedgerId(publicKey);
        }

        [JSInvokable]
        public static byte[] ComputeSharedKeyFromBox(byte[] box, byte[] secretSignatureKey) {
            var secretKey = _sealedBox.SecretKeyFromSignatureKey(secretSignatureKey);
            return _sealedBox.ComputeSharedKeyFromBox(box, secretKey);
        }

        [JSInvokable]
        public static byte[] Decrypt(byte[] box, byte[] sharedKey, byte[] publicSignatureKey, int paddingBlockSize) {
            var publicKey = _sealedBox.PublicKeyFromSignatureKey(publicSignatureKey);
            var result = _sealedBox.OpenFromSharedKey(box, sharedKey, publicKey);
            if (paddingBlockSize != 0) {
                result = _padding.Unpad(result, paddingBlockSize).ToArray();
            }

            return result;
        }

        [JSInvokable]
        public static byte[] Hash(byte[] ledgerId, string algorithm, byte[] message) {
            var provider = CryptoProvider.ForLedger(ledgerId);
            using var algo = provider.CreateOneShotHash(algorithm);
            return algo.ComputeHash(message);
        }

        [JSInvokable]
        public static bool IsValidSignature(byte[] ledgerId, string algorithm, byte[] publicKey, byte[] message, byte[] signature) {
            var provider = CryptoProvider.ForLedger(ledgerId);
            var algo = provider.CreateSignatureAlgorithm(algorithm);
            return algo.IsValidSignature(publicKey, message, signature);
        }

        [JSInvokable]
        public static object Parse(byte[] data) {
            return Ledger.Parser.Wasm.WasmBlockParser.Parse(data);
        }

        [JSInvokable]
        public static DotNetObjectReference<WasmValidationState> CreateValidator(byte[] ledgerId) {
            return DotNetObjectReference.Create(new WasmValidationState(ledgerId));
        }

        [JSInvokable]
        public static DotNetObjectReference<WasmNotaryChecker> CreateNotaryChecker(byte[] ledgerId) {
            return DotNetObjectReference.Create(new WasmNotaryChecker(ledgerId));
        }
    }
}
