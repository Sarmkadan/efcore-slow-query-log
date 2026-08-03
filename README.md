## SlowQueryRankingFingerprintTests

The `SlowQueryRankingFingerprintTests` class contains methods for testing slow query ranking fingerprints. It provides methods to get fingerprints groups by SQL and computes statistics, as well as methods to get fingerprints by total duration, P95 duration, and max duration. For example:
```csharp
public void GetFingerprints_groups_by_sql_and_computes_statistics
public void GetFingerprintsByTotalDuration_orders_by_total_duration
public void GetFingerprintsByP95Duration_orders_by_p95_duration
public void GetFingerprintsByMaxDuration_orders_by_max_duration
public void P95_calculation_works_correctly
```
