# EndToEndInterceptionTestsExtensions

Utility class providing methods to configure, capture, and inspect slow query events during end-to-end interception tests for Entity Framework Core. Designed to simplify testing scenarios where slow query logging or interception behavior must be validated without full application setup.

## API

### `SqliteConnection CreateInMemoryConnection()`

Creates and returns a new in-memory SQLite connection suitable for testing EF Core providers that require a live database connection. The connection is configured with options that ensure deterministic behavior and avoid interference from external state.

- **Parameters**: None
- **Return value**: A new `SqliteConnection` instance connected to an in-memory database.
- **Exceptions**: May throw `InvalidOperationException` if the SQLite provider is not available or if the connection cannot be initialized.

---

### `SlowQueryInterceptor CreateSlowQueryInterceptor(Action<SlowQueryLogOptions>? configure = null)`

Creates a new `SlowQueryInterceptor` instance with optional configuration for slow query logging behavior. The interceptor can be attached to an `EF.Core.DbContextOptionsBuilder` to simulate slow query logging during tests.

- **Parameters**:
  - `configure` (optional): A delegate to configure `SlowQueryLogOptions`, such as setting thresholds or enabling/disabling features.
- **Return value**: A new `SlowQueryInterceptor` instance.
- **Exceptions**: May throw `ArgumentNullException` if `configure` is not null and attempts to invoke it with null options.

---

### `SlowQueryInterceptor CreateDefaultSlowQueryInterceptor()`

Creates a `SlowQueryInterceptor` with default configuration values. The default threshold is set to a value suitable for testing typical slow query scenarios without external tuning.

- **Parameters**: None
- **Return value**: A new `SlowQueryInterceptor` instance with default settings.
- **Exceptions**: None

---

### `IReadOnlyList<SlowQuerySample> GetSlowQuerySamples()`

Returns a read-only snapshot of all captured slow query samples since the last reset or test initialization. The list reflects queries intercepted by the active interceptor and is suitable for assertions in test code.

- **Parameters**: None
- **Return value**: An `IReadOnlyList<SlowQuerySample>` containing zero or more captured slow query events. Never returns null.
- **Exceptions**: None

---

### `int GetSlowQueryCount()`

Returns the total number of slow query samples captured so far. Useful for asserting the number of slow queries without inspecting their details.

- **Parameters**: None
- **Return value**: An integer representing the count of captured slow queries. Returns 0 if no queries have been captured.
- **Exceptions**: None

---
### `void ClearSlowQueries()`

Resets the internal collection of captured slow query samples. Useful between test cases to ensure isolation and prevent interference from previous runs.

- **Parameters**: None
- **Return value**: None
- **Exceptions**: None

---
### `SlowQuerySample? CaptureSlowQuery(DbCommand command, TimeSpan duration, DateTimeOffset capturedAt)`

Simulates the interception of a slow query by manually recording a `SlowQuerySample`. Useful for testing interceptor behavior or simulating edge cases without executing actual slow queries.

- **Parameters**:
  - `command`: The `DbCommand` that represents the slow query.
  - `duration`: The execution duration of the query.
  - `capturedAt`: The timestamp when the query was captured.
- **Return value**: The newly created `SlowQuerySample` instance, or `null` if the interceptor is not active or the sample is filtered out.
- **Exceptions**: May throw `ArgumentNullException` if `command` is null.

---

## Usage

### Example 1: Basic End-to-End Slow Query Test
