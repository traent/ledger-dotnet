using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Traent.Ledger.Crypto;

namespace Traent.Ledger.Receipt {
    public class WriteReceiptReader {
        public event EventHandler<ValidationProblemDetectedEventArgs>? ProblemDetected;

        internal void OnEvaluationProblemDetected(ValidationProblem problem) {
            ProblemDetected?.Invoke(this, new ValidationProblemDetectedEventArgs(problem));
        }

        public bool TryRead(
            ISignatureAlgorithm signAlgo,
            ReadOnlySpan<byte> rawReceipt,
            [NotNullWhen(true)] out byte[]? key,
            [NotNullWhen(true)] out IWriteReceipt? receipt,
            IWriteReceiptConstraints? constraints = null
        ) {
            key = null;

            ReadOnlySpan<byte> embeddedKey, signature, payload;
            try {
                embeddedKey = rawReceipt.Slice(0, signAlgo.PublicKeyLengthInBytes);
                key = embeddedKey.ToArray();
                signature = rawReceipt.Slice(signAlgo.PublicKeyLengthInBytes, signAlgo.SignatureLengthInBytes);
                payload = rawReceipt.Slice(signAlgo.PublicKeyLengthInBytes + signAlgo.SignatureLengthInBytes);
            } catch {
                OnEvaluationProblemDetected(ValidationProblem.MalformedReceipt);
                receipt = null;
                return false;
            }

            if (!signAlgo.IsValidSignature(embeddedKey, payload, signature)) {
                OnEvaluationProblemDetected(ValidationProblem.InvalidSignature);
            }

            try {
                receipt = JsonSerializer.Deserialize<WriteReceiptPayload>(payload);
            } catch {
                OnEvaluationProblemDetected(ValidationProblem.MalformedPayload);
                receipt = null;
            }

            if (receipt is not null && constraints is not null) {
                if (!receipt.Ledger.SequenceEqual(constraints.Ledger)) {
                    OnEvaluationProblemDetected(ValidationProblem.LedgerIdMismatch);
                }
                if (!receipt.Hash.SequenceEqual(constraints.Hash)) {
                    OnEvaluationProblemDetected(ValidationProblem.HashMismatch);
                }
                if (constraints.MerkleRoot != null && !receipt.MerkleRoot.SequenceEqual(constraints.MerkleRoot)) {
                    OnEvaluationProblemDetected(ValidationProblem.MerkleRootMismatch);
                }
                if (receipt.WrittenAt < constraints.MinWrittenAt) {
                    OnEvaluationProblemDetected(ValidationProblem.BeforeMinTime);
                }
                if (receipt.WrittenAt > constraints.MaxWrittenAt) {
                    OnEvaluationProblemDetected(ValidationProblem.AfterMaxTime);
                }
            }

            return receipt != null;
        }
    }
}
