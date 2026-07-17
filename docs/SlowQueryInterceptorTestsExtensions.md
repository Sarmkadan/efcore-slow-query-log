# SlowQueryInterceptorTestsExtensions

`SlowQueryInterceptorTestsExtensions` is a static helper class that provides convenient extension methods for creating and inspecting `SlowQueryInterceptor` instances in unit tests.  
It allows test code to quickly generate `SqliteCommand` objects, configure interceptors with custom thresholds or callbacks, capture simulated slow queries, and query the interceptor’s internal state.

## API

### `public static SqliteCommand CreateTestCommand(this string sql, params (string name, object value)[] parameters)`

Creates a `SqliteCommand` that can be used with the interceptor.  
- **Parameters**  
  - `sql`: The SQL statement to execute.  
  - `parameters`: Zero or more name/value tuples that are added as parameters to the command.  
- **Return value**  
  - A `SqliteCommand` instance with the supplied SQL and parameters.  
- **Throws**  
  - `ArgumentNullException` if `sql` is null.  
  - `ArgumentException` if any parameter name is null or empty.

### `public static SlowQueryInterceptor CreateTestInterceptor(this TimeSpan threshold, bool includeParameters = false)`

Creates a `SlowQueryInterceptor` that captures queries exceeding the specified `threshold`.  
- **Parameters**  
  - `threshold`: The minimum duration a query must exceed to be captured.  
  - `includeParameters`: If `true`, the captured `SlowQuerySample` will contain the command’s parameters.  
- **Return value**  
  - A new `SlowQueryInterceptor` instance.  
- **Throws**  
  - `ArgumentOutOfRangeException` if `threshold` is negative.

### `public static SlowQueryInterceptor CreateTestInterceptor(this TimeSpan threshold, Action<SlowQuerySample> onSlowQuery, bool includeParameters = false)`

Creates a `SlowQueryInterceptor` that captures queries exceeding the specified `threshold` and invokes the supplied callback for each captured query.  
- **Parameters**  
  - `threshold`: The minimum duration a query must exceed to be captured.  
  - `onSlowQuery`: Callback invoked with the captured `SlowQuerySample`.  
  - `includeParameters`: If `true`, the captured `SlowQuerySample` will contain the command’s parameters.  
- **Return value**  
  - A new `SlowQueryInterceptor` instance.  
- **Throws**  
  - `ArgumentNullException` if `onSlowQuery` is null.  
  - `ArgumentOutOfRangeException` if `threshold` is negative.

### `public static SlowQuerySample? CaptureSlowQuery(this SlowQueryInterceptor interceptor, string sql, TimeSpan executionTime, params (string name, object value)[] parameters)`

Simulates the execution of a query and captures it if it exceeds the interceptor’s threshold.  
- **Parameters**  
  - `interceptor`: The interceptor to use.  
  - `sql`: The SQL statement to simulate.  
  - `executionTime`: The duration the query is assumed to have taken.  
  - `parameters`: Optional parameters to attach to the command.  
- **Return value**  
  - The captured `SlowQuerySample` if the query was slow; otherwise `null`.  
- **Throws**  
  - `ArgumentNullException` if `interceptor` or `sql` is null.  
  - `ArgumentOutOfRangeException` if `executionTime` is negative.

### `public static IEnumerable<SlowQuerySample> GetCapturedSamples(this SlowQueryInterceptor interceptor)`

Retrieves all samples that have been captured by the interceptor.  
- **Parameters**  
  - `interceptor`: The interceptor to query.  
- **Return value**  
  - An `IEnumerable<SlowQuerySample>` containing all captured samples.  
- **Throws**  
  - `ArgumentNullException` if `interceptor` is null.

### `public static int GetSlowQueryCount(this SlowQueryInterceptor interceptor)`

Returns the number of slow queries captured by the interceptor.  
- **Parameters**  
  - `interceptor`: The interceptor to query.  
- **Return value**  
  - The count of captured slow queries.  
- **Throws**  
  - `ArgumentNullException` if `interceptor` is null.

### `public static SlowQueryInterceptor CreateAlwaysCapturingInterceptor(bool includeParameters = false)`

Creates a `SlowQueryInterceptor` that captures every query regardless of duration.  
- **Parameters**  
  - `includeParameters`: If `true`, the captured `SlowQuerySample` will contain the command’s parameters.  
- **Return value**  
  - A new `SlowQueryInterceptor` instance that captures all queries.  
- **Throws**  
  - None.

## Usage

