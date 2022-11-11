using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Traent.Ledger.Evaluator {
    public class ByteArrayComparer : IEqualityComparer<byte[]> {
        public static readonly ByteArrayComparer Instance = new();

        public static bool AreEqual(byte[]? x, byte[]? y) {
            if (Object.ReferenceEquals(x, y)) {
                return true;
            } else if (x is null || y is null) {
                return false;
            } else {
                return AreEqual(x.AsSpan(), y.AsSpan());
            }
        }

        public static bool AreEqual(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.SequenceEqual(y);

        public bool Equals(byte[]? x, byte[]? y) => AreEqual(x, y);

        public int GetHashCode([DisallowNull] byte[] obj) {
            var r = 0;
            foreach (var b in obj) {
                r = HashHelpers.Combine(r, b);
            }
            return r;
        }
    }
}
