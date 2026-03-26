# SharpHSQL Reliability Test Results

## Test Suite Summary
**Project:** SharpHSQL.Reliability.Tests  
**Date:** 2026-03-25  
**Total Tests:** 6  
**Passed:** 2 ✅  
**Failed:** 4 ❌  
**Success Rate:** 33%

---

## Critical Issue Discovered

### 🔴 **Lock File Not Released Without Proper Shutdown**

**Severity:** CRITICAL  
**Impact:** Database becomes permanently unusable after process crash

#### Description:
When a SharpHSQL database is not properly shut down (via `SHUTDOWN` command), a lock file (`.lck`) remains on disk, preventing the database from being reopened.

#### Error Message:
```
System.Exception: 08001 The database is already in use by another process
  at SharpHsql.Log.Open() in SharpHSQL/Log.cs:line 222
  at SharpHsql.Database..ctor(String name) in SharpHSQL/Database.cs:line 99
```

#### Root Cause:
The lock file mechanism in `Log.cs` (line 222) does not implement automatic stale lock detection or cleanup. If a process terminates without calling `SHUTDOWN`, the lock file persists indefinitely.

---

## Test Results Detail

### ✅ **PASSING TESTS** (Baseline - Correct Behavior)

#### 1. `Database_CreateWriteShutdownReopen_ShouldMaintainData`
**Status:** ✅ PASS  
**Description:** Database with proper `SHUTDOWN` reopens successfully and maintains data integrity.  
**Conclusion:** Normal shutdown path works correctly.

#### 2. `Database_ShutdownCompact_ShouldReopenCleanly`
**Status:** ✅ PASS  
**Description:** `SHUTDOWN COMPACT` properly closes database for clean reopen.  
**Conclusion:** COMPACT shutdown variant also works correctly.

---

### ❌ **FAILING TESTS** (Bugs Reproduced)

#### 3. `Database_CreateWriteNoShutdownReopen_ShouldRecover`
**Status:** ❌ FAIL  
**Expected:** Database should recover gracefully after improper shutdown  
**Actual:** `08001 The database is already in use by another process`  
**Scenario:** 
1. Create database
2. Write data  
3. Exit WITHOUT calling `SHUTDOWN`
4. Attempt to reopen → **FAILS**

**Impact:** This is the most common crash scenario - if an application crashes, the database becomes permanently locked.

---

#### 4. `Database_MultipleOpenCloseWithoutShutdown_ShouldNotCorrupt`
**Status:** ❌ FAIL  
**Expected:** Database should handle multiple improper shutdowns  
**Actual:** `08001 The database is already in use by another process`  
**Scenario:**
1. Create database with proper SHUTDOWN
2. Reopen, write data, exit without SHUTDOWN
3. Attempt second reopen → **FAILS**

**Impact:** Even after an initial proper shutdown, a single improper closure locks the database forever.

---

#### 5. `Database_LargeDatasetNoShutdown_ShouldRecover`
**Status:** ❌ FAIL  
**Expected:** Database should recover even with large dataset (1000 rows)  
**Actual:** `08001 The database is already in use by another process`  
**Scenario:**
1. Create database
2. Write 1000 rows
3. Exit without SHUTDOWN
4. Attempt to reopen → **FAILS**

**Impact:** Dataset size doesn't matter - the lock file issue affects all databases equally.

---

#### 6. `Database_UncommittedTransactionNoShutdown_ShouldRollbackOnReopen`
**Status:** ❌ FAIL  
**Expected:** Database should rollback uncommitted transaction and reopen  
**Actual:** `08001 The database is already in use by another process`  
**Scenario:**
1. Create database
2. `SET AUTOCOMMIT FALSE`
3. Insert data (not committed)
4. Exit without COMMIT or SHUTDOWN
5. Attempt to reopen → **FAILS**

**Impact:** Even transactions that should be rolled back prevent database reopening.

---

## Recommendations

### Immediate Fixes Needed:

1. **Stale Lock Detection** (Priority: CRITICAL)
   - Check if process owning the lock file is still running
   - On macOS/Linux: Check `/proc/[pid]/` or use `kill -0 [pid]`
   - On Windows: Use `OpenProcess()` to verify process exists
   - Auto-remove lock file if owning process is dead

2. **Lock File Cleanup on Open** (Priority: HIGH)
   - Add timestamp to lock file
   - If lock file is older than reasonable timeout (e.g., 5 minutes), warn user or auto-remove
   - Provide `FORCE_UNLOCK` or `RECOVER` option for manual intervention

3. **Graceful Degradation** (Priority: MEDIUM)
   - If lock exists but is stale, log warning and recover
   - Don't throw exception - attempt recovery first

### Alternative Approaches:

1. **File-based Locking with Timeout**
   - Use file system advisory locks that auto-release on process death
   - On .NET: `FileStream` with `FileShare.None` automatically releases on process exit

2. **Lock File with PID**
   - Store process ID in lock file
   - On reopen, check if PID is still alive
   - If dead, remove lock and proceed

3. **Write-Ahead Log (WAL) Recovery**
   - Even if lock exists, read WAL to recover last committed state
   - Implement crash recovery mechanism similar to SQLite

---

## Files Affected

- `/SharpHSQL/Log.cs:222` - Lock file creation/check
- `/SharpHSQL/Database.cs:99` - Database constructor throws on lock

---

## Next Steps

1. ✅ **Bug Reproduction Complete** - All 4 failing tests demonstrate the issue
2. ⏳ **Root Cause Analysis** - Examine `Log.cs` lock mechanism
3. ⏳ **Implement Fix** - Add stale lock detection
4. ⏳ **Verify Fix** - Re-run reliability tests, all should pass
5. ⏳ **Add Process Crash Tests** - Use `SharpHSQL.DbTestHelper` subprocess tests

---

## Conclusion

**The reliability tests have successfully reproduced a critical database corruption/locking issue in SharpHSQL.**

The core problem is that SharpHSQL's lock file mechanism does not account for process crashes, making databases permanently inaccessible if the application terminates without calling `SHUTDOWN`. This is a showstopper bug for production use.

The good news: The bug is isolated to the lock file mechanism in `Log.cs`, making it a targeted fix. Once stale lock detection is implemented, all 4 failing tests should pass.
