using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    class WasmInContextEncapsulation : WasmEncapsulation<InContextEncapsulation> {
        public byte[] ContextLinkHash => Source.ContextLinkHash.ToArray();
        public WasmInContextEncapsulation(InContextEncapsulation source) : base(source) { }
    }
}
