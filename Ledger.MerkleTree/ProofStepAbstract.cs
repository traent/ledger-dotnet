using System.Collections.Generic;

namespace Traent.Ledger.MerkleTree {
    public class ProofStepAbstract {
        public bool AppendToLeft { get; init; }
        public PerfectBinaryTree.Node? Node { get; init; }
        public List<ProofStepAbstract>? SubProof { get; init; }

        public bool isSubProof => SubProof is not null;

        public ProofStepAbstract(bool appendToLeft, PerfectBinaryTree.Node node) {
            AppendToLeft = appendToLeft;
            Node = node;
        }

        public ProofStepAbstract(bool appendToLeft, List<ProofStepAbstract> subProof) {
            AppendToLeft = appendToLeft;
            SubProof = subProof;
        }
    }
}
