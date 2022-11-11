using System;
using Traent.Ledger.Parser;

namespace Ledger.Parser.Wasm {
    public static class WasmBlockParser {
        public static object Parse(byte[] data) {
            return Convert(IBlockExtensions.ReadBlock(new System.ReadOnlyMemory<byte>(data)));
        }

        public static object Convert(IBlock block) => block switch {
            AckBlock t => new WasmAckBlock(t),
            AddAuthorsBlock t => new WasmAddAuthorsBlock(t),
            AuthorSignatureEncapsulation t => new WasmAuthorSignatureEncapsulation(t),
            DataBlock t => new WasmDataBlock(t),
            InContextEncapsulation t => new WasmInContextEncapsulation(t),
            PolicyBlock t => new WasmPolicyBlock(t),
            PreviousBlockEncapsulation t => new WasmPreviousBlockEncapsulation(t),
            ReferenceBlock t => new WasmReferenceBlock(t),
            UpdateContextEncapsulation t => new WasmUpdateContextEncapsulation(t),
            _ => throw new NotImplementedException(),
        };
    }
}
