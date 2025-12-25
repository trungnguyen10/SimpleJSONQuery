using QueryParserLib;

namespace QueryParserLib.Tests;

public class QueryParserTests
{
    [Fact]
    public void Parse_SingleSegment_NameOnly()
    {
        var result = QueryParser.Parse("$['person']");
        Assert.Single(result);
        Assert.Equal("person", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_SingleSegment_WithIndices()
    {
        var result = QueryParser.Parse("$['items'][0][2]");
        Assert.Single(result);
        Assert.Equal("items", result[0].Name);
        Assert.Equal(new[] { 0, 2 }, result[0].Indices);
    }

    [Fact]
    public void Parse_MultipleSegments()
    {
        var result = QueryParser.Parse("$['root'][0]['child'][1]");
        Assert.Equal(2, result.Count);
        Assert.Equal("root", result[0].Name);
        Assert.Equal(new[] { 0 }, result[0].Indices);
        Assert.Equal("child", result[1].Name);
        Assert.Equal(new[] { 1 }, result[1].Indices);
    }

    [Fact]
    public void Parse_QuotedNameWithSpaces()
    {
        var result = QueryParser.Parse("$['a b'][10]");
        Assert.Single(result);
        Assert.Equal("a b", result[0].Name);
        Assert.Equal(new[] { 10 }, result[0].Indices);
    }

    [Fact]
    public void Parse_EscapedSingleQuote()
    {
        var result = QueryParser.Parse("$['a\\'b'][3]");
        Assert.Single(result);
        Assert.Equal("a'b", result[0].Name);
        Assert.Equal(new[] { 3 }, result[0].Indices);
    }

    [Fact]
    public void Parse_EscapedPeriod()
    {
        var result = QueryParser.Parse("$['my\\.selector']");
        Assert.Single(result);
        Assert.Equal("my.selector", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_MultipleEscapedPeriod()
    {
        var result = QueryParser.Parse("$['my\\.selector \\.another']");
        Assert.Single(result);
        Assert.Equal("my.selector .another", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_WithWhiteSpace()
    {
        var result = QueryParser.Parse("$['my selector']");
        Assert.Single(result);
        Assert.Equal("my selector", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_ThrowsIfStartsWithIndex()
    {
        Assert.Throws<FormatException>(() => QueryParser.Parse("$[0]"));
    }

    [Fact]
    public void Parse_CompactDot_SingleSegment_NameOnly()
    {
        var result = QueryParser.Parse("$.person");
        Assert.Single(result);
        Assert.Equal("person", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_CompactDot_SingleSegment_WithIndices()
    {
        var result = QueryParser.Parse("$.items[0][2]");
        Assert.Single(result);
        Assert.Equal("items", result[0].Name);
        Assert.Equal(new[] { 0, 2 }, result[0].Indices);
    }

    [Fact]
    public void Parse_CompactDot_MultipleSegments()
    {
        var result = QueryParser.Parse("$.root[0].child[1]");
        Assert.Equal(2, result.Count);
        Assert.Equal("root", result[0].Name);
        Assert.Equal(new[] { 0 }, result[0].Indices);
        Assert.Equal("child", result[1].Name);
        Assert.Equal(new[] { 1 }, result[1].Indices);
    }

    [Fact]
    public void Parse_MixedNotation()
    {
        var result = QueryParser.Parse("$.root[0]['child'][1]");
        Assert.Equal(2, result.Count);
        Assert.Equal("root", result[0].Name);
        Assert.Equal(new[] { 0 }, result[0].Indices);
        Assert.Equal("child", result[1].Name);
        Assert.Equal(new[] { 1 }, result[1].Indices);
    }

    [Fact]
    public void Parse_CompactDot_EscapedPeriod()
    {
        var result = QueryParser.Parse("$.my\\.selector");
        Assert.Single(result);
        Assert.Equal("my.selector", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_CompactDot_EscapedBackslash()
    {
        var result = QueryParser.Parse("$.my\\\\name");
        Assert.Single(result);
        Assert.Equal("my\\name", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_ThrowsIfDoesNotStartWithDollar()
    {
        Assert.Throws<FormatException>(() => QueryParser.Parse("['person']"));
        Assert.Throws<FormatException>(() => QueryParser.Parse(".person"));
        Assert.Throws<FormatException>(() => QueryParser.Parse("person"));
    }

    [Fact]
    public void Parse_EscapedBackslash()
    {
        var result = QueryParser.Parse("$['a\\\\b']");
        Assert.Single(result);
        Assert.Equal("a\\b", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_EscapedLeftBracket()
    {
        var result = QueryParser.Parse("$['a\\[b']");
        Assert.Single(result);
        Assert.Equal("a[b", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_EscapedRightBracket()
    {
        var result = QueryParser.Parse("$['a\\]b']");
        Assert.Single(result);
        Assert.Equal("a]b", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_CompactDot_EscapedLeftBracket()
    {
        var result = QueryParser.Parse("$.a\\[b");
        Assert.Single(result);
        Assert.Equal("a[b", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_CompactDot_EscapedRightBracket()
    {
        var result = QueryParser.Parse("$.a\\]b");
        Assert.Single(result);
        Assert.Equal("a]b", result[0].Name);
        Assert.Empty(result[0].Indices);
    }

    [Fact]
    public void Parse_CompactDot_EscapedSingleQuote()
    {
        var result = QueryParser.Parse("$.a\\'b");
        Assert.Single(result);
        Assert.Equal("a'b", result[0].Name);
        Assert.Empty(result[0].Indices);
    }
}
