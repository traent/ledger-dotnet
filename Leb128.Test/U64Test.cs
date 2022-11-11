using System;
using System.Buffers;
using Xunit;

namespace Leb128.Test {
    public class U64Test {
        [Theory]
        [MemberData(nameof(U64TestCases.GetCanonicalValues), MemberType = typeof(U64TestCases))]
        public void CanComputeSize(ulong value, byte[] expected) {
            Assert.Equal(expected.Length, U64.GetSize(value));
        }

        [Theory]
        [MemberData(nameof(U64TestCases.GetCanonicalValues), MemberType = typeof(U64TestCases))]
        public void WillNotOverrunSpan(ulong value, byte[] expected) {
            var dest = new byte[expected.Length - 1];
            _ = Assert.Throws<ArgumentException>(() => U64.Write(dest, value));
        }

        [Theory]
        [MemberData(nameof(U64TestCases.GetCanonicalValues), MemberType = typeof(U64TestCases))]
        public void CanEncodeToSpan(ulong value, byte[] expected) {
            var dest = new byte[U64.GetSize(value)];
            Assert.Equal(expected.Length, U64.Write(dest, value));
            Assert.Equal(expected, dest);
        }

        [Theory]
        [MemberData(nameof(U64TestCases.GetCanonicalValues), MemberType = typeof(U64TestCases))]
        public void CanEncodeToBuffer(ulong value, byte[] expected) {
            var writer = new ArrayBufferWriter<byte>();
            writer.WriteLeb128(value);
            Assert.Equal(expected, writer.WrittenMemory.ToArray());
        }

        [Theory]
        [MemberData(nameof(U64TestCases.GetCanonicalValues), MemberType = typeof(U64TestCases))]
        [MemberData(nameof(U64TestCases.GetNonCanonicalValues), MemberType = typeof(U64TestCases))]
        public void CanDecode(ulong expected, byte[] data) {
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            Assert.True(reader.TryReadLeb128(out ulong actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(U64TestCases.GetTruncatedValues), MemberType = typeof(U64TestCases))]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02 })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 })]
        public void RejectsIllegalEncoding(byte[] data) {
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            Assert.False(reader.TryReadLeb128(out ulong _));
        }
    }
}
