using System.Collections.Generic;
using System.Linq;

namespace Traent.Ledger.Evaluator {
    public record Policy(
        byte[] LedgerPublicKey,
        int MaxBlockSize,
        string HashingAlgorithm,
        string SigningAlgorithm,
        byte[][] AllowedBlocks,
        byte[][] AuthorKeys,
        ApplicationData ApplicationData
    ) {
        internal IEnumerable<BlockTypeSequence> ParseAllowedBlocks() =>
            AllowedBlocks.Select(BlockTypeSequence.FromRawBytes);
    }
}
