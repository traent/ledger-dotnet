using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    class WasmPolicyBlock : WasmBlock<PolicyBlock> {
        public JsNumber Version => Source.Version;

        public byte[] Policy => Source.Policy.ToArray();

        public WasmPolicyBlock(PolicyBlock source) : base(source) { }
    }
}
