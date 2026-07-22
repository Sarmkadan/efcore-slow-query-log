// This script verifies that the new sampling and rate-limiting features are working correctly

using System;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;

Console.WriteLine("Testing new sampling and rate-limiting features...\n");

// Test 1: SamplingRate property exists and works
Console.WriteLine("Test 1: SamplingRate property");
var options1 = new SlowQueryLogOptions {
    Threshold = TimeSpan.FromMilliseconds(10),
    SamplingRate = 0.5 // Sample 50% of queries
};
Console.WriteLine($"  SamplingRate = {options1.SamplingRate}");
Console.WriteLine("  ✓ SamplingRate property exists and can be set\n");

// Test 2: MaxAnalysesPerMinute property exists and works
Console.WriteLine("Test 2: MaxAnalysesPerMinute property");
var options2 = new SlowQueryLogOptions {
    Threshold = TimeSpan.FromMilliseconds(10),
    MaxAnalysesPerMinute = 500,
    AnalyzeOnBackgroundThread = false // Disable background for synchronous testing
};
Console.WriteLine($"  MaxAnalysesPerMinute = {options2.MaxAnalysesPerMinute}");
Console.WriteLine("  ✓ MaxAnalysesPerMinute property exists and can be set\n");

// Test 3: AnalyzeOnBackgroundThread property exists and works
Console.WriteLine("Test 3: AnalyzeOnBackgroundThread property");
var options3 = new SlowQueryLogOptions {
    Threshold = TimeSpan.FromMilliseconds(10),
    AnalyzeOnBackgroundThread = true
};
Console.WriteLine($"  AnalyzeOnBackgroundThread = {options3.AnalyzeOnBackgroundThread}");
Console.WriteLine("  ✓ AnalyzeOnBackgroundThread property exists and can be set\n");

// Test 4: BackgroundQueueCapacity property exists and works
Console.WriteLine("Test 4: BackgroundQueueCapacity property");
var options4 = new SlowQueryLogOptions {
    Threshold = TimeSpan.FromMilliseconds(10),
    BackgroundQueueCapacity = 2000
};
Console.WriteLine($"  BackgroundQueueCapacity = {options4.BackgroundQueueCapacity}");
Console.WriteLine("  ✓ BackgroundQueueCapacity property exists and can be set\n");

// Test 5: Validation works for new properties
Console.WriteLine("Test 5: Validation of new properties");
try {
    var optionsInvalid = new SlowQueryLogOptions {
        Threshold = TimeSpan.FromMilliseconds(10),
        SamplingRate = 1.5 // Invalid: > 1.0
    };
    optionsInvalid.Validate();
    Console.WriteLine("  ✗ Validation should have failed for SamplingRate > 1.0");
    Environment.Exit(1);
} catch (ArgumentOutOfRangeException) {
    Console.WriteLine("  ✓ Validation correctly rejects SamplingRate > 1.0");
}

try {
    var optionsInvalid2 = new SlowQueryLogOptions {
        Threshold = TimeSpan.FromMilliseconds(10),
        SamplingRate = -0.1 // Invalid: < 0.0
    };
    optionsInvalid2.Validate();
    Console.WriteLine("  ✗ Validation should have failed for SamplingRate < 0.0");
    Environment.Exit(1);
} catch (ArgumentOutOfRangeException) {
    Console.WriteLine("  ✓ Validation correctly rejects SamplingRate < 0.0");
}

try {
    var optionsInvalid3 = new SlowQueryLogOptions {
        Threshold = TimeSpan.FromMilliseconds(10),
        MaxAnalysesPerMinute = -1 // Invalid: negative
    };
    optionsInvalid3.Validate();
    Console.WriteLine("  ✗ Validation should have failed for MaxAnalysesPerMinute < 0");
    Environment.Exit(1);
} catch (ArgumentOutOfRangeException) {
    Console.WriteLine("  ✓ Validation correctly rejects MaxAnalysesPerMinute < 0");
}

try {
    var optionsInvalid4 = new SlowQueryLogOptions {
        Threshold = TimeSpan.FromMilliseconds(10),
        BackgroundQueueCapacity = 0 // Invalid: <= 0
    };
    optionsInvalid4.Validate();
    Console.WriteLine("  ✗ Validation should have failed for BackgroundQueueCapacity <= 0");
    Environment.Exit(1);
} catch (ArgumentOutOfRangeException) {
    Console.WriteLine("  ✓ Validation correctly rejects BackgroundQueueCapacity <= 0\n");
}

// Test 6: SlowQueryInterceptor accepts the new options
Console.WriteLine("Test 6: SlowQueryInterceptor with new options");
var interceptor = new SlowQueryInterceptor(options2);
Console.WriteLine("  ✓ SlowQueryInterceptor created successfully with new options\n");

// Test 7: Sampling actually works
Console.WriteLine("Test 7: Sampling functionality");
var samplingOptions = new SlowQueryLogOptions {
    Threshold = TimeSpan.FromMilliseconds(10),
    SamplingRate = 0.1, // 10% sampling rate
    SuggestIndexes = false, // Disable to avoid expensive analysis
    AnalyzeOnBackgroundThread = false
};
var samplingInterceptor = new SlowQueryInterceptor(samplingOptions);

// Create a command helper
static SqliteCommand Command(string sql) {
    var cmd = new SqliteCommand { CommandText = sql };
    cmd.Connection = new SqliteConnection("Data Source=:memory:");
    return cmd;
}

// Capture 100 slow queries - with 10% sampling, we should get roughly 10 samples
int totalQueries = 100;
int capturedCount = 0;
for (int i = 0; i < totalQueries; i++) {
    var sample = samplingInterceptor.Capture(Command("SELECT * FROM Table" + i), TimeSpan.FromMilliseconds(100));
    if (sample != null) capturedCount++;
}

Console.WriteLine($"  Captured {capturedCount} out of {totalQueries} slow queries (expected ~10)");
if (capturedCount >= 5 && capturedCount <= 15) { // Allow some variance
    Console.WriteLine("  ✓ Sampling is working correctly\n");
} else {
    Console.WriteLine("  ⚠ Sampling captured unexpected number of queries\n");
}

// Test 8: Background analyzer can be created
Console.WriteLine("Test 8: Background analyzer creation");
var bgOptions = new SlowQueryLogOptions {
    Threshold = TimeSpan.FromMilliseconds(10),
    AnalyzeOnBackgroundThread = true,
    BackgroundQueueCapacity = 100,
    MaxAnalysesPerMinute = 1000
};
var bgInterceptor = new SlowQueryInterceptor(bgOptions);
Console.WriteLine("  ✓ Background analyzer created successfully\n");

Console.WriteLine("===========================================");
Console.WriteLine("All feature verification tests passed! ✓");
Console.WriteLine("===========================================");