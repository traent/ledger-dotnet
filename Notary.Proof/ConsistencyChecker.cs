using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using Traent.Ledger.Crypto;
using Traent.Ledger.MerkleTree;

namespace Traent.Notary.Proof {
    public class ConsistencyChecker {
        private readonly byte[] _hashedLedgerId;
        private readonly BitArray _ledgerBits;

        [MemberNotNullWhen(true, nameof(MerkleRoot))]
        public bool LedgerAlreadyIncluded => MerkleRoot is not null;

        public byte[]? ExpectedDigest { get; private set; }
        public byte[]? MerkleRoot { get; private set; }

        public ConsistencyChecker(byte[] ledgerId) {
            _hashedLedgerId = SHA512.HashData(ledgerId);
            _ledgerBits = new BitArray(_hashedLedgerId);
        }

        public void CheckConsistencyStep(byte[][] path, byte[]? merkleConsistencyProof) {
            var proofChecker = new ProofChecker<byte[]>(Algorithms.Sha512MerkleCombiner, Enumerable.SequenceEqual);
            var nodes = path.Select(b => new SearchTreeNode(b)).ToArray();

            // check path length
            if (nodes.Length == 0) {
                throw new System.InvalidOperationException("unexpected proof: empty path");
            }

            ExpectedDigest = ExpectedDigest is null
                ? nodes[0].Hash
                : SHA512.HashData(ExpectedDigest.Concat(nodes[0].Hash).ToArray());

            // internal nodes
            for (byte depth = 0; depth < nodes.Length - 1; depth++) {
                var node = nodes[depth];

                // check kind
                if (!node.IsInternal) {
                    throw new System.InvalidOperationException("unexpected proof Nodes: wrong kind");
                }

                // check link
                byte key = Utils.GetLedgerKeyAtDepth(_ledgerBits, depth);
                if (!node.Data[key].SequenceEqual(nodes[depth + 1].Hash)) {
                    throw new System.InvalidOperationException("unexpected proof Nodes: broken link");
                }
            }

            // final node
            var finalNode = nodes[nodes.Length - 1];
            switch (finalNode.Kind) {
                case SearchTreeNode.NodeKind.Leaf:
                    // check ledger inclusion
                    bool isLedgerIncluded = finalNode.IsLedgerIncluded(_hashedLedgerId);
                    if (!isLedgerIncluded) {
                        // not included in the leaf
                        if (LedgerAlreadyIncluded) {
                            throw new System.InvalidOperationException("unexpected proof: ledger included and then removed");
                        }
                        if (merkleConsistencyProof is not null) {
                            throw new System.InvalidOperationException("unexpected proof: ledger not included in the iteration but Merkle consistency proof found");
                        }
                    } else {
                        // included in the leaf
                        var includedMerkleRoot = finalNode.GetMerkleRootFor(_hashedLedgerId);
                        if (!LedgerAlreadyIncluded) {
                            MerkleRoot = includedMerkleRoot;
                            if (merkleConsistencyProof is not null) {
                                throw new System.InvalidOperationException("unexpected proof: first inclusion in leaf but Merkle consistency proof found");
                            }
                        } else {
                            if (!MerkleRoot.SequenceEqual(includedMerkleRoot)) {
                                if (merkleConsistencyProof is null) {
                                    throw new System.InvalidOperationException("unexpected proof: Merkle consistency proof not found");
                                }
                                var consistencyProof = Utils.DeserializeMerkleProof(merkleConsistencyProof);
                                if (!proofChecker.IsConsistencyProofValid(MerkleRoot, includedMerkleRoot, consistencyProof)) {
                                    throw new System.InvalidOperationException("unexpected invalid consistencyProof");
                                }
                                MerkleRoot = includedMerkleRoot;
                            } else {
                                if (merkleConsistencyProof is not null) {
                                    throw new System.InvalidOperationException("unexpected proof: Merkle root unchanged, but Merkle consistency proof found");
                                }
                            }
                        }
                    }
                    break;
                case SearchTreeNode.NodeKind.Internal:
                    byte key = Utils.GetLedgerKeyAtDepth(_ledgerBits, (byte)(nodes.Length - 1));
                    if (!finalNode.Data[key].SequenceEqual(Utils.NoLink)) {
                        throw new System.InvalidOperationException("unexpected proof: full path to ledgerid not included in the proof");
                    }
                    if (LedgerAlreadyIncluded) {
                        throw new System.InvalidOperationException("unexpected proof: ledger included and then removed");
                    }
                    if (merkleConsistencyProof is not null) {
                        throw new System.InvalidOperationException("unexpected proof: ledger not included in the iteration but Merkle consistency proof found");
                    }
                    break;
                default:
                    throw new System.InvalidOperationException("unexpected proof Nodes: path not terminating with a leaf");
            }
        }

        [MemberNotNull(nameof(ExpectedDigest))]
        public void FinishConsistencyCheck() {
            // no-op right now, to maintain the interface
            // previously, it was enforcing that the ledger was included in the final path leaf
            if (ExpectedDigest is null) {
                throw new System.InvalidOperationException("unexpected proof: undefined expected digest");
            }
        }
    }
}
