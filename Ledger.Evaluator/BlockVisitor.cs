using System;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {
    abstract class BlockVisitor<T> {
        protected abstract T Visit(PolicyBlock block);
        protected abstract T Visit(DataBlock block);
        protected abstract T Visit(ReferenceBlock block);
        protected abstract T Visit(AddAuthorsBlock block);
        protected abstract T Visit(AckBlock block);
        protected abstract T Visit(AuthorSignatureEncapsulation block);
        protected abstract T Visit(PreviousBlockEncapsulation block);
        protected abstract T Visit(InContextEncapsulation block);
        protected abstract T Visit(UpdateContextEncapsulation block);

        public T Dispatch(IBlock block) => block switch {
            PolicyBlock b => Visit(b),
            DataBlock b => Visit(b),
            ReferenceBlock b => Visit(b),
            AddAuthorsBlock b => Visit(b),
            AckBlock b => Visit(b),
            AuthorSignatureEncapsulation b => Visit(b),
            PreviousBlockEncapsulation b => Visit(b),
            InContextEncapsulation b => Visit(b),
            UpdateContextEncapsulation b => Visit(b),
            _ => throw new ArgumentException(nameof(block)),
        };
    }
}
