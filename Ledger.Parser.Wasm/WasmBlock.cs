using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public class WasmBlock<T> where T : IBlock {
        protected T Source { get; }

        public string Type => Source.Type.ToString();

        public WasmBlock(T source) {
            Source = source;
        }
    }

    public class WasmEncapsulation<T> : WasmBlock<T> where T : IEncapsulation {
        public object Inner => WasmBlockParser.Convert(Source.Inner);

        public WasmEncapsulation(T source) : base(source) { }
    }

}
