using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Traent.Ledger.Crypto;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {

    public sealed class EvaluationState {
        public static EvaluationState ForEmptyLedger(ICryptoProvider cryptoProvider) => new(
            cryptoProvider: cryptoProvider,
            ledgerState: null,
            knownLinkHashes: new(),
            knownAuthorKeys: new(ByteArrayComparer.Instance)
        );

        public static EvaluationState ForLedger(
            ICryptoProvider cryptoProvider,
            ILedgerState ledgerState,
            IReadOnlyDictionary<ulong, byte[]> knownLinkHashes,
            IReadOnlyDictionary<byte[], byte[]> knownAuthorKeys
        ) => new(
            cryptoProvider: cryptoProvider,
            ledgerState: ledgerState,
            knownLinkHashes: new(knownLinkHashes),
            knownAuthorKeys: new(knownAuthorKeys, ByteArrayComparer.Instance)
        );

        public event EventHandler<EvaluationProblemDetectedEventArgs>? ProblemDetected;

        public ILedgerState? Evaluate(IBlock block) {
            var oldPolicy = _ledgerState?.Policy;

            _ledgerState = _ledgerState is null
                ? new GenesisEvaluator(this).Dispatch(block)
                : new BlockEvaluator(block, this, _ledgerState).Dispatch(block);

            if (_ledgerState is not null) {
                _knownLinkHashes.Add(_ledgerState.BlockCount - 1, _ledgerState.HeadLinkHash);

                if (_ledgerState.Policy != oldPolicy) {
                    ComputeAllowedBlocks();
                }
            }

            return _ledgerState;
        }

        public ILedgerState? Evaluate(byte[] rawBlock, out IBlock? block) {
            try {
                block = IBlockExtensions.ReadBlock(rawBlock);
            } catch {
                block = null;
                OnEvaluationProblem(EvaluationProblem.MalformedBlock);
            }

            if (block != null) {
                return Evaluate(block);
            } else if (_ledgerState != null) {
                _ledgerState = ComputeNewState(_ledgerState.Policy, rawBlock, updateContextLinkHash: false);
            }

            return _ledgerState;
        }

        internal readonly ICryptoProvider _cryptoProvider;
        private ILedgerState? _ledgerState;
        private readonly HashSet<BlockTypeSequence> _allowedBlockTypes = new();
        private readonly Dictionary<ulong, byte[]> _knownLinkHashes;
        private readonly Dictionary<byte[], byte[]> _knownAuthorKeys;

        private EvaluationState(
            ICryptoProvider cryptoProvider,
            ILedgerState? ledgerState,
            Dictionary<ulong, byte[]> knownLinkHashes,
            Dictionary<byte[], byte[]> knownAuthorKeys
        ) {
            _cryptoProvider = cryptoProvider;
            _ledgerState = ledgerState;
            _knownLinkHashes = knownLinkHashes;
            _knownAuthorKeys = knownAuthorKeys;

            ComputeAllowedBlocks();
        }

        private void ComputeAllowedBlocks() {
            _allowedBlockTypes.Clear();
            if (_ledgerState is not null) {
                foreach (var block in _ledgerState.Policy.ParseAllowedBlocks()) {
                    _ = _allowedBlockTypes.Add(block);
                }
            }
        }

        internal void OnEvaluationProblem(EvaluationProblem problem) {
            ProblemDetected?.Invoke(this, new EvaluationProblemDetectedEventArgs(problem));
        }

        internal ILedgerState ComputeNewState(Policy policy, ReadOnlySpan<byte> rawBlock, bool updateContextLinkHash) {
            using var hasher = _cryptoProvider.CreateOneShotHash(policy.HashingAlgorithm);
            var blockHash = hasher.ComputeHash(rawBlock);

            Span<byte> linkHashData = stackalloc byte[blockHash.Length + (_ledgerState?.HeadLinkHash.Length ?? 0)];
            blockHash.CopyTo(linkHashData);
            _ledgerState?.HeadLinkHash.CopyTo(linkHashData.Slice(blockHash.Length));
            var linkHash = hasher.ComputeHash(linkHashData);

            var contextLinkHash = _ledgerState is null || updateContextLinkHash
                ? linkHash
                : _ledgerState.ContextLinkHash;

            var blockCount = _ledgerState?.BlockCount ?? 0;

            return new LedgerState(
                Policy: policy,
                BlockCount: blockCount + 1,
                HeadBlockHash: blockHash,
                HeadLinkHash: linkHash,
                ContextLinkHash: contextLinkHash
            );
        }

        internal void AddAuthors(Policy policy, IEnumerable<byte[]> authors) {
            var signAlgo = _cryptoProvider.CreateSignatureAlgorithm(policy.SigningAlgorithm);
            using var authorKeyHasher = _cryptoProvider.CreateOneShotHash(policy.HashingAlgorithm);

            foreach (var publicKey in authors) {
                if (publicKey.Length != signAlgo.PublicKeyLengthInBytes) {
                    OnEvaluationProblem(EvaluationProblem.InvalidAuthorKey);
                }

                var authorId = authorKeyHasher.ComputeHash(publicKey);
                if (!_knownAuthorKeys.TryAdd(authorId, publicKey)) {
                    OnEvaluationProblem(EvaluationProblem.AuthorAlreadyPresent);
                }
            }
        }

        internal bool IsBlockTypeAllowed(BlockTypeSequence blockTypes) {
            return _allowedBlockTypes.Contains(blockTypes);
        }

        internal void CheckLinkHash(ulong blockIndex, ReadOnlySpan<byte> linkHash) {
            if (!_knownLinkHashes.TryGetValue(blockIndex, out var blockLinkHash)) {
                OnEvaluationProblem(EvaluationProblem.BlockNotFound);
            } else if (!blockLinkHash.AsSpan().SequenceEqual(linkHash)) {
                OnEvaluationProblem(EvaluationProblem.BlockLinkHashMismatch);
            }
        }

        internal void CheckSignature(Policy policy, byte[] authorId, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature) {
            var signAlgo = _cryptoProvider.CreateSignatureAlgorithm(policy.SigningAlgorithm);
            if (!_knownAuthorKeys.TryGetValue(authorId, out var publicKey)) {
                OnEvaluationProblem(EvaluationProblem.AuthorNotFound);
            } else if (!signAlgo.IsValidSignature(publicKey, data, signature)) {
                OnEvaluationProblem(EvaluationProblem.InvalidSignature);
            }
        }
    }
}
