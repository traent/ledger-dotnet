using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public class WasmAuthorSignatureEncapsulation : WasmEncapsulation<AuthorSignatureEncapsulation> {
        public byte[] AuthorId => Source.AuthorId.ToArray();
        public byte[] AuthorSignature => Source.AuthorSignature.ToArray();
        public WasmAuthorSignatureEncapsulation(AuthorSignatureEncapsulation source) : base(source) { }
    }
}
