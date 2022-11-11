using Traent.Ledger.Crypto;
using Traent.Ledger.Evaluator;
using Traent.Ledger.MerkleTree;
using Traent.Ledger.Parser;
using Traent.Ledger.Receipt;

namespace Traent.Ledger.Validator {
    class EvaluationResult : IEvaluationResult {
        internal static readonly MerkleBuilder<byte[]> MerkleBuilder = new(Algorithms.Sha512MerkleCombiner);

        public IBlock? Block { get; private set; }

        public byte[]? WriteReceiptSignatureKey { get; private set; }
        public IWriteReceipt? WriteReceipt { get; private set; }

        public ILedgerState? LedgerState { get; private set; }

        public List<byte[]>? MerkleFrontier { get; private set; }
        public List<NodeConcrete<byte[]>>? PerfectedMerkleNodes { get; private set; }
        public byte[]? MerkleRootHash { get; private set; }

        internal void SetState(ILedgerState? ledgerState, IBlock? block, List<byte[]>? previousFrontier) {
            LedgerState = ledgerState;
            Block = block;

            if (ledgerState != null && previousFrontier != null) {
                var blockIndex = ledgerState.BlockCount - 1;
                var leafHash = Algorithms.Sha512MerkleLeaf(ledgerState.HeadLinkHash);

                var (perfected, frontier) = MerkleBuilder.AddLeaf(previousFrontier, blockIndex, leafHash);

                MerkleFrontier = frontier;
                PerfectedMerkleNodes = perfected;
                MerkleRootHash = MerkleBuilder.MakeRoot(frontier);
            }
        }

        internal void SetWriteReceipt(byte[]? key, IWriteReceipt? receipt) {
            WriteReceiptSignatureKey = key;
            WriteReceipt = receipt;
        }
    }
}
