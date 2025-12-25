using System.Globalization;
using System.Text;

namespace QueryParserLib;

/// <summary>
/// Represents a segment in a JSON query path, consisting of a property name and optional array indices.
/// </summary>
/// <param name="Name">The name of the property or field.</param>
/// <param name="Indices">A read-only list of integer indices for array access within this segment.</param>
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

    private static (bool IsName, string Value) ParseDotSelector(string expression, ref int pos)
    {
        pos++; // skip '.'
        var sb = new StringBuilder();
        while (pos < expression.Length)
        {
            var ch = expression[pos];
            if (ch == '\\')
            {
                AppendWithEscape(sb, expression, ref pos, new[] { '\\', '[', ']', '.', '\'' }, "compact dot name");
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
        var value = sb.ToString();
        if (value.Length == 0)
            throw new FormatException("Empty name after '.' in compact dot notation");
        return (true, value);
    }

    /// <summary>
    /// Parses a JSON query expression into an ordered list of segments.
    /// </summary>
    /// <param name="expression">The query expression to parse.</param>
    /// <returns>A list of <see cref="Segment"/> objects representing the parsed query.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when the expression has invalid syntax.</exception>
    /// <remarks>
    /// The expression syntax is as follows:
    /// - Expression must start with <c>$</c> (root identifier).
    /// - Expression contains one or more selectors after the root identifier.
    /// - Selectors are grouped into segments: each name_selector starts a new segment, followed by zero or more index_selectors.
    /// - name_selector: <c>['</c>string<c>']</c> or <c>.</c>string (compact dot notation)
    /// - index_selector: <c>[</c>integer<c>]</c>
    /// 
    /// Supported escape sequences in strings: <c>\</c>, <c>[</c>, <c>]</c>, <c>.</c>, <c>'</c>
    /// </remarks>
    public static List<Segment> Parse(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var results = new List<Segment>();
        int pos = 0;
        Segment? currentSegment = null;

        // All expressions must start with $
        SkipWhitespace(expression, ref pos);
        if (pos >= expression.Length || expression[pos] != '$')
            throw new FormatException("Expression must start with '$' (root identifier)");
        pos++; // consume $

        while (pos < expression.Length)
        {
            SkipWhitespace(expression, ref pos);
            if (pos >= expression.Length) break;

            bool isName;
            string value;

            if (expression[pos] == '.')
            {
                // Parse compact dot notation name selector
                (isName, value) = ParseDotSelector(expression, ref pos);
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
                AppendWithEscape(sb, expression, ref pos, new[] { '\\', '[', ']', '.', '\'' }, "selector");
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
