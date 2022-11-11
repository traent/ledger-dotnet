using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Traent.Ledger.MerkleTree;

namespace Traent.Notary.Proof {
    public class Utils {
        public static readonly byte[] NoLink = Enumerable.Repeat((byte)0, 64).ToArray();

        public static void ExtendOnePaddedPrefixByOneDepthLevel(byte[] prefix, byte currentDepth, BitArray ledgerBits) {
            for (int i = 0; i < 3; i++) {
                int bitPosition = currentDepth * 3 + i;
                if (!ledgerBits[bitPosition]) {
                    prefix[bitPosition / 8] &= (byte)~(1 << (bitPosition % 8));
                }
            }
        }

        public static byte GetLedgerKeyAtDepth(BitArray ledgerBits, byte depth) {
            byte key = 0;
            for (int i = 0; i < 3; i++) {
                if (ledgerBits[depth * 3 + i]) {
                    key |= (byte)(1 << i);
                }
            }
            return key;
        }

        public static byte[] LedgerToOnePaddedPrefixOfSpecifiedDepth(byte[] ledger, byte depth) {
            byte[] prefix = (byte[])ledger.Clone();
            int bytesBeforeFullPadding = (depth * 3 + 7) / 8;
            for (int i = bytesBeforeFullPadding; i < 64; i++) {
                prefix[i] = 255;
            }
            int bitsOfPartialPadding = (bytesBeforeFullPadding * 8) - (depth * 3);
            for (int i = 0; i < bitsOfPartialPadding; i++) {
                prefix[bytesBeforeFullPadding - 1] |= (byte)(1 << (7 - i));
            }
            return prefix;
        }

        public static ProofStepConcrete<byte[]>[] DeserializeMerkleProof(ReadOnlySpan<byte> buffer) {
            if ((buffer.Length % 65) != 0) {
                throw new System.InvalidOperationException("unexpected MerkleProof serialization format");
            }
            int stepsCount = buffer.Length / 65;
            var proof = new List<ProofStepConcrete<byte[]>>(stepsCount);
            for (int i = 0; i < stepsCount; i++) {
                proof.Add(
                    new ProofStepConcrete<byte[]> {
                        AppendToLeft = buffer[i * 65] == 1,
                        Value = buffer.Slice(1 + i * 65, 64).ToArray(),
                    }
                );
            }
            return proof.ToArray();
        }
    }
}
