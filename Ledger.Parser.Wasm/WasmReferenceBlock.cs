using System.Linq;
using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public class WasmReferenceBlock : WasmBlock<ReferenceBlock> {
        public byte[][] Hashes => Source.Hashes.Select(h => h.ToArray()).ToArray();
        public WasmReferenceBlock(ReferenceBlock source) : base(source) { }
    }
}
