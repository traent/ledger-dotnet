using System;

namespace Traent.Ledger.Crypto {
    public interface IPadding {
        int ComputePaddedLength(int dataLength, int blockSize);

        byte[] CopyAndPad(ReadOnlySpan<byte> source, int blockSize) {
            var destination = new byte[ComputePaddedLength(source.Length, blockSize)];
            source.CopyTo(destination); // side-channel: leaks length as timing
            Pad(destination, source.Length, blockSize);
            return destination;
        }

        void Pad(Span<byte> buffer, int dataLength, int blockSize);

        ReadOnlySpan<byte> Unpad(ReadOnlySpan<byte> buffer, int blockSize);
    }
}
