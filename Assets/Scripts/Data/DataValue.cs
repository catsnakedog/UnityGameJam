using System;
using System.Globalization;
using System.Linq;

namespace GameJam.Data
{
    /// <summary>Shared import/runtime conversion. Values are parsed once when a table is indexed.</summary>
    public static class DataValue
    {
        public static string CSharpType(string type)
        {
            switch (type)
            {
                case "string": case "int": case "long": case "float": case "double": case "bool": return type;
                case "string[]": case "int[]": case "long[]": case "float[]": case "double[]": case "bool[]":
                    return "global::System.Collections.Generic.IReadOnlyList<" + type.Substring(0, type.Length - 2) + ">";
                default: throw new FormatException("Unsupported data type: " + type);
            }
        }

        public static object Parse(string type, string text)
        {
            text = text ?? "";
            if (type.EndsWith("[]", StringComparison.Ordinal))
            {
                var values = text.Length == 0 ? Array.Empty<string>() : text.Split('|');
                switch (type)
                {
                    case "string[]": return Array.AsReadOnly(values);
                    case "int[]": return Array.AsReadOnly(values.Select(x => (int)Parse("int", x)).ToArray());
                    case "long[]": return Array.AsReadOnly(values.Select(x => (long)Parse("long", x)).ToArray());
                    case "float[]": return Array.AsReadOnly(values.Select(x => (float)Parse("float", x)).ToArray());
                    case "double[]": return Array.AsReadOnly(values.Select(x => (double)Parse("double", x)).ToArray());
                    case "bool[]": return Array.AsReadOnly(values.Select(x => (bool)Parse("bool", x)).ToArray());
                }
            }
            switch (type)
            {
                case "string": return text;
                case "int": return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "long": return long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "float":
                    var single = float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (float.IsNaN(single) || float.IsInfinity(single)) throw new FormatException("A finite number is required.");
                    return single;
                case "double":
                    var number = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (double.IsNaN(number) || double.IsInfinity(number)) throw new FormatException("A finite number is required.");
                    return number;
                case "bool":
                    if (text == "1") return true;
                    if (text == "0") return false;
                    return bool.Parse(text);
                default: throw new FormatException("Unsupported data type: " + type);
            }
        }

        public static string Key(object value) => Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
