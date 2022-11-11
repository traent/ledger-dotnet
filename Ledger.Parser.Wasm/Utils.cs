using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ledger.Parser.Wasm {
    [JsonConverter(typeof(JsNumberConverter))]
    public readonly struct JsNumber {
        private const long MAX_SAFE_INTEGER = 9007199254740991;
        private const long MIN_SAFE_INTEGER = -9007199254740991;

        public double Value { get; }

        public JsNumber(double value) {
            Value = value;
        }

        public static implicit operator JsNumber(ulong v) {
            if (v <= MAX_SAFE_INTEGER) {
                return new JsNumber(v);
            } else {
                throw new ArgumentOutOfRangeException($"{v} cannot be safely converted to JS");
            }
        }

        public static implicit operator JsNumber(long v) {
            if (MIN_SAFE_INTEGER <= v && v <= MAX_SAFE_INTEGER) {
                return new JsNumber(v);
            } else {
                throw new ArgumentOutOfRangeException($"{v} cannot be safely converted to JS");
            }
        }
    }

    internal class JsNumberConverter : JsonConverter<JsNumber> {
        public override JsNumber Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => new JsNumber(reader.GetDouble());

        public override void Write(
            Utf8JsonWriter writer,
            JsNumber value,
            JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
    }
}
