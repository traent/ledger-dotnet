using System;
using Microsoft.JSInterop;
using System.Collections.Generic;
using Traent.Ledger.Evaluator;
using Traent.Ledger.Receipt;
using Traent.Ledger.Validator;

namespace Ledger.Wasm.Container {
    public class WasmValidationState {
        private readonly List<string?> _problems = new();
        private readonly ValidationState _state;

        public WasmValidationState(byte[] ledgerId) {
            var cryptoProvider = CryptoProvider.ForLedger(ledgerId);
            _state = ValidationState.ForEmptyLedger(cryptoProvider, ledgerId);

            _state.BlockEvaluationProblemDetected += (_, problem) => {
                _problems.Add($"{nameof(EvaluationProblem)}.{Enum.GetName(problem.Problem)}");
            };

            _state.WriteReceiptProblemDetected += (_, problem) => {
                _problems.Add($"{nameof(ValidationProblem)}.{Enum.GetName(problem.Problem)}");
            };
        }

        [JSInvokable]
        public WasmEvaluationResult Evaluate(byte[] rawBlock, byte[] rawReceipt) {
            var inner = _state.Evaluate(rawBlock, rawReceipt);

            var result = new WasmEvaluationResult(inner, _problems.ToArray());

            _problems.Clear();

            return result;
        }
    }
}
