using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public class WasmAckBlock : WasmBlock<AckBlock> {
        public JsNumber TargetIndex => Source.TargetIndex;
        public byte[] TargetLinkHash => Source.TargetLinkHash.ToArray();
        public WasmAckBlock(AckBlock source) : base(source) { }
    }
}
