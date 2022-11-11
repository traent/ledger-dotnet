using System.Text.Json;

namespace Traent.Ledger.Receipt {
    public class WriteReceiptBuilder {
        private readonly byte[] _publicKey;
        private readonly byte[] _secretKey;

        public WriteReceiptBuilder(byte[] publicKey, byte[] secretKey) {
            _publicKey = publicKey;
            _secretKey = secretKey;
        }

        public byte[] CreateWriteReceipt(byte[] ledgerId, byte[] linkHash, byte[] merkleTreeRoot, DateTimeOffset writtenAt) {
            var receiptPayload = new WriteReceiptPayload(
                Ledger: ledgerId,
                Hash: linkHash,
                MerkleRoot: merkleTreeRoot,
                WrittenAt: writtenAt
            );

            var payload = JsonSerializer.SerializeToUtf8Bytes(receiptPayload);

            var signAlgo = Ledger.Crypto.Algorithms.CreateEd25519();
            var signedPayload = new byte[
                signAlgo.PublicKeyLengthInBytes +
                signAlgo.SignatureLengthInBytes +
                payload.Length
            ];

            // CHECKME: why are we prepending the public key?
            _publicKey.CopyTo(signedPayload.AsSpan());
            signAlgo.Sign(_secretKey, payload,
                signedPayload.AsSpan(signAlgo.PublicKeyLengthInBytes, signAlgo.SignatureLengthInBytes));
            payload.CopyTo(signedPayload.AsSpan()
                .Slice(signAlgo.PublicKeyLengthInBytes + signAlgo.SignatureLengthInBytes));

            return signedPayload;
        }
    }
}
