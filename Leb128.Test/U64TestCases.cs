using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Leb128.Test {
    public class U64TestCases {
        public static IEnumerable<(ulong, byte[])> GetCanonicalPairs() {
            yield return (0x0000_0000_0000_0000ul, new byte[] { 0x00 });
            yield return (0x0000_0000_0000_0001ul, new byte[] { 0x01 });
            yield return (0x0000_0000_0000_007Ful, new byte[] { 0x7F });

            yield return (0x0000_0000_0000_0080ul, new byte[] { 0x80, 0x01 });
            yield return (0x0000_0000_0000_00FFul, new byte[] { 0xFF, 0x01 });
            yield return (0x0000_0000_0000_3FFFul, new byte[] { 0xFF, 0x7F });

            yield return (0x0000_0000_0000_4000ul, new byte[] { 0x80, 0x80, 0x01 });
            yield return (0x0000_0000_0000_7FFFul, new byte[] { 0xFF, 0xFF, 0x01 });
            yield return (0x0000_0000_001F_FFFFul, new byte[] { 0xFF, 0xFF, 0x7F });

            yield return (0x0000_0000_0020_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x01 });
            yield return (0x0000_0000_003F_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0x01 });
            yield return (0x0000_0000_0FFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F });

            yield return (0x0000_0000_1000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 });
            yield return (0x0000_0000_1FFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x01 });
            yield return (0x0000_0000_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F });
            yield return (0x0000_0001_0000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x10 });
            yield return (0x0000_0007_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x7F });

            yield return (0x0000_0008_0000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 });
            yield return (0x0000_000F_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 });
            yield return (0x0000_03FF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F });

            yield return (0x0000_0400_0000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 });
            yield return (0x0000_07FF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 });
            yield return (0x0001_FFFF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F });

            yield return (0x0002_0000_0000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 });
            yield return (0x0003_FFFF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 });
            yield return (0x00FF_FFFF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F });

            yield return (0x0100_0000_0000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 });
            yield return (0x01FF_FFFF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 });
            yield return (0x7FFF_FFFF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F });

            yield return (0x8000_0000_0000_0000ul, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01 });
            yield return (0xFFFF_FFFF_FFFF_FFFFul, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 });
        }

        public static IEnumerable<object[]> GetCanonicalValues() {
            foreach (var (value, data) in GetCanonicalPairs()) {
                yield return new object[] { value, data };
            }
        }

        public static IEnumerable<object[]> GetTruncatedValues() {
            for (var i = 1; i < U64.MaxCanonicalBytes; i++) {
                yield return new object[] { Enumerable.Repeat((byte)0x80u, i).ToArray() };
                yield return new object[] { Enumerable.Repeat((byte)0xFFu, i).ToArray() };
            }
        }

        public static IEnumerable<object[]> GetNonCanonicalValues() {
            foreach (var (value, data) in GetCanonicalPairs()) {
                for (var padding = 1; padding + data.Length < U64.MaxCanonicalBytes; padding++) {
                    var padded = new byte[padding + data.Length];
                    Array.Copy(data, padded, data.Length);
                    for (var i = 0; i < padding; i++) {
                        padded[data.Length - 1 + i] |= 0x80;
                    }
                    yield return new object[] { value, padded };
                }
            }
        }
    }
}
