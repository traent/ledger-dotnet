namespace Traent.Ledger.Receipt {
    public interface IWriteReceiptConstraints {
        byte[] Ledger { get; }
        byte[] Hash { get; }
        byte[]? MerkleRoot { get; }
        DateTimeOffset MinWrittenAt { get; }
        DateTimeOffset MaxWrittenAt { get; }
    }
}
