namespace Traent.Ledger.Receipt {
    public enum ValidationProblem {
        MalformedReceipt,
        InvalidSignature,
        MalformedPayload,
        LedgerIdMismatch,
        HashMismatch,
        MerkleRootMismatch,
        BeforeMinTime,
        AfterMaxTime,
    }
}
