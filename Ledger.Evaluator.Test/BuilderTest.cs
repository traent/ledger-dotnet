using System;
using System.Collections.Generic;
using Traent.Ledger.Crypto;
using Traent.Ledger.Parser;
using Xunit;

namespace Traent.Ledger.Evaluator.Test {
    public class BuilderTest {
        private static List<EvaluationProblem> Evaluate(byte[] ledgerPublicKey, params byte[][] blocks) {
            var problems = new List<EvaluationProblem>();

            var eval = EvaluationState.ForEmptyLedger(CryptoProvider.ForPublicKey(ledgerPublicKey));
            eval.ProblemDetected += (sender, args) => problems.Add(args.Problem);

            ulong expectedBlockCount = 0;
            foreach (var block in blocks) {
                var ledgerState = eval.Evaluate(IBlockExtensions.ReadBlock(block));
                expectedBlockCount++;
                Assert.NotNull(ledgerState?.Policy);
                Assert.Equal(expectedBlockCount, ledgerState?.BlockCount);
            }

            return problems;
        }

        [Fact]
        public void CannotGeneratePoliciesWithNegativeMaxBlockSize() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            Assert.Throws<ArgumentException>(() => LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, -42, out var _)
            );
        }

        [Fact]
        public void CanGenerateValidPoliciesWithoutAuthors() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            _ = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, 5 * 1024 * 1024, out var policyBlock);

            var problems = Evaluate(ledgerPublicKey, policyBlock);
            Assert.Empty(problems);
        }

        [Fact]
        public void CanGenerateValidPoliciesWithAuthors() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var alicePublicKey, out _);
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            _ = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, new[] { alicePublicKey }, 5 * 1024 * 1024, out var policyBlock);

            var problems = Evaluate(ledgerPublicKey, policyBlock);
            Assert.Empty(problems);
        }

        [Fact]
        public void CannotGeneratePoliciesWithoutAuthorsAndAuthorRequirements() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            Assert.Throws<ArgumentException>(() => LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, Array.Empty<byte[]>(), 5 * 1024 * 1024, out var _)
            );
        }

        [Fact]
        public void CanGenerateValidReference() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            using var hasher = CryptoProvider.ForPublicKey(ledgerPublicKey).CreateOneShotHash("hmacsha512(ledgerId)");
            _ = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, 5 * 1024 * 1024, out var policyBlock)

                .MakeReferenceBlock(new[] { hasher.ComputeHash(new byte[] { }).AsReadOnlyMemory() })
                .Build(out var refBlock);

            var problems = Evaluate(ledgerPublicKey, policyBlock, refBlock);
            Assert.Empty(problems);
        }

        [Fact]
        public void CanGenerateValidData() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            _ = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, 5 * 1024 * 1024, out var policyBlock)

                .MakeDataBlock(new byte[] { 1, 2, 3 })
                .AddUpdateContextEncapsulation()
                .AddInContextEncapsulation()
                .Build(out var dataBlock);

            var problems = Evaluate(ledgerPublicKey, policyBlock, dataBlock);
            Assert.Empty(problems);
        }

        [Fact]
        public void CanGenerateValidSequentialAcks() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var alicePublicKey, out var aliceSecretKey);
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            using var hasher = CryptoProvider.ForPublicKey(ledgerPublicKey).CreateOneShotHash("hmacsha512(ledgerId)");
            var builder = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, new[] { alicePublicKey }, 5 * 1024 * 1024, out var policyBlock)

                .MakeAckBlock()
                .AddAuthorSignatureEncapsulation(alicePublicKey, aliceSecretKey)
                .Build(out var signedAckBlock1)

                .MakeAckBlock()
                .AddAuthorSignatureEncapsulation(alicePublicKey, aliceSecretKey)
                .Build(out var signedAckBlock2);

            var problems = Evaluate(ledgerPublicKey, policyBlock, signedAckBlock1, signedAckBlock2);
            Assert.Empty(problems);

            // the second ack is acking the first ack; it is targeting an absent block if it is applied first
            problems = Evaluate(ledgerPublicKey, policyBlock, signedAckBlock2, signedAckBlock1);
            Assert.Equal(new[] { EvaluationProblem.BlockNotFound }, problems);
        }

        [Fact]
        public void CanGenerateValidConcurrentAcks() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var alicePublicKey, out var aliceSecretKey);
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            using var hasher = CryptoProvider.ForPublicKey(ledgerPublicKey).CreateOneShotHash("hmacsha512(ledgerId)");
            var builder = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, new[] { alicePublicKey }, 5 * 1024 * 1024, out var policyBlock);

            // create two acks "concurrently" (from the same state)
            _ = builder
                .MakeAckBlock()
                .AddAuthorSignatureEncapsulation(alicePublicKey, aliceSecretKey)
                .Build(out var signedAckBlock1);

            _ = builder
                .MakeAckBlock()
                .AddAuthorSignatureEncapsulation(alicePublicKey, aliceSecretKey)
                .Build(out var signedAckBlock2);

            // check that they can be added to the ledger in either order
            var problems = Evaluate(ledgerPublicKey, policyBlock, signedAckBlock1, signedAckBlock2);
            Assert.Empty(problems);

            problems = Evaluate(ledgerPublicKey, policyBlock, signedAckBlock2, signedAckBlock1);
            Assert.Empty(problems);
        }

        [Fact]
        public void CanGenerateValidAddAuthors() {
            Algorithms.CreateEd25519().GenerateKeyPair(out var alicePublicKey, out var aliceSecretKey);
            Algorithms.CreateEd25519().GenerateKeyPair(out var bobPublicKey, out _);
            Algorithms.CreateEd25519().GenerateKeyPair(out var ledgerPublicKey, out _);

            using var hasher = CryptoProvider.ForPublicKey(ledgerPublicKey).CreateOneShotHash("hmacsha512(ledgerId)");
            _ = LedgerBuilder
                .BuildGenesisBlock(ledgerPublicKey, new[] { alicePublicKey }, 5 * 1024 * 1024, out var policyBlock)

                .MakeAddAuthorsBlock(new[] { bobPublicKey.AsReadOnlyMemory() })
                .AddUpdateContextEncapsulation()
                .AddPreviousBlockEncapsulation()
                .AddAuthorSignatureEncapsulation(alicePublicKey, aliceSecretKey)
                .Build(out var signedAddAuthorsBlock);

            var problems = Evaluate(ledgerPublicKey, policyBlock, signedAddAuthorsBlock);
            Assert.Empty(problems);
        }
    }
}
