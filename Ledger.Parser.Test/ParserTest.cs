using System;
using System.Collections.Generic;
using Xunit;

namespace Traent.Ledger.Parser.Test {

    static class ByteSequenceExtension {
        public static byte[] ToBytes(this object data) =>
            data switch {
                byte[] bytes => bytes,
                string s => System.Text.Encoding.UTF8.GetBytes(s),
                _ => throw new ArgumentException(null, nameof(data)),
            };

        public static ReadOnlyMemory<byte> AsReadOnlyMemory(this object data) => data.ToBytes();
    }

    public class ParserTest {
        private static IEnumerable<(BlockType, string)> GetValidBlockTypePairs() {
            yield return (BlockType.Policy, "G\u0000{\"LedgerKey\":\"samplebase64\",\"MaxBlockSize\":65536}");
            yield return (BlockType.Policy, "G\u0000");
            yield return (BlockType.Policy, "G\u0000undefined");
            yield return (BlockType.Policy, "G\u0000notjson");
            yield return (BlockType.Policy, "G\u0000{\"LedgerKey\":\"samplebase64\",\"MaxBlockSize\":65536");
            yield return (BlockType.Policy, "G\u0000\u0000binary\u0001data");
            yield return (BlockType.Data, "D");
            yield return (BlockType.Data, "Dexample raw payload of any byte");
            yield return (BlockType.Reference, "R\u0000");
            yield return (BlockType.Reference, "R\u0008testhash");
            yield return (BlockType.Reference, "R\u0005hash1\u0006hash02\u0007hash003");
            yield return (BlockType.AddAuthors, "A\u0000");
            yield return (BlockType.AddAuthors, "A\u0008testhash");
            yield return (BlockType.AddAuthors, "A\u0005hash1\u0006hash02\u0007hash003");
            yield return (BlockType.Ack, "K\u0004\u0008testhash");
            yield return (BlockType.AuthorSignature, "S\u0000\u0009Dexample encapsulated 1signature");
            yield return (BlockType.PreviousBlock, "P\u0008testhashDexample encapsulated 2");
            yield return (BlockType.InContext, "C\u0008testhashDexample encapsulated 3");
            yield return (BlockType.UpdateContext, "UDexample encapsulated 4");
            yield return (BlockType.UpdateContext, "UUUUUUUDmaximum nesting of depth 8");
        }

        public static IEnumerable<object[]> GetValidBlocksWithType() {
            foreach (var (type, block) in GetValidBlockTypePairs()) {
                yield return new object[] { type, block };
            }
        }

        private static IEnumerable<object[]> GetValidBlocks() {
            foreach (var (_, block) in GetValidBlockTypePairs()) {
                yield return new object[] { block };
            }
        }

        [Theory]
        [MemberData(nameof(GetValidBlocksWithType))]
        public void CanParseValidBlocks(BlockType blockType, string blockData) {
            var block = blockData.AsReadOnlyMemory().ReadBlock();
            Assert.Equal(blockType, block.Type);
            Assert.Equal(blockData.ToBytes(), block.Raw.ToArray());
        }

        [Theory]
        [InlineData("-")]
        [InlineData("G")]
        [InlineData("R\u0001")]
        [InlineData("R\u0000\u0001")]
        [InlineData("R\u0000\u0004xxx")]
        [InlineData("A\u0001")]
        [InlineData("A\u0000\u0001")]
        [InlineData("A\u0000\u0004xxx")]
        [InlineData("K")]
        [InlineData("K\u0004")]
        [InlineData("K\u0004\u0001")]
        [InlineData("K\u0004\u0004xxx")]
        [InlineData("K\u0000\u0000xxx")]
        [InlineData("S")]
        [InlineData("S\u0000")]
        [InlineData("S\u0000\u0000")]
        [InlineData("S\u0000\u0004xxx")]
        [InlineData("S\u0000\u0004Dxxx")]
        [InlineData("P\u0000")]
        [InlineData("P\u0001")]
        [InlineData("P\u0001D")]
        [InlineData("P\u0004xxxD")]
        [InlineData("C\u0000")]
        [InlineData("C\u0001")]
        [InlineData("C\u0001D")]
        [InlineData("C\u0004xxxD")]
        [InlineData("U")]
        [InlineData("UC")]
        [InlineData("UUUUUUUUDtoo much nesting")]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x10 })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 })]
        public void RejectsInvalidBlocks(object blockData) {
            _ = Assert.ThrowsAny<Exception>(() => blockData.AsReadOnlyMemory().ReadBlock());
        }
    }
}
