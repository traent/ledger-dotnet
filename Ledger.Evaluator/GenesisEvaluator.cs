using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {

    class GenesisEvaluator : BlockVisitor<ILedgerState?> {
        private readonly EvaluationState _state;

        public GenesisEvaluator(EvaluationState state) {
            _state = state;
        }

        private void Report(EvaluationProblem problem) => _state.OnEvaluationProblem(problem);

        private ILedgerState? ReportBlockTypeNotAllowed() {
            Report(EvaluationProblem.BlockTypeNotAllowed);
            return null;
        }

        #region BlockVisitor
        protected override ILedgerState? Visit(PolicyBlock block) {
            try {
                var policy = PolicyParser.Parse(block.Version, block.Policy.Span);

                _state.AddAuthors(policy, policy.AuthorKeys);
                return _state.ComputeNewState(policy, block.Raw.Span, updateContextLinkHash: true);
            } catch {
                Report(EvaluationProblem.CannotParsePolicy);
                return null;
            }
        }

        protected override ILedgerState? Visit(DataBlock block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(ReferenceBlock block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(AddAuthorsBlock block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(AckBlock block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(AuthorSignatureEncapsulation block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(PreviousBlockEncapsulation block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(InContextEncapsulation block) => ReportBlockTypeNotAllowed();
        protected override ILedgerState? Visit(UpdateContextEncapsulation block) => ReportBlockTypeNotAllowed();
        #endregion
    }
}
