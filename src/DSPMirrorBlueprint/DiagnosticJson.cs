using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace DSPMirrorBlueprint
{
    // Serializes only the dictionaries, lists, and scalar values assembled by
    // the diagnostic exporter. It never traverses live game objects.
    internal static class DiagnosticJson
    {
        public static string Stringify(object value)
        {
            var builder = new StringBuilder(64 * 1024);
            WriteValue(builder, value, 0);
            builder.Append('\n');
            return builder.ToString();
        }

        private static void WriteValue(
            StringBuilder builder,
            object value,
            int indent)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            string text = value as string;
            if (text != null)
            {
                WriteString(builder, text);
                return;
            }

            if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
                return;
            }

            Type type = value.GetType();
            if (IsNumber(type))
            {
                if (value is double &&
                    (Double.IsNaN((double)value) || Double.IsInfinity((double)value)))
                {
                    builder.Append("null");
                    return;
                }
                if (value is float &&
                    (Single.IsNaN((float)value) || Single.IsInfinity((float)value)))
                {
                    builder.Append("null");
                    return;
                }

                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                WriteObject(builder, dictionary, indent);
                return;
            }

            IEnumerable sequence = value as IEnumerable;
            if (sequence != null)
            {
                WriteArray(builder, sequence, indent);
                return;
            }

            WriteString(builder, value.ToString());
        }

        private static void WriteObject(
            StringBuilder builder,
            IDictionary dictionary,
            int indent)
        {
            builder.Append('{');
            bool first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!first) builder.Append(',');
                first = false;
                builder.Append('\n');
                Indent(builder, indent + 1);
                WriteString(
                    builder,
                    Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
                builder.Append(": ");
                WriteValue(builder, entry.Value, indent + 1);
            }

            if (!first)
            {
                builder.Append('\n');
                Indent(builder, indent);
            }
            builder.Append('}');
        }

        private static void WriteArray(
            StringBuilder builder,
            IEnumerable sequence,
            int indent)
        {
            builder.Append('[');
            bool first = true;
            foreach (object item in sequence)
            {
                if (!first) builder.Append(',');
                first = false;
                builder.Append('\n');
                Indent(builder, indent + 1);
                WriteValue(builder, item, indent + 1);
            }

            if (!first)
            {
                builder.Append('\n');
                Indent(builder, indent);
            }
            builder.Append(']');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32)
                            builder.Append("\\u" + ((int)character).ToString("x4"));
                        else
                            builder.Append(character);
                        break;
                }
            }
            builder.Append('"');
        }

        private static bool IsNumber(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        private static void Indent(StringBuilder builder, int count)
        {
            for (int i = 0; i < count; i++) builder.Append("  ");
        }
    }
}
