using System.Globalization;
using System.Text;

namespace QueryParserLib;

public sealed record Segment(string Name, IReadOnlyList<int> Indices);

public static class QueryParser
{
    // Parse an expression into ordered list of segments.
    // Syntax assumed:
    // - Expression contains one or more selectors, must start with a name_selector.
    // - Selectors are grouped into segments: each name_selector starts a new segment, followed by zero or more index_selectors.
    // - name_selector: ['string'] where string is enclosed in single quotes.
    // - index_selector: [integer]
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

            var (isName, value) = ParseSelector(expression, ref pos);

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
        if (pos >= expression.Length || expression[pos] != '[')
            throw new FormatException("Expected '[' at start of selector");

        pos++; // skip '['
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
                if (ch == '\\')
                {
                    pos++;
                    if (pos >= expression.Length) throw new FormatException("Invalid escape in selector");
                    sb.Append(expression[pos]);
                    pos++;
                    continue;
                }
                sb.Append(ch);
                pos++;
                continue;
            }

            if (ch == ']')
            {
                pos++;
                break;
            }

            if (ch == '\'')
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
