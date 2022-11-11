using System;
using System.Collections.Generic;
using System.Linq;

namespace Traent.Ledger.MerkleTree {
    public class ProofChecker<T> {
        private readonly Func<T, T, T> _combiner;
        private readonly Func<T, T, bool> _equalityComparer;

        public ProofChecker(Func<T, T, T> combiner, Func<T, T, bool> equalityComparer) {
            _combiner = combiner;
            _equalityComparer = equalityComparer;
        }

        public bool IsInclusionProofValid(T leaf, T root, IEnumerable<ProofStepConcrete<T>> steps) {
            if (!steps.Any()) {
                return false;
            }
            var value = steps.First().Value;
            if (!_equalityComparer(value, leaf)) {
                return false;
            }
            foreach (var step in steps.Skip(1)) {
                if (step.AppendToLeft) {
                    value = _combiner(step.Value, value);
                } else {
                    value = _combiner(value, step.Value);
                }
            }
            return _equalityComparer(value, root);
        }

        public bool IsConsistencyProofValid(T oldRoot, T newRoot, IEnumerable<ProofStepConcrete<T>> steps) {
            if (!steps.Any()) {
                return false;
            }
            var appendToLeftStepsComposition = steps.First().Value;
            var allStepsComposition = appendToLeftStepsComposition;
            foreach (var step in steps.Skip(1)) {
                if (step.AppendToLeft) {
                    appendToLeftStepsComposition = _combiner(step.Value, appendToLeftStepsComposition);
                    allStepsComposition = _combiner(step.Value, allStepsComposition);
                } else {
                    allStepsComposition = _combiner(allStepsComposition, step.Value);
                }
            }
            return _equalityComparer(appendToLeftStepsComposition, oldRoot) && _equalityComparer(allStepsComposition, newRoot);
        }
    }
}
