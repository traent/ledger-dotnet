namespace Traent.Ledger.Evaluator {
    public interface ILedgerState {
        Policy Policy { get; }
        ulong BlockCount { get; }
        byte[] HeadBlockHash { get; }
        byte[] HeadLinkHash { get; }
        byte[] ContextLinkHash { get; }
    }
}
