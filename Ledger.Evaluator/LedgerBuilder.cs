using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Traent.Ledger.Crypto;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {
    public record ApplicationData(
        string Version,
        string Serialization,
        string? Encryption
    ) {
        public static readonly ApplicationData Default = new(
            Version: "1.0.0",
            Serialization: "messagepack-offchainfields",
            Encryption: "xchacha20poly1305-sealedbox-iso7816:4(128)"
        );
    }

    public record AppPolicy(
        byte[] LedgerPublicKey,
        int MaxBlockSize,
        string HashingAlgorithm,
        string SigningAlgorithm,
        byte[][] AllowedBlocks,
        byte[][] AuthorKeys,
        ApplicationData ApplicationData
    ) : Policy(
        LedgerPublicKey,
        MaxBlockSize,
        HashingAlgorithm,
        SigningAlgorithm,
        AllowedBlocks,
        AuthorKeys,
        ApplicationData
    );

    class TeeWriter<T> : IBufferWriter<T> {
        private readonly ArrayBufferWriter<T> _buffer = new();
        private readonly IBufferWriter<T> _writer;

        public ReadOnlySpan<T> WrittenSpan => _buffer.WrittenSpan;

        public TeeWriter(IBufferWriter<T> writer) {
            _writer = writer;
        }

        public void Advance(int count) {
            _buffer.Advance(count);
            _writer.Write(WrittenSpan[^count..]);
        }

        public Memory<T> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);
        public Span<T> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);
    }

    class Signer : ISigner {
        private readonly ReadOnlyMemory<byte> _secretKey;
        private readonly TeeWriter<byte> _teeWriter;
        private readonly ISignatureAlgorithm _algo;

        public IBufferWriter<byte> Writer => _teeWriter;
        public int SignatureLengthInBytes => _algo.SignatureLengthInBytes;

        public Signer(ReadOnlyMemory<byte> secretKey, IBufferWriter<byte> writer, ISignatureAlgorithm algo) {
            _teeWriter = new(writer);
            _secretKey = secretKey;
            _algo = algo;
        }

        public void CreateSignature(Span<byte> signature) {
            _algo.Sign(_secretKey.Span, _teeWriter.WrittenSpan, signature);
        }
    }

    public static class LedgerBuilder {
        private static readonly byte[][] AllowedBlocks = new byte[][] {
            BlockTypeSequence.FromBlockTypes(
                BlockType.AuthorSignature,
                BlockType.InContext,
                BlockType.UpdateContext,
                BlockType.AddAuthors
            ).ToRawBytes(),
            BlockTypeSequence.FromBlockTypes(
                BlockType.AuthorSignature,
                BlockType.PreviousBlock,
                BlockType.UpdateContext,
                BlockType.AddAuthors
            ).ToRawBytes(),

            BlockTypeSequence.FromBlockTypes(
                BlockType.AuthorSignature,
                BlockType.InContext,
                BlockType.UpdateContext,
                BlockType.Data
            ).ToRawBytes(),
            BlockTypeSequence.FromBlockTypes(
                BlockType.AuthorSignature,
                BlockType.PreviousBlock,
                BlockType.UpdateContext,
                BlockType.Data
            ).ToRawBytes(),

            BlockTypeSequence.FromBlockTypes(
                BlockType.AuthorSignature,
                BlockType.Ack
            ).ToRawBytes(),

            BlockTypeSequence.FromBlockTypes(
                BlockType.AuthorSignature,
                BlockType.Reference
            ).ToRawBytes(),
        };

        private static readonly byte[][] NoAuthorsAllowedBlocks = new byte[][] {
            BlockTypeSequence.FromBlockTypes(
                BlockType.InContext,
                BlockType.UpdateContext,
                BlockType.Data
            ).ToRawBytes(),
            BlockTypeSequence.FromBlockTypes(
                BlockType.PreviousBlock,
                BlockType.UpdateContext,
                BlockType.Data
            ).ToRawBytes(),

            BlockTypeSequence.FromBlockTypes(
                BlockType.Reference
            ).ToRawBytes(),
        };

        private static ILedgerBasicBlockBuilder BuildGenesisBlock(AppPolicy policy, out byte[] block) {
            block = BlockBuilder
                .MakePolicyBlock(1, JsonSerializer.SerializeToUtf8Bytes(policy))
                .ToBytes();

            var cryptoProvider = CryptoProvider.ForPublicKey(policy.LedgerPublicKey);
            var eval = EvaluationState.ForEmptyLedger(cryptoProvider);
            var state = eval.Evaluate(IBlockExtensions.ReadBlock(block));
            if (state is null) {
                throw new ArgumentException(nameof(policy));
            }

            return BuildFromState(state);
        }

        public static ILedgerBasicBlockBuilder BuildGenesisBlock(byte[] publicKey, byte[][] authors, int maxBlockSize, out byte[] block) {
            if (!authors.Any()) {
                throw new ArgumentException("Author list is empty");
            }

            return BuildGenesisBlock(new(
                LedgerPublicKey: publicKey,
                MaxBlockSize: maxBlockSize,
                HashingAlgorithm: "hmacsha512(ledgerId)",
                SigningAlgorithm: "ed25519",
                AllowedBlocks: AllowedBlocks,
                AuthorKeys: authors,
                ApplicationData: ApplicationData.Default
            ), out block);
        }

        public static ILedgerBasicBlockBuilder BuildGenesisBlock(byte[] publicKey, int maxBlockSize, out byte[] block) {
            return BuildGenesisBlock(new(
                LedgerPublicKey: publicKey,
                MaxBlockSize: maxBlockSize,
                HashingAlgorithm: "hmacsha512(ledgerId)",
                SigningAlgorithm: "ed25519",
                AllowedBlocks: NoAuthorsAllowedBlocks,
                AuthorKeys: Array.Empty<byte[]>(),
                ApplicationData: ApplicationData.Default
            ), out block);
        }

        public static ILedgerBasicBlockBuilder BuildFromState(ILedgerState state) {
            return new LedgerBasicBlockBuilder(state);
        }
    }

    record LedgerBasicBlockBuilder(ILedgerState State) : ILedgerBasicBlockBuilder {
        public ILedgerWrapperBuilder MakeAckBlock() {
            return MakeAckBlock(State.BlockCount - 1, State.HeadLinkHash);
        }

        public ILedgerWrapperBuilder MakeAckBlock(ulong targetIndex, byte[] targetHash) {
            return Wrap(BlockBuilder.MakeAckBlock(targetIndex, targetHash));
        }

        public ILedgerWrapperBuilder MakeAddAuthorsBlock(IReadOnlyList<ReadOnlyMemory<byte>> publicKeys) {
            return Wrap(BlockBuilder.MakeAddAuthorsBlock(publicKeys));
        }

        public ILedgerWrapperBuilder MakeDataBlock(ReadOnlyMemory<byte> data) {
            return Wrap(BlockBuilder.MakeDataBlock(data));
        }

        public ILedgerWrapperBuilder MakeReferenceBlock(IReadOnlyList<ReadOnlyMemory<byte>> hashes) {
            return Wrap(BlockBuilder.MakeReferenceBlock(hashes));
        }

        private LedgerWrapperBuilder Wrap(IBlockBuilder block) => new(State, block);
    }

    record LedgerWrapperBuilder(ILedgerState State, IBlockBuilder Block) : ILedgerWrapperBuilder {

        public ILedgerWrapperBuilder AddAuthorSignatureEncapsulation(ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> secretKey) {
            var policy = State.Policy;
            var cryptoProvider = CryptoProvider.ForPublicKey(policy.LedgerPublicKey);
            using var hash = cryptoProvider.CreateOneShotHash(policy.HashingAlgorithm);
            var authorId = hash.ComputeHash(publicKey.Span);

            return this with {
                Block = Block.AddAuthorSignatureEncapsulation(authorId, buffer =>
                    new Signer(secretKey, buffer, cryptoProvider.CreateSignatureAlgorithm(policy.SigningAlgorithm))),
            };
        }

        public ILedgerWrapperBuilder AddInContextEncapsulation() {
            return this with {
                Block = Block.AddInContextEncapsulation(State.ContextLinkHash),
            };
        }

        public ILedgerWrapperBuilder AddPreviousBlockEncapsulation() {
            return this with {
                Block = Block.AddPreviousBlockEncapsulation(State.HeadBlockHash),
            };
        }

        public ILedgerWrapperBuilder AddUpdateContextEncapsulation() {
            return this with {
                Block = Block.AddUpdateContextEncapsulation(),
            };
        }

        public ILedgerBasicBlockBuilder Build(out byte[] block) {
            block = Block.ToBytes();

            var cryptoProvider = CryptoProvider.ForPublicKey(State.Policy.LedgerPublicKey);
            var eval = EvaluationState.ForLedger(cryptoProvider, State, new Dictionary<ulong, byte[]>(), new Dictionary<byte[], byte[]>());
            var state = eval.Evaluate(IBlockExtensions.ReadBlock(block));

            Debug.Assert(state is not null);
            return new LedgerBasicBlockBuilder(state);
        }
    }
}
