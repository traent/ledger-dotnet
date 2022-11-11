using System;
using System.Buffers;

namespace Traent.Ledger.Parser.Test {
    record SigningKey(IBufferWriter<byte> Writer, byte[] Signature) : ISigner {
        public int SignatureLengthInBytes => Signature.Length;
        public void CreateSignature(Span<byte> signature) => Signature.CopyTo(signature);
    }
}
