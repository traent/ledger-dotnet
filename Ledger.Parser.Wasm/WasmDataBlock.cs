using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public class WasmDataBlock : WasmBlock<DataBlock> {
        public byte[] data => Source.Data.ToArray();
        public WasmDataBlock(DataBlock source) : base(source) { }
    }
}
