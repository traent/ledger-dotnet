using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Traent.Ledger.Crypto;
using Traent.Ledger.Parser;
using Xunit;

namespace Traent.Ledger.Evaluator.Test {

    public class TestEvalHelper {
        public List<EvaluationProblem> Problems = new();
        public ILedgerState? LedgerState = null;
        public Dictionary<ulong, byte[]> LinkHashes = new();
        public EvaluationState State;

        private TestEvalHelper(EvaluationState state) {
            State = state;

            State.ProblemDetected += (sender, args) => Problems.Add(args.Problem);
        }

        public TestEvalHelper(byte[] ledgerId) :
            this(EvaluationState.ForEmptyLedger(CryptoProvider.ForLedger(ledgerId))) { }

        public TestEvalHelper(
            byte[] ledgerId,
            ILedgerState ledgerState,
            IReadOnlyDictionary<ulong, byte[]> knownBlocks,
            IReadOnlyDictionary<byte[], byte[]> knownKeys
        ) : this(EvaluationState.ForLedger(CryptoProvider.ForLedger(ledgerId), ledgerState, knownBlocks, knownKeys)) { }

        public void Evaluate(string block) => Evaluate(block.ToBytes());

        public void Evaluate(byte[] block) {
            LedgerState = State.Evaluate(block, out var _);
            if (LedgerState is not null) {
                LinkHashes[LedgerState.BlockCount - 1] = LedgerState.HeadLinkHash;
            }
        }
    }


    public class EvaluationTest {
        private static readonly byte[] PolicyBlock = "G".ToBytes();
        private static readonly byte[] DataBlock = "D".ToBytes();
        private static readonly byte[] RefBlock = "R".ToBytes();
        private static readonly byte[] AddAuthorsBlock = "A".ToBytes();
        private static readonly byte[] AckBlock = "K".ToBytes();
        private static readonly byte[] SignedBlock = "SD".ToBytes();
        private static readonly byte[] PrevBlock = "PD".ToBytes();
        private static readonly byte[] InContextBlock = "CD".ToBytes();
        private static readonly byte[] UpdateContextBlock = "UD".ToBytes();

        private static readonly byte[] _ledgerId;
        private static readonly Policy _policy;
        private static readonly Dictionary<byte[], byte[]> _authors = new();
        private static readonly TestEvalHelper _helper;
        private static byte[]? _policyBlockHash => _helper.LedgerState?.HeadBlockHash;
        private static byte[]? _policyLinkHash => _helper.LedgerState?.HeadLinkHash;
        private static readonly byte[] _aliceKeyHash;
        private static readonly byte[] _alicePublicKey;
        private static readonly byte[] _aliceSecretKey;
        private static readonly byte[] _bobPublicKey;
        private static readonly byte[] _bobSecretKey;

        static EvaluationTest() {
            _ledgerId = Convert.FromBase64String("hash+of+mybase64");

            var cryptoProvider = CryptoProvider.ForLedger(_ledgerId);
            var signing = cryptoProvider.CreateSignatureAlgorithm("ed25519");

            signing.GenerateKeyPair(out _alicePublicKey, out _aliceSecretKey);
            signing.GenerateKeyPair(out _bobPublicKey, out _bobSecretKey);

            _policy = new Policy(
                LedgerPublicKey: Convert.FromBase64String("mybase64"),
                MaxBlockSize: 10 * 1024,
                HashingAlgorithm: "hmacsha512(ledgerId)",
                SigningAlgorithm: "ed25519",
                AllowedBlocks: new byte[][] {
                    PolicyBlock,
                    DataBlock,
                    RefBlock,
                    AddAuthorsBlock,
                    AckBlock,
                    SignedBlock,
                    PrevBlock,
                    InContextBlock,
                    UpdateContextBlock,
                },
                AuthorKeys: new byte[][] { _alicePublicKey },
                ApplicationData.Default
            );

            using var hasher = cryptoProvider.CreateOneShotHash(_policy.HashingAlgorithm);
            _aliceKeyHash = hasher.ComputeHash(_alicePublicKey);
            _authors[_aliceKeyHash] = _alicePublicKey;

            var policyBlock = BlockBuilder
                .MakePolicyBlock(1, JsonSerializer.SerializeToUtf8Bytes(_policy))
                .ToBytes();

            _helper = new TestEvalHelper(_ledgerId);
            _helper.Evaluate(policyBlock);
        }

