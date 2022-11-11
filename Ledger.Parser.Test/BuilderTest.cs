using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Traent.Ledger.Parser.Test {

    static class WriterExtension {
        public static ReadOnlyMemory<byte> ToMemory(this IBlockBuilder builder) {
            return BlockBuilder.ToBytes(builder);
        }

        public static IReadOnlyList<ReadOnlyMemory<byte>> ToMemoryList(this IReadOnlyList<byte[]> bytes) =>
            bytes.Select<byte[], ReadOnlyMemory<byte>>(x => x).ToArray();

    }

    public class BuilderTest {
        static byte[] RandBytes(int size = 42) {
            var data = new byte[size];
            var rand = new Random();
            rand.NextBytes(data);
            return data;
        }

        [Fact]
        public void CanWritePolicyBlock() {
            var policy = Encoding.UTF8.GetBytes(@"{
                ""LedgerKey"": ""hello there"",
                ""MaxBlockSize"": 65536
            }");

            var buffer = BlockBuilder
                .MakePolicyBlock(0, policy)
                .ToMemory();

            var block = buffer.ReadBlock();
            var policyBlock = Assert.IsType<PolicyBlock>(block);

            Assert.Equal(0ul, policyBlock.Version);
            Assert.Equal(policy, policyBlock.Policy.ToArray());
        }

        [Fact]
        public void CanWriteDataBlock() {
            var data = RandBytes();
            var buffer = BlockBuilder
                .MakeDataBlock(data)
                .ToMemory();

            var block = buffer.ReadBlock();
            var dataBlock = Assert.IsType<DataBlock>(block);
            Assert.Equal(data, dataBlock.Data.ToArray());
        }

        [Fact]
        public void CanWriteReferenceBlock() {
            var refHashes = new[] {
                RandBytes(64),
                RandBytes(32)
            };

            var buffer = BlockBuilder
                .MakeReferenceBlock(refHashes.ToMemoryList())
                .ToMemory();

            var block = buffer.ReadBlock();
            var referenceBlock = Assert.IsType<ReferenceBlock>(block);
            Assert.Equal(refHashes, referenceBlock.Hashes.Select(x => x.ToArray()));
        }

        [Fact]
        public void CanWriteAddAuthorsBlock() {
            var keys = new[] {
                RandBytes(64),
                RandBytes(32)
            };

            var buffer = BlockBuilder
                .MakeAddAuthorsBlock(keys.ToMemoryList())
                .ToMemory();

            var block = buffer.ReadBlock();
            var addAuthorsBlock = Assert.IsType<AddAuthorsBlock>(block);
            Assert.Equal(keys, addAuthorsBlock.AuthorKeys.Select(x => x.ToArray()));
        }

        [Fact]
        public void CanWriteAckBlock() {
            var targetIndex = (ulong)new Random().Next();
            var targetHash = RandBytes();

            var buffer = BlockBuilder
                .MakeAckBlock(targetIndex, targetHash)
                .ToMemory();

            var block = buffer.ReadBlock();
            var ackBlock = Assert.IsType<AckBlock>(block);
            Assert.Equal(targetIndex, ackBlock.TargetIndex);
            Assert.Equal(targetHash, ackBlock.TargetLinkHash.ToArray());
        }

        [Fact]
        public void CanWriteAuthorSignatureEncapsulation() {
            var data = RandBytes();
            var authorId = RandBytes();
            var signature = RandBytes();

            var buffer = BlockBuilder
                .MakeDataBlock(data)
                .AddAuthorSignatureEncapsulation(authorId, writer => new SigningKey(writer, signature))
                .ToMemory();

            var block = buffer.ReadBlock();
            var signatureBlock = Assert.IsType<AuthorSignatureEncapsulation>(block);
            Assert.Equal(authorId, signatureBlock.AuthorId.ToArray());
            Assert.Equal(signature, signatureBlock.AuthorSignature.ToArray());

            var innerBlock = Assert.IsType<DataBlock>(signatureBlock.Inner);
            Assert.Equal(data, innerBlock.Data.ToArray());
        }

        [Fact]
        public void CanWritePreviousBlockEncapsulation() {
            var data = RandBytes();
            var prevBlockHash = RandBytes();

            var buffer = BlockBuilder
                .MakeDataBlock(data)
                .AddPreviousBlockEncapsulation(prevBlockHash)
                .ToMemory();

            var block = buffer.ReadBlock();
            var signatureBlock = Assert.IsType<PreviousBlockEncapsulation>(block);
            Assert.Equal(prevBlockHash, signatureBlock.PreviousBlockHash.ToArray());

            var innerBlock = Assert.IsType<DataBlock>(signatureBlock.Inner);
            Assert.Equal(data, innerBlock.Data.ToArray());
        }

        [Fact]
        public void CanWriteInContextEncapsulation() {
            var data = RandBytes();
            var contextLinkHash = RandBytes();

            var buffer = BlockBuilder
                .MakeDataBlock(data)
                .AddInContextEncapsulation(contextLinkHash)
                .ToMemory();

            var block = buffer.ReadBlock();
            var signatureBlock = Assert.IsType<InContextEncapsulation>(block);
            Assert.Equal(contextLinkHash, signatureBlock.ContextLinkHash.ToArray());

            var innerBlock = Assert.IsType<DataBlock>(signatureBlock.Inner);
            Assert.Equal(data, innerBlock.Data.ToArray());
        }

        [Fact]
        public void CanWriteUpdateContextEncapsulation() {
            var data = RandBytes();

            var buffer = BlockBuilder
                .MakeDataBlock(data)
                .AddUpdateContextEncapsulation()
                .ToMemory();

            var block = buffer.ReadBlock();
            var signatureBlock = Assert.IsType<UpdateContextEncapsulation>(block);

            var innerBlock = Assert.IsType<DataBlock>(signatureBlock.Inner);
            Assert.Equal(data, innerBlock.Data.ToArray());
        }
    }
}
