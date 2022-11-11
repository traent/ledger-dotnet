using Microsoft.JSInterop;
using System;
using Traent.Notary.Proof;

namespace Ledger.Wasm.Container {
    public class WasmNotaryChecker {
        private readonly ConsistencyChecker _inner;

        public WasmNotaryChecker(byte[] ledgerId) {
            _inner = new ConsistencyChecker(ledgerId);
        }

        [JSInvokable]
        public byte[]? GetExpectedDigest() {
            return _inner.ExpectedDigest;
        }

        [JSInvokable]
        public byte[]? GetMerkleRoot() {
            return _inner.MerkleRoot;
        }

        [JSInvokable]
        public string[] CheckConsistencyStep(byte[][] path, byte[]? merkleConsistencyProof) {
            try {
                _inner.CheckConsistencyStep(path, merkleConsistencyProof);
                return Array.Empty<string>();
            } catch (Exception e) {
                return new string[] { e.Message };
            }
        }

        [JSInvokable]
        public string[] FinishConsistencyCheck() {
            try {
                _inner.FinishConsistencyCheck();
                return Array.Empty<string>();
            } catch (Exception e) {
                return new string[] { e.Message };
            }
        }
    }
}