        private static IEnumerable<(EvaluationProblem Problem, byte[] Block)> GetInvalidBlocksAndProblems() {
            yield return (
                Problem: EvaluationProblem.MalformedBlock,
                Block: new byte[0]
            );

            yield return (
                Problem: EvaluationProblem.BlockTypeNotAllowed,
                Block: BlockBuilder
                    .MakeDataBlock("encapsulations not allowed by policy".ToBytes())
                    .AddPreviousBlockEncapsulation(_policyBlockHash)
                    .AddInContextEncapsulation(_policyLinkHash)
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.BlockTooBig,
                BlockBuilder.MakeDataBlock("very big block".PadRight(1 + 10 * 1024).ToBytes()).ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.InvalidHashLength,
                Block: BlockBuilder
                    .MakeReferenceBlock(new[] { "sha-512 hmac digests are 64 bytes long, this one is 54".AsReadOnlyMemory() })
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.BlockNotFound,
                Block: BlockBuilder
                    .MakeAckBlock(12, "block #12 in not yet in the chain".ToBytes())
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.BlockLinkHashMismatch,
                Block: BlockBuilder
                    .MakeAckBlock(0, "this is not the link hash of block #0".ToBytes())
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.InvalidAuthorKey,
                Block: BlockBuilder
                    .MakeAddAuthorsBlock(new[] { "public keys must be 32 bytes, this one is 44".AsReadOnlyMemory() })
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.AuthorAlreadyPresent,
                Block: BlockBuilder
                    .MakeAddAuthorsBlock(new[] {
                        "repeated keys should be reported".AsReadOnlyMemory(),
                        _alicePublicKey.AsReadOnlyMemory(),
                    })
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.AuthorNotFound,
                Block: BlockBuilder
                    .MakeDataBlock("payload to be signed".ToBytes())
                    .AddAuthorSignatureEncapsulation("not alice's key id".ToBytes(), writer => new Signer(_aliceSecretKey, writer))
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.InvalidSignature,
                Block: BlockBuilder
                    .MakeDataBlock("payload to be signed".ToBytes())
                    .AddAuthorSignatureEncapsulation(_aliceKeyHash, writer => new Signer(_bobSecretKey, writer))
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.PreviousBlockHashMismatch,
                Block: BlockBuilder
                    .MakeDataBlock("payload with previous block".ToBytes())
                    .AddPreviousBlockEncapsulation("this is not the block hash of block #0".ToBytes())
                    .ToBytes()
            );

            yield return (
                Problem: EvaluationProblem.ContextLinkHashMismatch,
                Block: BlockBuilder
                    .MakeDataBlock("payload in context".ToBytes())
                    .AddInContextEncapsulation("this is not the link hash of block #0".ToBytes())
                    .ToBytes()
            );
        }

        public static IEnumerable<object[]> GetInvalidProblemBlockTestcases() =>
            GetInvalidBlocksAndProblems().Select(pair => new object[] { pair.Problem, pair.Block });

        public static IEnumerable<object[]> GetInvalidBlocks() =>
            GetInvalidBlocksAndProblems().Select(pair => new[] { pair.Block });

