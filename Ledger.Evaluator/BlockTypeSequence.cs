using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Leb128;
using Traent.Ledger.Parser;

namespace Traent.Ledger.Evaluator {
    struct BlockTypeSequence : IEquatable<BlockTypeSequence> {
        private readonly List<BlockType> _typeList;

        private BlockTypeSequence(List<BlockType> list) {
            _typeList = list;
        }

        public byte[] ToRawBytes() {
            var buffer = new ArrayBufferWriter<byte>();
            foreach (var block in _typeList) {
                buffer.WriteLeb128((ulong)block);
            }
            return buffer.WrittenSpan.ToArray();
        }

        public static BlockTypeSequence FromBlockTypes(params BlockType[] types) {
            return new(types.ToList());
        }

        public static BlockTypeSequence FromRawBytes(byte[] block) {
            var types = new List<BlockType>();
            var sequence = new SequenceReader<byte>(new ReadOnlySequence<byte>(block));
            while (sequence.Remaining != 0) {
                if (!sequence.TryReadLeb128(out ulong blockTypeValue)) {
                    throw new FormatException("Malformed BlockType sequence");
                }

                var blockType = (BlockType)blockTypeValue;
                if (!Enum.IsDefined(blockType)) {
                    throw new FormatException("Unknown BlockType");
                }

                types.Add(blockType);
            }
            return new(types);
        }

        public static BlockTypeSequence FromEncapsulatedBlock(IBlock block) {
            var types = new List<BlockType>();
            while (true) {
                types.Add(block.Type);
                if (block is IEncapsulation encapsulation) {
                    block = encapsulation.Inner;
                } else {
                    return new(types);
                }
            }
        }

        public bool Equals(BlockTypeSequence other) => _typeList.SequenceEqual(other._typeList);

        public override int GetHashCode() {
            var r = 0;
            foreach (var type in _typeList) {
                var numType = (ulong)type;

                r = HashHelpers.Combine(r, (int)numType);
                r = HashHelpers.Combine(r, (int)(numType >> 32));
            }
            return r;
        }
    }
}
