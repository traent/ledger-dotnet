namespace Traent.Ledger.Receipt {
    public class ValidationProblemDetectedEventArgs {
        public ValidationProblem Problem { get; }

        internal ValidationProblemDetectedEventArgs(ValidationProblem problem) {
            Problem = problem;
        }

    }
}
