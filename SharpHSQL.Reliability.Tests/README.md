# SharpHSQL Reliability Tests

## Overview

This test project focuses on **database reliability and corruption scenarios** rather than functional correctness. The goal is to reproduce and document database corruption issues that occur when processes crash or databases are not properly shut down.

**Key Difference from Functional Tests:**
- Functional tests verify SQL commands work correctly
- Reliability tests verify the database can **survive crashes and recover from corruption**

## Purpose

SharpHSQL databases are fragile and tend to corrupt if a process terminates abnormally (crash, kill signal, power loss). These tests:

1. **Reproduce corruption scenarios** - Tests are written correctly but FAIL due to bugs
2. **Document failure modes** - Each failing test documents what goes wrong
3. **Serve as regression tests** - Once bugs are fixed, tests should pass
4. **Guide fixes** - Test failures point to exact issues that need resolution

## Project Structure

```
SharpHSQL.Reliability.Tests/
├── DatabaseLifecycleTests.cs           # Baseline open/close/reopen scenarios
├── ProcessCrashSimulationTests.cs      # Kill subprocess mid-operation (TODO)
├── FileCorruptionTests.cs              # Manually corrupt files (TODO)
├── ConcurrentAccessTests.cs            # Multiple process access (TODO)
├── RELIABILITY_TEST_RESULTS.md         # Detailed failure analysis
└── README.md                           # This file

SharpHSQL.DbTestHelper/
└── Program.cs                          # Helper executable for subprocess crash simulation
```

## Running the Tests

### Run All Reliability Tests
```bash
dotnet test SharpHSQL.Reliability.Tests/SharpHSQL.Reliability.Tests.csproj
```

### Run Specific Test
```bash
dotnet test SharpHSQL.Reliability.Tests/SharpHSQL.Reliability.Tests.csproj --filter "FullyQualifiedName~Database_CreateWriteNoShutdownReopen_ShouldRecover"
```

### View Detailed Failures
```bash
dotnet test SharpHSQL.Reliability.Tests/SharpHSQL.Reliability.Tests.csproj -v normal
```

## Current Test Results

**As of 2026-03-25:**
- ✅ Passed: 2/6 (33%)
- ❌ Failed: 4/6 (67%)

**Critical Issue Identified:**
- **Lock file not released after crash** - Database becomes permanently locked

See [`RELIABILITY_TEST_RESULTS.md`](./RELIABILITY_TEST_RESULTS.md) for detailed analysis.

## Test Categories

### 1. DatabaseLifecycleTests ✅ Implemented

Tests normal database lifecycle without subprocess isolation:

| Test | Status | Description |
|------|--------|-------------|
| `Database_CreateWriteShutdownReopen_ShouldMaintainData` | ✅ PASS | Baseline - proper SHUTDOWN |
| `Database_CreateWriteNoShutdownReopen_ShouldRecover` | ❌ FAIL | No SHUTDOWN → lock file persists |
| `Database_MultipleOpenCloseWithoutShutdown_ShouldNotCorrupt` | ❌ FAIL | Multiple improper closures |
| `Database_ShutdownCompact_ShouldReopenCleanly` | ✅ PASS | SHUTDOWN COMPACT variant |
| `Database_LargeDatasetNoShutdown_ShouldRecover` | ❌ FAIL | 1000 rows without SHUTDOWN |
| `Database_UncommittedTransactionNoShutdown_ShouldRollbackOnReopen` | ❌ FAIL | Uncommitted transaction |

### 2. ProcessCrashSimulationTests ⏳ TODO

Tests using `SharpHSQL.DbTestHelper` subprocess:

- `Process_KilledDuringWrite_DatabaseShouldRecover` - Kill process mid-INSERT
- `Process_KilledDuringCommit_TransactionShouldBeAtomic` - Kill during COMMIT
- `Process_KilledAfterCreateTable_ShouldReopenWithTable` - Kill after DDL

### 3. FileCorruptionTests ⏳ TODO

Tests manual file corruption scenarios:

