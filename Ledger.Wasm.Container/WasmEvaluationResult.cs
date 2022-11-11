using Ledger.Parser.Wasm;
using Traent.Ledger.Receipt;
using Traent.Ledger.Validator;

namespace Ledger.Wasm.Container {
    public class WasmEvaluationResult {
        public object? Block { get; }
        public byte[]? LinkHash { get; }
        public byte[]? MerkleRootHash { get; }
        public byte[]? WriteReceiptSignatureKey { get; }
        public IWriteReceipt? WriteReceipt { get; }
        public string?[] Problems { get; }
        public WasmEvaluationResult(IEvaluationResult? result, string?[] problems) {
            if (result != null) {
                Block = result.Block == null ? null : WasmBlockParser.Convert(result.Block);
                LinkHash = result.Block == null ? null : result.LedgerState?.HeadLinkHash;
                MerkleRootHash = result.MerkleRootHash;
                WriteReceiptSignatureKey = result.WriteReceiptSignatureKey;
                WriteReceipt = result.WriteReceipt;
            }
            Problems = problems;
        }
    }
}
