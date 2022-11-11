using System;
using System.Collections.Generic;
using System.Text.Json;
using Traent.Ledger.Parser;
using Xunit;

namespace Traent.Ledger.Evaluator.Test {
    public class RequirementsTest {
        public record BlockTestCase(
            string Data,
            HashSet<ulong> AckedIndexes,
            HashSet<byte[]> AckedLinkHashes,
            HashSet<byte[]> NewAuthors,
            HashSet<byte[]> Signers
        ) {
            public BlockTestCase(string Data) : this(Data, new(), new(), new(), new()) { }

            public IBlock Block => Data.AsReadOnlyMemory().ReadBlock();
        }

        public static IEnumerable<object[]> GetValidBlockTypePairs() {
            var empty = Array.Empty<byte>();
            var testhash = "testhash".ToBytes();

            yield return new[] { new BlockTestCase("G\u0001{\"LedgerKey\":\"samplebase64\",\"MaxBlockSize\":65536}") };
            yield return new[] { new BlockTestCase("D") };
            yield return new[] { new BlockTestCase("Dexample raw payload of any byte") };
            yield return new[] { new BlockTestCase("R\u0000") };
            yield return new[] { new BlockTestCase("R\u0008testhash") };
            yield return new[] { new BlockTestCase("R\u0005hash1\u0006hash02\u0007hash003") };
            yield return new[] { new BlockTestCase("P\u0008testhashDexample encapsulated 2") };
            yield return new[] { new BlockTestCase("C\u0008testhashDexample encapsulated 3") };
            yield return new[] { new BlockTestCase("UDexample encapsulated 4") };

            yield return new[] {
                new BlockTestCase("G\u0001" + JsonSerializer.Serialize(new Policy(
                    LedgerPublicKey: Convert.FromBase64String("mybase64"),
                    MaxBlockSize: 10 * 1024,
                    HashingAlgorithm: "hmacsha512(ledgerId)",
                    SigningAlgorithm: "ed25519",
                    AllowedBlocks: new byte[][] { },
                    AuthorKeys: new byte[][] { empty, testhash },
                    ApplicationData.Default
                ))) with {
                    NewAuthors = new() { empty, testhash },
                },
            };

            yield return new[] {
                new BlockTestCase("A\u0000") with {
                    NewAuthors = new() { empty },
                },
            };

            yield return new[] {
                new BlockTestCase("A\u0000") with {
                    NewAuthors = new() { empty },
                },
            };

            yield return new[] {
                new BlockTestCase("A\u0008testhash") with {
                    NewAuthors = new() { testhash },
                }
            };

            yield return new[] {
                new BlockTestCase("A\u0005hash1\u0006hash02\u0007hash003") with {
                    NewAuthors = new() { "hash1".ToBytes(), "hash02".ToBytes(), "hash003".ToBytes() },
                }
            };

            yield return new[] {
                new BlockTestCase("K\u0004\u0008testhash") with {
                    AckedIndexes = new() { 4ul },
                    AckedLinkHashes = new() { "testhash".ToBytes() },
                },
            };

            yield return new[] {
                new BlockTestCase("S\u0000\u0009Dexample encapsulated 1signature") with {
                    Signers = new() { empty },
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetValidBlockTypePairs))]
        public void CanCollectBlockRequirements(BlockTestCase testCase) {
            var state = RequirementsState.Create();

            state.Evaluate(testCase.Block);

            Assert.Equal(testCase.AckedIndexes, state.AckedIndexes);
            Assert.Equal(testCase.AckedLinkHashes, state.AckedLinkHashes);
            Assert.Equal(testCase.NewAuthors, state.NewAuthors);
            Assert.Equal(testCase.Signers, state.Signers);
        }

        [Fact]
        public void RejectsNullInput() {
            var state = RequirementsState.Create();

            _ = Assert.Throws<ArgumentException>(() => state.Evaluate(null!));
        }
    }
}
