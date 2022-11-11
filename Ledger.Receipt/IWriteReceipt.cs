namespace Traent.Ledger.Receipt {

    public interface IWriteReceipt {
        byte[] Ledger { get; }
        byte[] Hash { get; }
        byte[] MerkleRoot { get; }
        DateTimeOffset WrittenAt { get; }
    }
}
