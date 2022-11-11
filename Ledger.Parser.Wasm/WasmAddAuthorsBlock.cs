using System.Linq;
using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public class WasmAddAuthorsBlock : WasmBlock<AddAuthorsBlock> {
        public byte[][] AuthorKeys => Source.AuthorKeys.ToArray().Select(x => x.ToArray()).ToArray();
        public WasmAddAuthorsBlock(AddAuthorsBlock source) : base(source) { }
    }
}
