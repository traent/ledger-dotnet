using System.Collections.Generic;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {
    public sealed class RequirementsState {
        public static RequirementsState Create() => new();

        public IEnumerable<ulong> AckedIndexes => _ackedIndexes;
        public IEnumerable<byte[]> AckedLinkHashes => _ackedLinkHashes;
        public IEnumerable<byte[]> NewAuthors => _newAuthors;
        public IEnumerable<byte[]> Signers => _signers;

        private readonly HashSet<ulong> _ackedIndexes = new();
        private readonly HashSet<byte[]> _ackedLinkHashes = new(ByteArrayComparer.Instance);
        private readonly HashSet<byte[]> _newAuthors = new(ByteArrayComparer.Instance);
        private readonly HashSet<byte[]> _signers = new(ByteArrayComparer.Instance);
        private readonly Visitor _visitor;

        private RequirementsState() {
            _visitor = new(this);
        }

        public void Evaluate(IBlock block) {
            _ = _visitor.Dispatch(block);
        }

        class Visitor : BlockVisitor<RequirementsState> {
            private readonly RequirementsState _state;

            public Visitor(RequirementsState state) {
                _state = state;
            }

            protected override RequirementsState Visit(PolicyBlock block) {
                try {
                    var policy = PolicyParser.Parse(block.Version, block.Policy.Span);

                    foreach (var publicKey in policy.AuthorKeys) {
                        _ = _state._newAuthors.Add(publicKey);
                    }
                } catch {
                }
                return _state;
            }

            protected override RequirementsState Visit(DataBlock block) => _state;
            protected override RequirementsState Visit(ReferenceBlock block) => _state;

            protected override RequirementsState Visit(AddAuthorsBlock block) {
                foreach (var publicKey in block.AuthorKeys) {
                    _ = _state._newAuthors.Add(publicKey.ToArray());
                }
                return _state;
            }

            protected override RequirementsState Visit(AckBlock block) {
                _ = _state._ackedIndexes.Add(block.TargetIndex);
                _ = _state._ackedLinkHashes.Add(block.TargetLinkHash.ToArray());
                return _state;
            }

            protected override RequirementsState Visit(AuthorSignatureEncapsulation block) {
                _ = _state._signers.Add(block.AuthorId.ToArray());
                return Dispatch(block.Inner);
            }

            protected override RequirementsState Visit(PreviousBlockEncapsulation block) => Dispatch(block.Inner);
            protected override RequirementsState Visit(InContextEncapsulation block) => Dispatch(block.Inner);
            protected override RequirementsState Visit(UpdateContextEncapsulation block) => Dispatch(block.Inner);
        }
    }
}