        public static IEnumerable<object[]> GetValidBlocks() {
            yield return new object[] { "Dexample raw payload of any byte".ToBytes() };
            yield return new object[] { BlockBuilder.MakeDataBlock("another example payload".ToBytes()).ToBytes() };

            yield return new object[] { "R\u0040sha-512 hmac digests are 64 bytes long, so we have to keep going".ToBytes() };
            yield return new object[] {
                BlockBuilder
                    .MakeReferenceBlock(new [] {
                        "sha-512 hmac digests are 64 bytes long, so we have to keep going".AsReadOnlyMemory(),
                        "sha-512 hmac digests are 64 bytes long, so we have to keep going".AsReadOnlyMemory(),
                    })
                    .ToBytes(),
            };

            yield return new object[] { "A\u0020public keys are 32 bytes long...".ToBytes() };
            yield return new object[] {
                BlockBuilder
                    .MakeAddAuthorsBlock(new [] {
                        "multiple keys cause no problems ".AsReadOnlyMemory(),
                        "as long as they are different...".AsReadOnlyMemory()
                    })
                    .ToBytes(),
            };

            yield return new object[] { BlockBuilder.MakeAckBlock(0, _policyLinkHash).ToBytes() };

            yield return new object[] {
                BlockBuilder
                    .MakeDataBlock("payload to be signed".ToBytes())
                    .AddAuthorSignatureEncapsulation(_aliceKeyHash, writer => new Signer(_aliceSecretKey, writer))
                    .ToBytes(),
            };

            yield return new object[] {
                BlockBuilder
                    .MakeDataBlock("payload with previous block".ToBytes())
                    .AddPreviousBlockEncapsulation(_policyBlockHash)
                    .ToBytes(),
            };

            yield return new object[] {
                BlockBuilder
                    .MakeDataBlock("payload in context".ToBytes())
                    .AddInContextEncapsulation(_policyLinkHash)
                    .ToBytes(),
            };

            yield return new object[] {
                BlockBuilder
                    .MakeDataBlock("payload updating context".ToBytes())
                    .AddUpdateContextEncapsulation()
                    .ToBytes(),
            };
        }

        [Fact]
        public void CanEvaluateValidPolicies() {
            var policyBlock = BlockBuilder
                .MakePolicyBlock(1, JsonSerializer.SerializeToUtf8Bytes(_policy))
                .ToBytes();

            var helper = new TestEvalHelper(_ledgerId);
            helper.Evaluate(policyBlock);
            Assert.Empty(helper.Problems);

            Assert.NotNull(helper.LedgerState?.Policy);
            Assert.Equal(1ul, helper.LedgerState?.BlockCount);

            using var hasher = CryptoProvider.ForLedger(_ledgerId).CreateOneShotHash(_policy.HashingAlgorithm);
            var policyBlockHash = hasher.ComputeHash(policyBlock.ToBytes());
            var policyLinkHash = hasher.ComputeHash(policyBlockHash);

            Assert.Equal(policyBlockHash, helper.LedgerState?.HeadBlockHash);
            Assert.Equal(policyLinkHash, helper.LedgerState?.HeadLinkHash);
        }

        [Fact]
        public void RejectsMultiplePolicies() {
            var policy = _policy with { AuthorKeys = new[] { _bobPublicKey } };
            var policyBlock = BlockBuilder
                .MakePolicyBlock(1, JsonSerializer.SerializeToUtf8Bytes(policy))
                .ToBytes();

            var helper = new TestEvalHelper(_ledgerId, _helper.LedgerState!, _helper.LinkHashes, _authors);
            _ = Assert.Throws<NotImplementedException>(() => helper.Evaluate(policyBlock));
        }

        [Theory]
        [MemberData(nameof(GetValidBlocks))]
        public void CanEvaluateValidBlocks(byte[] testCase) {
            var helper = new TestEvalHelper(_ledgerId, _helper.LedgerState!, _helper.LinkHashes, _authors);

            helper.Evaluate(testCase);
            Assert.Empty(helper.Problems);
            Assert.Equal(2ul, helper.LedgerState?.BlockCount);
        }

        [Theory]
        [MemberData(nameof(GetValidBlocks))]
        [MemberData(nameof(GetInvalidBlocks))]
        public void CanEvaluateBlocksWithNoHandlers(byte[] testCase) {
            var state = EvaluationState.ForLedger(
                cryptoProvider: CryptoProvider.ForLedger(_ledgerId),
                ledgerState: _helper.LedgerState!,
                knownLinkHashes: _helper.LinkHashes,
                knownAuthorKeys: _authors
            );
            _ = state.Evaluate(testCase, out var _);
        }

        [Theory]
        [MemberData(nameof(GetInvalidProblemBlockTestcases))]
        public void CanDetectInvalidBlocks(EvaluationProblem problem, byte[] testCase) {
            var helper = new TestEvalHelper(_ledgerId, _helper.LedgerState!, _helper.LinkHashes, _authors);

            helper.Evaluate(testCase);
            Assert.Equal(new[] { problem }, helper.Problems);
            Assert.Equal(2ul, helper.LedgerState?.BlockCount);
        }

