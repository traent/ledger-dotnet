using System;
using System.Linq;

namespace Traent.Notary.Proof {
    public class ConsistencyProof : IConsistencyProof {
        public byte[] LedgerId { get; init; }
        public int Iteration { get; init; }

        public byte[][][] PathHistory { get; init; }
        public byte[][] MerkleConsistencyProofs { get; init; }
        public byte[]? MerkleRoot { get; init; }

        public ConsistencyProof(byte[] ledgerId, int iteration, byte[][][] pathHistory, byte[][] merkleConsistencyProofs, byte[]? merkleRoot) {
            LedgerId = ledgerId;
            Iteration = iteration;
            PathHistory = pathHistory;
            MerkleConsistencyProofs = merkleConsistencyProofs;
            MerkleRoot = merkleRoot;
        }

        public void Verify(byte[]? digest = null) {
            var checker = new ConsistencyChecker(LedgerId);
            for (int i = 0; i <= Iteration; i++) {
                checker.CheckConsistencyStep(PathHistory[i], MerkleConsistencyProofs[i]);
            }

            checker.FinishConsistencyCheck();

            // check ledger Merkle root
            if (MerkleRoot is null) {
                // this proof is telling that LedgerId never appeared during notarization
                if (checker.LedgerAlreadyIncluded) {
                    throw new System.InvalidOperationException("unexpected proof: ledger unexpectedly included");
                }
            } else {
                // this proof is telling that LedgerId is notarized and associated to MerkleRoot
                if (!checker.LedgerAlreadyIncluded) {
                    throw new System.InvalidOperationException("unexpected proof: ledger unexpectedly not included");
                }
                if (!checker.MerkleRoot.SequenceEqual(MerkleRoot)) {
                    throw new System.InvalidOperationException("unexpected proof: unexpected consistency merkle root");
                }
            }

            // optionally check digest
            if (digest is not null) {
                if (!checker.ExpectedDigest.SequenceEqual(digest)) {
                    throw new System.InvalidOperationException("unexpected proof: digest mismatch");
                }
            }
        }
    }
}
