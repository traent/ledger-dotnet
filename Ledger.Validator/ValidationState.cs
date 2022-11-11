using Traent.Ledger.Crypto;
using Traent.Ledger.Evaluator;
using Traent.Ledger.Receipt;

namespace Traent.Ledger.Validator {

    public sealed class ValidationState {
        private readonly ISignatureAlgorithm _signatureAlgorithm;
        private readonly WriteReceiptReader _writeReceiptReader = new WriteReceiptReader();
        private readonly EvaluationState _evaluationState;
        private readonly byte[] _ledgerId;

        private DateTimeOffset? _lastWrittenAt;
        private List<byte[]>? _lastMerkleFrontier;

        public event EventHandler<EvaluationProblemDetectedEventArgs>? BlockEvaluationProblemDetected {
            add => _evaluationState.ProblemDetected += value;
            remove => _evaluationState.ProblemDetected -= value;
        }

        public event EventHandler<ValidationProblemDetectedEventArgs>? WriteReceiptProblemDetected {
            add => _writeReceiptReader.ProblemDetected += value;
            remove => _writeReceiptReader.ProblemDetected -= value;
        }

        public static ValidationState ForEmptyLedger(ICryptoProvider cryptoProvider, byte[] ledgerId) {
            var signAlgo = cryptoProvider.CreateSignatureAlgorithm("ed25519");
            var eval = EvaluationState.ForEmptyLedger(cryptoProvider);
            return new(signAlgo, eval, ledgerId, lastMerkleFrontier: new(), lastWrittenAt: null);
        }

        public static ValidationState ForLedger(
            ICryptoProvider cryptoProvider,
            byte[] ledgerId,
            ILedgerState ledgerState,
            IReadOnlyDictionary<ulong, byte[]> knownLinkHashes,
            IReadOnlyDictionary<byte[], byte[]> knownAuthorKeys,
            List<byte[]>? lastMerkleFrontier,
            DateTimeOffset? lastWrittenAt
        ) {
            var signAlgo = cryptoProvider.CreateSignatureAlgorithm("ed25519");
            var eval = EvaluationState.ForLedger(cryptoProvider, ledgerState, knownLinkHashes, knownAuthorKeys);
            return new(signAlgo, eval, ledgerId, lastMerkleFrontier, lastWrittenAt);
        }

        private ValidationState(
            ISignatureAlgorithm signatureAlgorithm,
            EvaluationState state,
            byte[] ledgerId,
            List<byte[]>? lastMerkleFrontier,
            DateTimeOffset? lastWrittenAt
        ) {
            _signatureAlgorithm = signatureAlgorithm;
            _evaluationState = state;
            _ledgerId = ledgerId;
            _lastMerkleFrontier = lastMerkleFrontier;
            _lastWrittenAt = lastWrittenAt;
        }

        public IEvaluationResult? Evaluate(byte[]? rawBlock, byte[]? rawReceipt) {
            EvaluationResult? result = new EvaluationResult();
            WriteReceiptConstraints? constraints = null;
            if (rawBlock != null) {
                var state = _evaluationState.Evaluate(rawBlock, out var block);
                result.SetState(state, block, _lastMerkleFrontier);
                if (state != null) {
                    constraints = new WriteReceiptConstraints(
                        ledger: _ledgerId,
                        hash: state.HeadLinkHash,
                        merkleRoot: result.MerkleRootHash,
                        minWrittenAt: _lastWrittenAt
                    );
                }
            }

            if (rawReceipt != null) {
                _ = _writeReceiptReader.TryRead(_signatureAlgorithm, rawReceipt, out var key, out var receipt, constraints);
                result.SetWriteReceipt(key, receipt);
            }

            _lastMerkleFrontier = result.MerkleFrontier;
            _lastWrittenAt = result.WriteReceipt?.WrittenAt ?? _lastWrittenAt;
            return result;
        }
    }
}
