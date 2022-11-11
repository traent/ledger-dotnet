using System;
using System.Buffers;
using System.Numerics;

namespace Leb128 {
    public static partial class U64 {
        public const int MaxCanonicalBytes = 10;

        public static int GetSize(ulong value) {
            var highestBit = BitOperations.Log2(value);
            return (highestBit + 7) / 7;
        }

        public static int Write(Span<byte> destination, ulong value) {
            for (var i = 0; i < destination.Length; i++) {
                if (value > 0x7Fu) {
                    destination[i] = (byte)((uint)value | ~0x7Fu);
                    value >>= 7;
                } else {
                    destination[i] = (byte)value;
                    return i + 1;
                }
            }

            throw new ArgumentException("The buffer is too small to write the input value");
        }

        public static void WriteLeb128(this IBufferWriter<byte> writer, ulong value) {
            writer.Advance(Write(writer.GetSpan(MaxCanonicalBytes), value));
        }

        // based on
        // https://github.com/dotnet/runtime/blob/55a5a0c2f6b9525f6ba3cdd744df0723524bf011/src/libraries/System.Private.CoreLib/src/System/IO/BinaryReader.cs
        public static bool TryReadLeb128(this ref SequenceReader<byte> reader, out ulong value) {
            ulong result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 10 bytes,
            // or the tenth byte is about to cause integer overflow.
            // This means that we can read the first 9 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = MaxCanonicalBytes - 1;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7) {
                if (!reader.TryRead(out byteReadJustNow)) {
                    value = 0;
                    return false;
                }

                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu) {
                    value = result;
                    return true; // early exit
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.

            if (!reader.TryRead(out byteReadJustNow) || byteReadJustNow > 0b_1u) {
                value = 0;
                return false;
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            value = result;
            return true;
        }
    }
}
