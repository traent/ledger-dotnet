namespace Traent.Notary.Proof {
    public interface IInclusionProof {
        byte[] LedgerId { get; }
        int Iteration { get; }

        byte[][] Path { get; }
        byte[] MerkleRoot { get; }

        void Verify(byte[]? digest = null, byte[]? previousDigest = null);
    }
}
