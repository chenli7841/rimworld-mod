using System;
using System.Text;

namespace LiAIChat.AI
{
    public static class OpenAIResponseParser
    {
        public static string ExtractOutputText(string json)
        {
            int outputTextIndex =
                json.IndexOf(
                    "\"output_text\"",
                    StringComparison.Ordinal);

            if (outputTextIndex < 0)
            {
                throw new Exception(
                    "Could not find output_text in OpenAI response. " +
                    "Response: " + json
                );
            }

            int textPropertyIndex =
                json.IndexOf(
                    "\"text\"",
                    outputTextIndex,
                    StringComparison.Ordinal);

            if (textPropertyIndex < 0)
            {
                throw new Exception(
                    "Could not find text property in OpenAI response. " +
                    "Response: " + json
                );
            }

            int colonIndex =
                json.IndexOf(
                    ':',
                    textPropertyIndex);

            if (colonIndex < 0)
            {
                throw new Exception(
                    "Invalid OpenAI response: text property has no value."
                );
            }

            int startQuote =
                json.IndexOf(
                    '"',
                    colonIndex + 1);

            if (startQuote < 0)
            {
                throw new Exception(
                    "Invalid OpenAI response: text value not found."
                );
            }

            StringBuilder result =
                new StringBuilder();

            for (int i = startQuote + 1;
                 i < json.Length;
                 i++)
            {
                char c = json[i];

                // End of JSON string
                if (c == '"')
                {
                    break;
                }

                // Normal character
                if (c != '\\')
                {
                    result.Append(c);
                    continue;
                }

                // Escape sequence
                if (i + 1 >= json.Length)
                {
                    break;
                }

                char escaped = json[++i];

                switch (escaped)
                {
                    case '"':
                        result.Append('"');
                        break;

                    case '\\':
                        result.Append('\\');
                        break;

                    case '/':
                        result.Append('/');
                        break;

                    case 'b':
                        result.Append('\b');
                        break;

                    case 'f':
                        result.Append('\f');
                        break;

                    case 'n':
                        result.Append('\n');
                        break;

                    case 'r':
                        result.Append('\r');
                        break;

                    case 't':
                        result.Append('\t');
                        break;

                    case 'u':
                        {
                            // JSON Unicode escape:
                            // \u4f60 -> 你

                            if (i + 4 >= json.Length)
                            {
                                throw new Exception(
                                    "Invalid Unicode escape in OpenAI response."
                                );
                            }

                            string hex =
                                json.Substring(
                                    i + 1,
                                    4);

                            int unicodeValue =
                                Convert.ToInt32(
                                    hex,
                                    16);

                            result.Append(
                                (char)unicodeValue
                            );

                            i += 4;

                            break;
                        }

                    default:
                        result.Append(escaped);
                        break;
                }
            }

            return result.ToString();
        }
    }
}