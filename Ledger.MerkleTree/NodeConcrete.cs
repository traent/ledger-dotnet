namespace Traent.Ledger.MerkleTree {
    public record NodeConcrete<T>(
        PerfectBinaryTree.Node Node,
        T Value
    );
}
