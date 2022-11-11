using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    class WasmPreviousBlockEncapsulation : WasmEncapsulation<PreviousBlockEncapsulation> {
        public byte[] PreviousBlockHash => Source.PreviousBlockHash.ToArray();
        public WasmPreviousBlockEncapsulation(PreviousBlockEncapsulation source) : base(source) { }
    }
}
