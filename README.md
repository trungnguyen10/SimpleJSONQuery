# SimpleJSONQuery

A lightweight C# library for parsing JSON query expressions into structured segments.

## Overview

SimpleJSONQuery provides a simple way to parse JSON path-like query expressions into a list of `Segment` objects, each containing a property name and optional array indices.

## Features

- Parse JSON query expressions with bracket notation: `['property'][0]['nested'][1]`
- Support for compact dot notation: `.property[0].nested[1]`
- Mixed notation support
- Escape sequences for special characters: `\`, `[`, `]`, `.`, `'`
- Whitespace tolerance

## Installation

Add the library to your project via NuGet (when published) or reference the project directly.

## Usage

```csharp
using QueryParserLib;

var segments = QueryParser.Parse("$['users'][0]['name']");

// Result:
// segments[0].Name = "users"
// segments[0].Indices = [0]
// segments[1].Name = "name"
// segments[1].Indices = []
```

### Query Syntax

All expressions must start with `$` (the root identifier).

#### Bracket Notation
- Property access: `$['propertyName']`
- Array index: `$[0]`, `$[1]`, etc.
- Combined: `$['items'][0]['name']`

#### Compact Dot Notation
- Property access: `$.propertyName`
- Combined with indices: `$.items[0].name`

#### Mixed Notation
- `$.root[0]['child'][1]`

#### Escaping
Special characters can be escaped with backslash:
- `$['my\.property']` - literal dot in property name
- `$['item\[0\]']` - literal brackets in property name
- `$['name\'s']` - literal single quote in property name
- `$['path\\to\\file']` - literal backslash in property name

## API Reference

### QueryParser.Parse(string expression)

Parses a JSON query expression into an ordered list of segments.

**Parameters:**
- `expression`: The query expression string to parse

**Returns:**
- `List<Segment>`: Ordered list of parsed segments

**Exceptions:**
- `ArgumentNullException`: When expression is null
- `FormatException`: When expression has invalid syntax

### Segment Record

Represents a segment in the query path.

**Properties:**
- `Name`: The property name (string)
- `Indices`: Read-only list of array indices (IReadOnlyList<int>)

## Examples

```csharp
// Simple property access
var result = QueryParser.Parse("$['user']");
// 1 segment: Name="user", Indices=[]

// Array access
var result = QueryParser.Parse("$['items'][0]");
// 1 segment: Name="items", Indices=[0]

// Nested access
var result = QueryParser.Parse("$['data']['users'][0]['name']");
// 2 segments:
//   0: Name="data", Indices=[]
//   1: Name="users", Indices=[0]
//   2: Name="name", Indices=[]

// Compact dot notation
var result = QueryParser.Parse("$.user.profile");
// 2 segments: Name="user", Name="profile"

// Mixed with escaping
var result = QueryParser.Parse("$.my\\.data[0]['special\'name']");
// 2 segments:
//   0: Name="my.data", Indices=[0]
//   1: Name="special'name", Indices=[]
```

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.