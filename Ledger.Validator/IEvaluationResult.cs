using Traent.Ledger.Evaluator;
using Traent.Ledger.Parser;
using Traent.Ledger.Receipt;

namespace Traent.Ledger.Validator {
    public interface IEvaluationResult {
        IBlock? Block { get; }
        ILedgerState? LedgerState { get; }
        List<byte[]>? MerkleFrontier { get; }
        byte[]? MerkleRootHash { get; }
        byte[]? WriteReceiptSignatureKey { get; }
        IWriteReceipt? WriteReceipt { get; }
    }
}
