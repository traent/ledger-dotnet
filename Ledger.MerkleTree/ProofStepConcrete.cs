namespace Traent.Ledger.MerkleTree {
    public readonly struct ProofStepConcrete<T> {
        public bool AppendToLeft { get; init; }
        public T Value { get; init; }
    }
}
