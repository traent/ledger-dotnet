using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace Traent.Ledger.Crypto.Test {
    public class HexString : IEquatable<HexString> {
        private readonly string _original;
        private readonly byte[] _bytes;

        private HexString(string original) {
            _original = original;
            _bytes = Convert.FromHexString(original);
        }

        public int Length => _bytes.Length;
        public HexString Slice(int start, int length) =>
            HexString.Wrap(_original.Substring(start * 2, length * 2));

        public override string ToString() => _original;
        public byte[] ToBytes() => _bytes.ToArray();

        public bool Equals(HexString? other) =>
            other is not null && _bytes.SequenceEqual(other._bytes);

        public static implicit operator ReadOnlySpan<byte>(HexString hexString) => hexString._bytes;

        public static HexString Wrap(string original) => new HexString(original);
        public static HexString Wrap(ReadOnlySpan<byte> original) =>
            new HexString(Convert.ToHexString(original));
    }

    public class HexColumnFileDataAttribute : DataAttribute {
        private readonly string _filePath;

        /// <summary>
        /// Load data from a file as the data source for a theory
        /// </summary>
        /// <param name="filePath">The absolute or relative path to the file to load</param>
        public HexColumnFileDataAttribute(string filePath) {
            // Get the absolute path to the input file
            filePath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);

            if (!File.Exists(filePath)) {
                throw new ArgumentException($"Could not find file at path: {filePath}");
            }

            _filePath = filePath;
        }

        /// <inheritDoc />
        public override IEnumerable<object[]> GetData(MethodInfo testMethod) {
            if (testMethod == null) {
                throw new ArgumentNullException(nameof(testMethod));
            }

            // Load the file by line
            return File.ReadAllLines(_filePath)
                .Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim().Split(':').Select(HexString.Wrap).ToArray());
        }
    }
}
