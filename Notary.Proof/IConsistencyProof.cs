using System.Text.Json.Serialization;

namespace Traent.Notary.Proof {
    public interface IConsistencyProof {
        byte[] LedgerId { get; }
        int Iteration { get; }

        byte[][][] PathHistory { get; }
        byte[][] MerkleConsistencyProofs { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        byte[]? MerkleRoot { get; }

        void Verify(byte[]? digest = null);
    }
}
