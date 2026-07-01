using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FileDeduper.Utils
{
    /// <summary>
    /// 极简 JSON 解析器，仅用于配置文件读写。
    /// 支持子集：对象( Dictionary&lt;string,object&gt; )、数组( List&lt;object&gt; )、
    /// 字符串、数字( double 或 long )、bool、null。
    /// 不依赖任何第三方库，不依赖 System.Web.Extensions。
    /// </summary>
    internal static class MiniJson
    {
        public static object Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int index = 0;
            SkipWhitespace(text, ref index);
            return ParseValue(text, ref index);
        }

        public static string Quote(string s)
        {
            var sb = new StringBuilder();
            sb.Append('"');
            if (s != null)
            {
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '\\': sb.Append("\\\\"); break;
                        case '"': sb.Append("\\\""); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length)
            {
                char c = text[index];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') index++;
                else break;
            }
        }

        private static object ParseValue(string text, ref int index)
        {
            SkipWhitespace(text, ref index);
            if (index >= text.Length) return null;
            char c = text[index];
            if (c == '{') return ParseObject(text, ref index);
            if (c == '[') return ParseArray(text, ref index);
            if (c == '"') return ParseString(text, ref index);
            if (c == 't' || c == 'f') return ParseBool(text, ref index);
            if (c == 'n') return ParseNull(text, ref index);
            return ParseNumber(text, ref index);
        }

        private static Dictionary<string, object> ParseObject(string text, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // skip '{'
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == '}')
            {
                index++;
                return dict;
            }
            while (index < text.Length)
            {
                SkipWhitespace(text, ref index);
                string key = ParseString(text, ref index);
                SkipWhitespace(text, ref index);
                if (index < text.Length && text[index] == ':') index++;
                object value = ParseValue(text, ref index);
                dict[key] = value;
                SkipWhitespace(text, ref index);
                if (index < text.Length && text[index] == ',') { index++; continue; }
                if (index < text.Length && text[index] == '}') { index++; break; }
                break;
            }
            return dict;
        }

        private static List<object> ParseArray(string text, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == ']')
            {
                index++;
                return list;
            }
            while (index < text.Length)
            {
                object value = ParseValue(text, ref index);
                list.Add(value);
                SkipWhitespace(text, ref index);
                if (index < text.Length && text[index] == ',') { index++; continue; }
                if (index < text.Length && text[index] == ']') { index++; break; }
                break;
            }
            return list;
        }

        private static string ParseString(string text, ref int index)
        {
            var sb = new StringBuilder();
            index++; // skip opening quote
            while (index < text.Length)
            {
                char c = text[index++];
                if (c == '"') break;
                if (c == '\\' && index < text.Length)
                {
                    char esc = text[index++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 3 < text.Length)
                            {
                                string hex = text.Substring(index, 4);
                                int code = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                sb.Append((char)code);
                                index += 4;
                            }
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string text, ref int index)
        {
            int start = index;
            bool isDouble = false;
            while (index < text.Length)
            {
                char c = text[index];
                if (c == '.' || c == 'e' || c == 'E') isDouble = true;
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                    index++;
                else break;
            }
            string num = text.Substring(start, index - start);
            if (isDouble)
            {
                double d;
                double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
                return d;
            }
            long l;
            if (long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) return l;
            double dd;
            double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out dd);
            return dd;
        }

        private static bool ParseBool(string text, ref int index)
        {
            if (index + 4 <= text.Length && text.Substring(index, 4) == "true")
            {
                index += 4;
                return true;
            }
            if (index + 5 <= text.Length && text.Substring(index, 5) == "false")
            {
                index += 5;
                return false;
            }
            return false;
        }

        private static object ParseNull(string text, ref int index)
        {
            if (index + 4 <= text.Length && text.Substring(index, 4) == "null")
            {
                index += 4;
            }
            return null;
        }
    }
}
