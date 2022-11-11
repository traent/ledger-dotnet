using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Traent.Ledger.Evaluator {
    static class PolicyParser {
        public static Policy Parse(ulong version, ReadOnlySpan<byte> policyBytes) => version switch {
            1 => ParseV1(policyBytes),
            _ => throw new ArgumentException(nameof(version))
        };

        private static Policy ParseV1(ReadOnlySpan<byte> policyBytes) {
            var policy = JsonSerializer.Deserialize<Policy>(policyBytes);

            // FIXME: we would like to enforce some constraints that are not
            // part of the JSON schema specification. We should use a custom
            // deserializer for policies to enforce them

            // sanity checks; these should be already enforced by the validation
            Debug.Assert(policy is not null);
            Debug.Assert(policy.HashingAlgorithm is not null);
            Debug.Assert(policy.SigningAlgorithm is not null);
            Debug.Assert(policy.LedgerPublicKey is not null);
            Debug.Assert(policy.AllowedBlocks is not null);
            Debug.Assert(policy.AllowedBlocks.All(block => block is not null));
            Debug.Assert(policy.AuthorKeys is not null);
            Debug.Assert(policy.AuthorKeys.All(publicKey => publicKey is not null));

            if (policy.MaxBlockSize <= 0) {
                throw new Exception("Invalid max block size");
            }

            var allowed = policy.ParseAllowedBlocks().ToList();
            if (allowed.Count != allowed.Distinct().Count()) {
                throw new Exception("Repeated block type");
            }

            return policy;
        }
    }
}
