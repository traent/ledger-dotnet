using System;
using System.Collections.Generic;

namespace Traent.Ledger.Evaluator {
    public interface ILedgerBasicBlockBuilder {
        ILedgerWrapperBuilder MakeAckBlock();
        ILedgerWrapperBuilder MakeAckBlock(ulong targetIndex, byte[] targetHash);
        ILedgerWrapperBuilder MakeAddAuthorsBlock(IReadOnlyList<ReadOnlyMemory<byte>> publicKeys);
        ILedgerWrapperBuilder MakeDataBlock(ReadOnlyMemory<byte> data);
        ILedgerWrapperBuilder MakeReferenceBlock(IReadOnlyList<ReadOnlyMemory<byte>> hashes);
    }

    public interface ILedgerWrapperBuilder {
        ILedgerWrapperBuilder AddAuthorSignatureEncapsulation(ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> secretKey);
        ILedgerWrapperBuilder AddInContextEncapsulation();
        ILedgerWrapperBuilder AddPreviousBlockEncapsulation();
        ILedgerWrapperBuilder AddUpdateContextEncapsulation();
        ILedgerBasicBlockBuilder Build(out byte[] block);
    }
}
