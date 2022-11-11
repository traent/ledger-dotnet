using System;
using System.Linq;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {

    class BlockEvaluator : BlockVisitor<ILedgerState> {
        private bool _shouldUpdateContextLinkHash = false;
        private readonly IBlock _root;
        private readonly EvaluationState _state;
        private readonly ILedgerState _ledgerState;

        public BlockEvaluator(IBlock block, EvaluationState state, ILedgerState ledgerState) {
            _root = block;
            _state = state;
            _ledgerState = ledgerState;

            if (_root.Raw.Length > _ledgerState.Policy.MaxBlockSize) {
                _state.OnEvaluationProblem(EvaluationProblem.BlockTooBig);
            }

            if (!_state.IsBlockTypeAllowed(BlockTypeSequence.FromEncapsulatedBlock(block))) {
                _state.OnEvaluationProblem(EvaluationProblem.BlockTypeNotAllowed);
            }
        }

        private ILedgerState ComputeNewState() =>
            _state.ComputeNewState(_ledgerState.Policy, _root.Raw.Span, _shouldUpdateContextLinkHash);

        #region BlockVisitor
        protected override ILedgerState Visit(PolicyBlock block) {
            // We might want to investigate what is the exact semantics of a
            // non-genesis policy block; right now we assume that it is
            // forbidden in any sane (genesis) policy. If somehow a second
            // policy is evaluated, it is rejected as not-yet-implemented.

            throw new NotImplementedException();
        }

        protected override ILedgerState Visit(DataBlock block) {
            return ComputeNewState();
        }

        protected override ILedgerState Visit(ReferenceBlock block) {
            using var hasher = _state._cryptoProvider.CreateOneShotHash(_ledgerState.Policy.HashingAlgorithm);

            foreach (var hash in block.Hashes) {
                if (hasher.HashLengthInBytes != hash.Length) {
                    _state.OnEvaluationProblem(EvaluationProblem.InvalidHashLength);
                }
            }

            return ComputeNewState();
        }

        protected override ILedgerState Visit(AddAuthorsBlock block) {
            _state.AddAuthors(_ledgerState.Policy, block.AuthorKeys.Select(publicKey => publicKey.ToArray()));

            return ComputeNewState();
        }

        protected override ILedgerState Visit(AckBlock block) {
            _state.CheckLinkHash(block.TargetIndex, block.TargetLinkHash.Span);

            return ComputeNewState();
        }

        protected override ILedgerState Visit(AuthorSignatureEncapsulation block) {
            _state.CheckSignature(
                policy: _ledgerState.Policy,
                authorId: block.AuthorId.ToArray(),
                data: block.Inner.Raw.Span,
                signature: block.AuthorSignature.Span
            );

            return Dispatch(block.Inner);
        }

        protected override ILedgerState Visit(PreviousBlockEncapsulation block) {
            if (!ByteArrayComparer.AreEqual(block.PreviousBlockHash.Span, _ledgerState.HeadBlockHash)) {
                _state.OnEvaluationProblem(EvaluationProblem.PreviousBlockHashMismatch);
            }

            return Dispatch(block.Inner);
        }

        protected override ILedgerState Visit(InContextEncapsulation block) {
            if (!ByteArrayComparer.AreEqual(block.ContextLinkHash.Span, _ledgerState.ContextLinkHash)) {
                _state.OnEvaluationProblem(EvaluationProblem.ContextLinkHashMismatch);
            }

            return Dispatch(block.Inner);
        }

        protected override ILedgerState Visit(UpdateContextEncapsulation block) {
            _shouldUpdateContextLinkHash = true;

            return Dispatch(block.Inner);
        }
        #endregion
    }
}
