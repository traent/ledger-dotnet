using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Leb128.Test {
    public class U32TestCases {
        public static IEnumerable<(uint, byte[])> GetCanonicalPairs() {
            foreach (var (value, data) in U64TestCases.GetCanonicalPairs()) {
                if (value <= 0xFFFF_FFFFul) {
                    yield return ((uint)value, data);
                }
            }
        }

        public static IEnumerable<object[]> GetCanonicalValues() {
            foreach (var (value, data) in GetCanonicalPairs()) {
                yield return new object[] { value, data };
            }
        }

        public static IEnumerable<object[]> GetTruncatedValues() {
            for (var i = 1; i < U32.MaxCanonicalBytes; i++) {
                yield return new object[] { Enumerable.Repeat((byte)0x80u, i).ToArray() };
                yield return new object[] { Enumerable.Repeat((byte)0xFFu, i).ToArray() };
            }
        }

        public static IEnumerable<object[]> GetNonCanonicalValues() {
            foreach (var (value, data) in GetCanonicalPairs()) {
                for (var padding = 1; padding + data.Length < U32.MaxCanonicalBytes; padding++) {
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
