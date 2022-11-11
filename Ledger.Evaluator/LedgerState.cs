namespace Traent.Ledger.Evaluator {
    record LedgerState(
        Policy Policy,
        ulong BlockCount,
        byte[] HeadBlockHash,
        byte[] HeadLinkHash,
        byte[] ContextLinkHash
    ) : ILedgerState;
}
