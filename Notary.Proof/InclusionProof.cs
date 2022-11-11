using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;

namespace Traent.Notary.Proof {
    public class InclusionProof : IInclusionProof {
        public byte[] LedgerId { get; init; }
        public int Iteration { get; init; }

        public byte[][] Path { get; init; }
        public byte[] MerkleRoot { get; init; }

        public InclusionProof(byte[] ledgerId, int iteration, byte[][] path, byte[] merkleRoot) {
            LedgerId = ledgerId;
            Iteration = iteration;
            Path = path;
            MerkleRoot = merkleRoot;
        }

        public void Verify(byte[]? digest = null, byte[]? previousDigest = null) {
            var hashedLedgerId = SHA512.HashData(LedgerId);
            var ledgerBits = new BitArray(hashedLedgerId);
            var nodes = Path.Select(b => new SearchTreeNode(b)).ToArray();

            // check path length
            if (nodes.Length == 0) {
                throw new System.InvalidOperationException("unexpected proof: empty path");
            }

            // internal nodes
            for (byte depth = 0; depth < nodes.Length - 1; depth++) {
                var node = nodes[depth];

                // check kind
                if (!node.IsInternal) {
                    throw new System.InvalidOperationException("unexpected proof Nodes: wrong kind");
                }

                // check link
                byte key = Utils.GetLedgerKeyAtDepth(ledgerBits, depth);
                if (!node.Data[key].SequenceEqual(nodes[depth + 1].Hash)) {
                    throw new System.InvalidOperationException("unexpected proof Nodes: broken link");
                }
            }

            // leaf
            var leaf = nodes[nodes.Length - 1];

            // check kind
            if (!leaf.IsLeaf) {
                throw new System.InvalidOperationException("unexpected proof Nodes: path not terminating with a leaf");
            }

            // check ledger inclusion
            var includedMerkleRoot = leaf.GetMerkleRootFor(hashedLedgerId);
            if (!includedMerkleRoot.SequenceEqual(MerkleRoot)) {
                throw new System.InvalidOperationException("unexpected proof Nodes: unexpected inclusion merkle root");
            }

            // optionally check digest
            if (digest is not null && previousDigest is not null) {
                var expectedDigest = SHA512.HashData(previousDigest.Concat(nodes[0].Hash).ToArray());
                if (!expectedDigest.SequenceEqual(digest)) {
                    throw new System.InvalidOperationException("unexpected proof: digest mismatch");
                }
            }
        }
    }
}
