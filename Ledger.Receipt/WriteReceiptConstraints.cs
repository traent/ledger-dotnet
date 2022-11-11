namespace Traent.Ledger.Receipt {
    public class WriteReceiptConstraints : IWriteReceiptConstraints {
        // use 2020-01-01T00:00:00Z as default minimum time
        private static readonly DateTimeOffset DefaultMin = DateTimeOffset.UnixEpoch.AddYears(50);

        public byte[] Ledger { get; }
        public byte[] Hash { get; }
        public byte[]? MerkleRoot { get; }
        public DateTimeOffset MinWrittenAt { get; }
        public DateTimeOffset MaxWrittenAt { get; }

        public WriteReceiptConstraints(
            byte[] ledger,
            byte[] hash,
            byte[]? merkleRoot = null,
            DateTimeOffset? maxWrittenAt = null,
            DateTimeOffset? minWrittenAt = null
        ) {
            Ledger = ledger;
            Hash = hash;
            MerkleRoot = merkleRoot;
            MaxWrittenAt = maxWrittenAt ?? DateTimeOffset.UtcNow.AddMinutes(10);
            MinWrittenAt = minWrittenAt ?? DefaultMin;
        }
    }
}
