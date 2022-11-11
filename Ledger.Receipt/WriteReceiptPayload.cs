namespace Traent.Ledger.Receipt {
    record WriteReceiptPayload(
        byte[] Ledger,
        byte[] Hash,
        byte[] MerkleRoot,
        DateTimeOffset WrittenAt
    ) : IWriteReceipt;
}
