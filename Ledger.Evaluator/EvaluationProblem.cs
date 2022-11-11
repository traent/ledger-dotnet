namespace Traent.Ledger.Evaluator {
    public enum EvaluationProblem {
        // IBlock
        MalformedBlock,
        BlockTooBig,
        BlockTypeNotAllowed,

        // Policy
        InvalidPolicyVersion,
        CannotParsePolicy,

        // Data

        // Reference
        InvalidHashLength,

        // Ack
        BlockNotFound,
        BlockLinkHashMismatch,

        // Policy/AddAuthors
        InvalidAuthorKey,
        AuthorAlreadyPresent,

        // AuthorSignature
        AuthorNotFound,
        InvalidSignature,

        // PreviousBlock
        PreviousBlockHashMismatch,

        // InContext
        ContextLinkHashMismatch
    }
}
