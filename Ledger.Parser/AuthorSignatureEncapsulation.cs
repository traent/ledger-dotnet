using System;
using System.Buffers;

namespace Traent.Ledger.Parser {
    public interface ISigner {
        int SignatureLengthInBytes { get; }
        IBufferWriter<byte> Writer { get; }
        void CreateSignature(Span<byte> signature);
    }

    public sealed class AuthorSignatureEncapsulation : IEncapsulation {
        #region IBlock
        BlockType IBlock.Type => BlockType.AuthorSignature;
        public ReadOnlyMemory<byte> Raw { get; private init; }
        #endregion IBlock

        public ReadOnlyMemory<byte> AuthorId { get; private init; }
        public IBlock Inner { get; private init; }
        public ReadOnlyMemory<byte> AuthorSignature { get; private init; }

        private AuthorSignatureEncapsulation(
            ReadOnlyMemory<byte> raw,
            ReadOnlyMemory<byte> authorId,
            IBlock inner,
            ReadOnlyMemory<byte> authorSignature
        ) {
            Raw = raw;
            AuthorId = authorId;
            Inner = inner;
            AuthorSignature = authorSignature;
        }

        internal static IBlock Read(ref BlockReader reader) {
            // The data is serialized in this order:
            // 1. author id
            // 2. signature size
            // 3. inner block
            // 4. signature
            //
            // This makes it possible to generate blocks in a streaming fashion,
            // for example a signature over a data block with a stream content.
            // If the signature was before the inner block, the whole stream
            // would have to be read twice (one to generate the signature, one
            // to serialize the inner block).
            var authorId = reader.ReadSizedBuffer();
            var signatureSize = reader.ReadLeb128();
            var authorSignature = reader.ReadFromEnd(signatureSize);
            var inner = reader.ReadBlock();

            return new AuthorSignatureEncapsulation(reader.Raw, authorId, inner, authorSignature);
        }
    }

    public static partial class BlockBuilder {
        public static IBlockBuilder AddAuthorSignatureEncapsulation(this IBlockBuilder inner, ReadOnlyMemory<byte> authorId, Func<IBufferWriter<byte>, ISigner> signerFactory) =>
           new AuthorSignatureBuilder(inner, authorId, signerFactory);

        record AuthorSignatureBuilder(IBlockBuilder Inner, ReadOnlyMemory<byte> AuthorId, Func<IBufferWriter<byte>, ISigner> SignerFactory) : IBlockBuilder {
            public void Write(IBufferWriter<byte> writer) {
                writer.Write(BlockType.AuthorSignature);
                writer.Write(AuthorId);

                var signer = SignerFactory(writer);
                writer.Write((ulong)signer.SignatureLengthInBytes);
                signer.Writer.Write(Inner);

                Span<byte> signature = stackalloc byte[signer.SignatureLengthInBytes];
                signer.CreateSignature(signature);
                writer.Write(signature);
            }
        }
    }
}
