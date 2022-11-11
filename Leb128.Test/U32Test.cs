using System;
using System.Buffers;
using Xunit;

namespace Leb128.Test {
    public class U32Test {
        [Theory]
        [MemberData(nameof(U32TestCases.GetCanonicalValues), MemberType = typeof(U32TestCases))]
        public void CanComputeSize(uint value, byte[] expected) {
            Assert.Equal(expected.Length, U32.GetSize(value));
        }

        [Theory]
        [MemberData(nameof(U32TestCases.GetCanonicalValues), MemberType = typeof(U32TestCases))]
        public void WillNotOverrunSpan(uint value, byte[] expected) {
            var dest = new byte[expected.Length - 1];
            _ = Assert.Throws<ArgumentException>(() => U32.Write(dest, value));
        }

        [Theory]
        [MemberData(nameof(U32TestCases.GetCanonicalValues), MemberType = typeof(U32TestCases))]
        public void CanEncodeToSpan(uint value, byte[] expected) {
            var dest = new byte[U32.GetSize(value)];
            Assert.Equal(expected.Length, U32.Write(dest, value));
            Assert.Equal(expected, dest);
        }

        [Theory]
        [MemberData(nameof(U32TestCases.GetCanonicalValues), MemberType = typeof(U32TestCases))]
        public void CanEncodeToBuffer(uint value, byte[] expected) {
            var writer = new ArrayBufferWriter<byte>();
            writer.WriteLeb128(value);
            Assert.Equal(expected, writer.WrittenMemory.ToArray());
        }

        [Theory]
        [MemberData(nameof(U32TestCases.GetCanonicalValues), MemberType = typeof(U32TestCases))]
        [MemberData(nameof(U32TestCases.GetNonCanonicalValues), MemberType = typeof(U32TestCases))]
        public void CanDecode(uint expected, byte[] data) {
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            Assert.True(reader.TryReadLeb128(out uint actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(U32TestCases.GetTruncatedValues), MemberType = typeof(U32TestCases))]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80 })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x10 })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x10 })]
        public void RejectsIllegalEncoding(byte[] data) {
            var sequence = new ReadOnlySequence<byte>(data);
            var reader = new SequenceReader<byte>(sequence);
            Assert.False(reader.TryReadLeb128(out uint _));
        }
    }
}
