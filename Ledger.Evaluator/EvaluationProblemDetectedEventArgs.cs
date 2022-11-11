using System;

namespace Traent.Ledger.Evaluator {
    public class EvaluationProblemDetectedEventArgs : EventArgs {
        public EvaluationProblem Problem { get; }

        internal EvaluationProblemDetectedEventArgs(EvaluationProblem problem) {
            Problem = problem;
            // TODO: what additional data do we want to propagate?
            // ledgerState? current [root] block? custom (per-problem) info? 
        }
    }
}
