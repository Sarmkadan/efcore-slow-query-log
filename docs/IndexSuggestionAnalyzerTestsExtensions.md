# IndexSuggestionAnalyzerTestsExtensions

Helper class providing extension methods for testing index suggestion functionality in Entity Framework Core's slow query logging system. These methods encapsulate common test patterns for verifying that the `IndexSuggestionAnalyzer` correctly identifies columns that would benefit from indexing based on SQL query analysis.

## API

### `RunSuggestsIndexForWhereColumn`

Runs a test case that verifies the analyzer suggests an index for a column used in a WHERE clause. The test constructs a query filtering on a specific column and asserts that the analyzer produces a suggestion to create an index on that column.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer fails to suggest an index for the WHERE clause column

---

### `RunSqlWithoutFiltersYieldsNoSuggestions`

Runs a test case that verifies the analyzer produces no suggestions when the SQL query contains no filter conditions. The test constructs a simple query without WHERE clauses and asserts that no index suggestions are generated.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer produces any suggestions for a query without filters

---
### `RunSuggestsJoinAndOrderColumns`

Runs a test case that verifies the analyzer suggests indexes for columns used in JOIN conditions and ORDER BY clauses. The test constructs a query with JOIN and ORDER BY operations and asserts that the analyzer produces appropriate suggestions for the involved columns.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer fails to suggest indexes for JOIN or ORDER BY columns

---
### `RunEmptySqlYieldsNoSuggestions`

Runs a test case that verifies the analyzer produces no suggestions when the SQL query is empty or contains only whitespace. The test constructs an empty query string and asserts that no index suggestions are generated.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer produces any suggestions for an empty query

---
### `RunParameterMarkersAreNotTreatedAsColumns`

Runs a test case that verifies the analyzer does not treat parameter markers (e.g., `@p0`) as column references. The test constructs a query using parameterized values and asserts that the analyzer ignores these markers when generating suggestions.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer incorrectly treats parameter markers as column references

---
### `RunNumericLiteralsAreNotTreatedAsColumns`

Runs a test case that verifies the analyzer does not treat numeric literals as column references. The test constructs a query with numeric values in conditions and asserts that the analyzer ignores these literals when generating suggestions.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer incorrectly treats numeric literals as column references

---
### `RunOrderByOrdinalIsNotTreatedAsColumn`

Runs a test case that verifies the analyzer does not treat ORDER BY ordinal positions (e.g., `ORDER BY 1`) as column references. The test constructs a query with ordinal-based sorting and asserts that the analyzer ignores these ordinals when generating suggestions.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the analyzer incorrectly treats ORDER BY ordinals as column references

---
### `RunToSqlHintBuildsCreateIndexStatement`

Runs a test case that verifies the analyzer can generate a valid CREATE INDEX statement when a SQL hint is provided. The test constructs a query with a hint and asserts that the generated suggestion is a syntactically correct CREATE INDEX statement.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if the generated CREATE INDEX statement is invalid or malformed

---
### `RunAllTests`

Executes all the individual test methods provided by this class. This method serves as a convenience to run the entire suite of index suggestion analyzer tests in one call.

Parameters:
- None

Return value:
- `void`

Throws:
- `XunitException` if any of the individual test methods fail

## Usage

```csharp
// Example 1: Testing WHERE clause index suggestion
[Fact]
public void TestWhereClauseIndexSuggestion()
{
    IndexSuggestionAnalyzerTestsExtensions.RunSuggestsIndexForWhereColumn();
}

// Example 2: Testing combined JOIN and ORDER BY scenarios
[Fact]
public void TestJoinAndOrderByIndexSuggestions()
{
    IndexSuggestionAnalyzerTestsExtensions.RunSuggestsJoinAndOrderColumns();
}
```

## Notes

- These extension methods are designed for use within xUnit test classes and assume the presence of the EF Core slow query logging infrastructure.
- Thread safety is not a concern for these methods as they are intended for test execution and do not maintain state between invocations.
- The methods validate analyzer behavior against specific SQL patterns rather than testing the analyzer's internal logic directly.
- Edge cases implicitly covered include queries with multiple conditions, mixed parameterized and literal values, and complex JOIN hierarchies.