- `Database_WithMissingLogFile_ShouldRecoverFromScript`
- `Database_WithTruncatedDataFile_ShouldDetectCorruption`
- `Database_WithCorruptScriptFile_ShouldReportError`

### 4. ConcurrentAccessTests ⏳ TODO

Tests multiple process scenarios:

- `TwoProcesses_OpeningSameDatabase_SecondShouldFail`
- `Process_WritingWhileAnotherReads_ShouldNotCorrupt`

## Using SharpHSQL.DbTestHelper

The helper executable simulates database operations in a separate process that can be killed:

```bash
# Create DB and write without shutdown (simulates crash)
dotnet run --project SharpHSQL.DbTestHelper create-and-write ./testdb 100

# Create DB and write with proper shutdown
dotnet run --project SharpHSQL.DbTestHelper create-write-shutdown ./testdb 100

# Infinite write loop (kill to simulate crash)
dotnet run --project SharpHSQL.DbTestHelper write-loop ./testdb &
# Kill it: kill -9 $!

# Verify database integrity
dotnet run --project SharpHSQL.DbTestHelper read-verify ./testdb 100
```

## Expected Behavior vs. Actual Behavior

### Expected (How it SHOULD work):

1. ✅ Database opened with SHUTDOWN → Reopens successfully
2. ✅ Database without SHUTDOWN → Auto-detects stale lock, recovers from WAL
3. ✅ Process crash → Next open recovers committed transactions
4. ✅ Corrupt file → Returns clear error, doesn't crash

### Actual (Current behavior - BUGS):

1. ✅ Database opened with SHUTDOWN → Works correctly
2. ❌ Database without SHUTDOWN → **Permanently locked** (`.lck` file persists)
3. ❌ Process crash → **Database unusable** (lock file not cleaned up)
4. ❌ Corrupt file → Throws unclear exceptions

## Known Issues

### 🔴 Critical: Lock File Not Released (Bug #1)

**Symptoms:**
- Database becomes permanently locked after process crash
- Error: `08001 The database is already in use by another process`
- Workaround: Manually delete `.lck` file

**Files Affected:**
- `SharpHSQL/Log.cs:222` - Lock file creation
- `SharpHSQL/Database.cs:99` - Database constructor

**Fix Needed:**
- Implement stale lock detection
- Store PID in lock file
- Check if PID is still alive before throwing error
- Auto-remove stale locks

## Contributing New Tests

When adding reliability tests:

1. **Write test expecting correct behavior** (e.g., "should recover")
2. **Run test - it will likely FAIL** (reproducing the bug)
3. **Document the failure** in test comments
4. **Update RELIABILITY_TEST_RESULTS.md** with findings

Example test structure:

```csharp
/// <summary>
/// EXPECTED TO FAIL: Description of scenario
/// Expected: What should happen
/// Actual: What actually happens (bug)
/// </summary>
[Fact]
public void MyReliabilityTest()
{
    // Arrange: Setup scenario
    
    // Act: Perform operation (that causes corruption)
    
    // Assert: Expect recovery/graceful handling
    // This assertion will FAIL if bug exists
    Assert.Null(exception);
}
```

## Next Steps

1. ✅ **Phase 1 Complete**: Basic lifecycle tests implemented and documented
2. ⏳ **Phase 2**: Implement ProcessCrashSimulationTests using subprocess helper
3. ⏳ **Phase 3**: Implement FileCorruptionTests
4. ⏳ **Phase 4**: Implement ConcurrentAccessTests
5. ⏳ **Phase 5**: Fix identified bugs
6. ⏳ **Phase 6**: Verify all tests pass after fixes

## Resources

- [RELIABILITY_TEST_RESULTS.md](./RELIABILITY_TEST_RESULTS.md) - Detailed failure analysis
- [SharpHSQL Source](../SharpHSQL/) - Main library
- [xUnit Documentation](https://xunit.net/) - Test framework

---

**Remember:** These tests are SUPPOSED to fail. They document bugs that need to be fixed.