        [Theory]
        // Policy using an unsupported version
        [InlineData("G\u0000reserved version")]
        // Null policy
        [InlineData("G\u0001null")]
        // Missing field
        [InlineData("G\u0001{\"LedgerPublicKey\":\"mybase64\",\"SigningAlgorithm\":\"hello\",\"HashingAlgorithm\":\"hello\",\"AllowedBlocks\":[],\"AuthorKeys\":[]}")]
        // Mistyped (sub-)field
        [InlineData("G\u0001{\"LedgerPublicKey\":\"mybase64\",\"MaxBlockSize\":true,\"SigningAlgorithm\":\"hello\",\"HashingAlgorithm\":\"hello\",\"AllowedBlocks\":[],\"AuthorKeys\":[]}")]
        // Illegal block type
        [InlineData("G\u0001{\"LedgerPublicKey\":\"mybase64\",\"MaxBlockSize\":123,\"SigningAlgorithm\":\"hello\",\"HashingAlgorithm\":\"hello\",\"AllowedBlocks\":[\"LQ==\"],\"AuthorKeys\":[]}")]
        // Malformed block type
        [InlineData("G\u0001{\"LedgerPublicKey\":\"mybase64\",\"MaxBlockSize\":123,\"SigningAlgorithm\":\"hello\",\"HashingAlgorithm\":\"hello\",\"AllowedBlocks\":[\"gA==\"],\"AuthorKeys\":[]}")]
        // Unexpected field
        [InlineData("G\u0001{\"UnexpectedField\":true,\"LedgerPublicKey\":\"mybase64\",\"MaxBlockSize\":123,\"SigningAlgorithm\":\"hello\",\"HashingAlgorithm\":\"hello\",\"AllowedBlocks\":[],\"AuthorKeys\":[]}")]
        // Repeated field
        // FIXME: this testcase currently fails; it requires a custom policy deserializer
        // [InlineData("G\u0001{\"MaxBlockSize\":7,\"LedgerPublicKey\":\"mybase64\",\"MaxBlockSize\":123,\"AllowedBlocks\":[],\"AuthorKeys\":[]}")]
        public void RejectsUnparsablePolicies(string testCase) {
            var expected = new[] { EvaluationProblem.CannotParsePolicy };
            var helper = new TestEvalHelper(_ledgerId);
            helper.Evaluate(testCase);

            Assert.Equal(expected, helper.Problems);
            Assert.Null(helper.LedgerState);
        }

        [Fact]
        public void DetectsInvalidMaxBlockSizeInPolicy() {
            var policy = _policy with { MaxBlockSize = 0 };
            var policyBlock = BlockBuilder
                .MakePolicyBlock(1, JsonSerializer.SerializeToUtf8Bytes(policy))
                .ToBytes();

            var helper = new TestEvalHelper(_ledgerId);
            helper.Evaluate(policyBlock);

            Assert.Equal(new[] { EvaluationProblem.CannotParsePolicy }, helper.Problems);
            Assert.Null(helper.LedgerState);
        }

        [Fact]
        public void DetectsRepeatedAllowedBlockTypeInPolicy() {
            var policy = _policy with { AllowedBlocks = new[] { DataBlock, DataBlock } };
            var policyBlock = BlockBuilder
                .MakePolicyBlock(1, JsonSerializer.SerializeToUtf8Bytes(policy))
                .ToBytes();

            var helper = new TestEvalHelper(_ledgerId);
            helper.Evaluate(policyBlock);

            Assert.Equal(new[] { EvaluationProblem.CannotParsePolicy }, helper.Problems);
            Assert.Null(helper.LedgerState);
        }

        [Theory]
        [MemberData(nameof(GetValidBlocks))]
        public void RejectsNonPolicyGenesis(byte[] testCase) {
            var helper = new TestEvalHelper(_ledgerId);
            helper.Evaluate(testCase);

            Assert.Equal(new[] { EvaluationProblem.BlockTypeNotAllowed }, helper.Problems);
            Assert.Null(helper.LedgerState);
        }
    }
}
