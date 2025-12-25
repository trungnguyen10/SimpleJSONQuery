using System.Globalization;
using System.Text;

namespace QueryParserLib;

public sealed record Segment(string Name, IReadOnlyList<int> Indices);

public static class QueryParser
{
    private const char SelectorOpen = '[';
    private const char SelectorClose = ']';
    private const char NameQuote = '\'';

    private static void AppendWithEscape(StringBuilder sb, string expression, ref int pos, char[]? allowedEscapes, string context)
    {
        var ch = expression[pos];
        if (ch == '\\')
        {
            pos++;
            if (pos >= expression.Length)
                throw new FormatException($"Invalid escape in {context}");
            var next = expression[pos];
            if (allowedEscapes == null || Array.IndexOf(allowedEscapes, next) >= 0)
            {
                sb.Append(next);
                pos++;
            }
            else
            {
                throw new FormatException($"Invalid escape sequence '\\{next}' in {context}");
            }
        }
        else
        {
            sb.Append(ch);
            pos++;
        }
    }

    // Parse an expression into ordered list of segments.
    // Syntax assumed:
    // - Expression contains one or more selectors, must start with a name_selector.
    // - Selectors are grouped into segments: each name_selector starts a new segment, followed by zero or more index_selectors.
    // - name_selector: [SelectorOpen NameQuote string NameQuote SelectorClose] or .string (compact dot notation)
    // - index_selector: [SelectorOpen integer SelectorClose]
    public static List<Segment> Parse(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var results = new List<Segment>();
        int pos = 0;
        Segment? currentSegment = null;

        while (pos < expression.Length)
        {
            SkipWhitespace(expression, ref pos);
            if (pos >= expression.Length) break;

            bool isName;
            string value;

            if (expression[pos] == '.')
            {
                // Parse compact dot notation name selector
                pos++; // skip '.'
                var sb = new StringBuilder();
                while (pos < expression.Length)
                {
                    var ch = expression[pos];
                    if (ch == '\\')
                    {
                        AppendWithEscape(sb, expression, ref pos, new[] { '\\', '.' }, "compact dot name");
                    }
                    else if (char.IsWhiteSpace(ch) || ch == SelectorOpen)
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(ch);
                        pos++;
                    }
                }
                value = sb.ToString();
                if (value.Length == 0)
                    throw new FormatException("Empty name after '.' in compact dot notation");
                isName = true;
            }
            else if (expression[pos] == SelectorOpen)
            {
                // Parse bracketed selector
                (isName, value) = ParseSelector(expression, ref pos);
            }
            else
            {
                throw new FormatException($"Expected '.' or '{SelectorOpen}' at start of selector");
            }

            if (isName)
            {
                // Start new segment
                if (currentSegment != null)
                {
                    results.Add(currentSegment);
                }
                currentSegment = new Segment(value, new List<int>());
            }
            else
            {
                // Must have a current segment
                if (currentSegment == null)
                    throw new FormatException("Expression must start with a name selector");

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
                    throw new FormatException($"Selector '{value}' is not a valid integer index");

                ((List<int>)currentSegment.Indices).Add(idx);
            }

            SkipWhitespace(expression, ref pos);
        }

        if (currentSegment != null)
        {
            results.Add(currentSegment);
        }

        if (results.Count == 0)
            throw new FormatException("Expression must contain at least one selector");

        return results;
    }

    private static void SkipWhitespace(string expression, ref int pos)
    {
        while (pos < expression.Length && char.IsWhiteSpace(expression[pos])) pos++;
    }

    private static (bool IsName, string Value) ParseSelector(string expression, ref int pos)
    {
        if (pos >= expression.Length || expression[pos] != SelectorOpen)
            throw new FormatException($"Expected '{SelectorOpen}' at start of selector");

        pos++; // skip SelectorOpen
        var sb = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';
        bool wasQuoted = false;

        while (pos < expression.Length)
        {
            var ch = expression[pos];
            if (inQuotes)
            {
                if (ch == quoteChar)
                {
                    inQuotes = false;
                    pos++;
                    continue;
                }
                AppendWithEscape(sb, expression, ref pos, null, "selector");
                continue;
            }

            if (ch == SelectorClose)
            {
                pos++;
                break;
            }

            if (ch == NameQuote)
            {
                wasQuoted = true;
                inQuotes = true;
                quoteChar = ch;
                pos++;
                continue;
            }

            sb.Append(ch);
            pos++;
        }

        if (inQuotes) throw new FormatException("Unterminated quoted string in selector");

        var content = sb.ToString().Trim();
        if (content.Length == 0) throw new FormatException("Empty selector");

        return (wasQuoted, content);
    }
}
