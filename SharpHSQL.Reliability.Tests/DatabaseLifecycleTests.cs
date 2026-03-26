using System;
using System.IO;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Reliability.Tests
{
    /// <summary>
    /// Baseline database lifecycle tests.
    /// Tests normal open/close/reopen scenarios to establish baseline behavior.
    /// Some of these tests may FAIL, revealing corruption issues.
    /// </summary>
    public class DatabaseLifecycleTests : IDisposable
    {
        private readonly string _testDbDir;

        public DatabaseLifecycleTests()
        {
            _testDbDir = Path.Combine(Path.GetTempPath(), $"SharpHSQL_Test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDbDir);
        }

        /// <summary>
        /// BASELINE: This test SHOULD PASS - represents correct shutdown behavior.
        /// Creates DB, writes data, proper SHUTDOWN, reopens, verifies data intact.
        /// </summary>
        [Fact]
        public void Database_CreateWriteShutdownReopen_ShouldMaintainData()
        {
            var dbPath = Path.Combine(_testDbDir, "test1");

            // Phase 1: Create and write with proper shutdown
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                db.Execute("CREATE TABLE Data (id INT PRIMARY KEY, value VARCHAR(50))", channel);
                db.Execute("INSERT INTO Data VALUES (1, 'Test1')", channel);
                db.Execute("INSERT INTO Data VALUES (2, 'Test2')", channel);
                db.Execute("SHUTDOWN", channel);
            }

            // Phase 2: Reopen and verify
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                var result = db.Execute("SELECT COUNT(*) FROM Data", channel);
                Assert.Null(result.Error);
                Assert.Equal(2, result.Root.Data[0]);
                
                db.Execute("SHUTDOWN", channel);
            }
        }

        /// <summary>
        /// EXPECTED TO FAIL: Database not properly shut down.
        /// Creates DB, writes data, exits WITHOUT shutdown, attempts to reopen.
        /// Expected: Should recover gracefully
        /// Actual: Will likely FAIL with corruption or exception
        /// </summary>
        [Fact]
        public void Database_CreateWriteNoShutdownReopen_ShouldRecover()
        {
            var dbPath = Path.Combine(_testDbDir, "test2");

            // Phase 1: Create and write WITHOUT shutdown (simulating crash)
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                db.Execute("CREATE TABLE Data (id INT PRIMARY KEY, value VARCHAR(50))", channel);
                db.Execute("INSERT INTO Data VALUES (1, 'Test1')", channel);
                db.Execute("INSERT INTO Data VALUES (2, 'Test2')", channel);
                // NO SHUTDOWN - simulating process crash
                // Force disposal to release resources (simulates process termination)
                db.Dispose();
            }

            // Phase 2: Attempt to reopen
            // EXPECTED TO FAIL HERE
            Exception caughtException = null;
            try
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                var result = db.Execute("SELECT COUNT(*) FROM Data", channel);
                
                // If we got here, check if data is intact
                Assert.Null(result.Error); // May FAIL if result has error
                Assert.Equal(2, result.Root.Data[0]); // May FAIL if data corrupted
                
                db.Execute("SHUTDOWN", channel);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Database should not throw exceptions on open after improper shutdown
            Assert.Null(caughtException); // WILL LIKELY FAIL
        }

        /// <summary>
        /// EXPECTED TO FAIL: Multiple open/close cycles without shutdown.
        /// Tests if database can survive multiple improper terminations.
        /// </summary>
        [Fact]
        public void Database_MultipleOpenCloseWithoutShutdown_ShouldNotCorrupt()
        {
            var dbPath = Path.Combine(_testDbDir, "test3");

            // Create initial database
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                db.Execute("CREATE TABLE Data (id INT PRIMARY KEY, value VARCHAR(50))", channel);
                db.Execute("SHUTDOWN", channel);
            }

            // Cycle 1: Write without shutdown
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                db.Execute("INSERT INTO Data VALUES (1, 'Cycle1')", channel);
                // NO SHUTDOWN
                db.Dispose(); // Simulates process termination
            }

            // Cycle 2: Write without shutdown
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                db.Execute("INSERT INTO Data VALUES (2, 'Cycle2')", channel);
                // NO SHUTDOWN
                db.Dispose(); // Simulates process termination
            }

            // Final: Attempt to read
            Exception ex = Xunit.Record.Exception(() => {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                var result = db.Execute("SELECT COUNT(*) FROM Data", channel);
                Assert.Null(result.Error);
                db.Execute("SHUTDOWN", channel);
            });

            Assert.Null(ex); // WILL LIKELY FAIL - may throw exception
        }

        /// <summary>
        /// BASELINE: SHUTDOWN COMPACT should properly close database.
        /// Tests if SHUTDOWN COMPACT provides better recovery than regular SHUTDOWN.
        /// </summary>
        [Fact]
        public void Database_ShutdownCompact_ShouldReopenCleanly()
        {
            var dbPath = Path.Combine(_testDbDir, "test4");

            // Create with SHUTDOWN COMPACT
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                db.Execute("CREATE TABLE Data (id INT PRIMARY KEY, value VARCHAR(50))", channel);
                db.Execute("INSERT INTO Data VALUES (1, 'Test1')", channel);
                db.Execute("SHUTDOWN COMPACT", channel);
            }

            // Reopen
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                var result = db.Execute("SELECT COUNT(*) FROM Data", channel);
                Assert.Null(result.Error);
                Assert.Equal(1, result.Root.Data[0]);
                
                db.Execute("SHUTDOWN", channel);
            }
        }

        /// <summary>
        /// EXPECTED TO FAIL: Write many rows, exit without shutdown.
        /// Tests if larger datasets increase corruption risk.
        /// </summary>
        [Fact]
        public void Database_LargeDatasetNoShutdown_ShouldRecover()
        {
            var dbPath = Path.Combine(_testDbDir, "test5");
            const int rowCount = 1000;

            // Write large dataset without shutdown
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                db.Execute("CREATE TABLE Data (id INT PRIMARY KEY, value VARCHAR(50))", channel);
                
                for (int i = 1; i <= rowCount; i++)
                {
                    db.Execute($"INSERT INTO Data VALUES ({i}, 'Data_{i}')", channel);
                }
                // NO SHUTDOWN
                db.Dispose(); // Simulates process termination
            }

            // Attempt to reopen and verify
            Exception ex = Xunit.Record.Exception(() => {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                var result = db.Execute("SELECT COUNT(*) FROM Data", channel);
                Assert.Null(result.Error);
                // May have lost some rows, but should have most
                Assert.True((int)result.Root.Data[0] >= rowCount * 0.9); // At least 90% of data
                
                db.Execute("SHUTDOWN", channel);
            });

            Assert.Null(ex); // WILL LIKELY FAIL
        }

        /// <summary>
        /// EXPECTED TO FAIL: Transaction not committed, no shutdown.
        /// Tests if uncommitted transactions cause corruption.
        /// </summary>
        [Fact]
        public void Database_UncommittedTransactionNoShutdown_ShouldRollbackOnReopen()
        {
            var dbPath = Path.Combine(_testDbDir, "test6");

            // Start transaction, don't commit, don't shutdown
            {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                db.Execute("CREATE TABLE Data (id INT PRIMARY KEY, value VARCHAR(50))", channel);
                db.Execute("SET AUTOCOMMIT FALSE", channel);
                db.Execute("INSERT INTO Data VALUES (1, 'Uncommitted')", channel);
                // NO COMMIT, NO SHUTDOWN
                db.Dispose(); // Simulates process termination
            }

            // Reopen - transaction should be rolled back
            Exception ex = Xunit.Record.Exception(() => {
                var db = new Database(dbPath);
                var channel = db.Connect("sa", "");
                
                var result = db.Execute("SELECT COUNT(*) FROM Data", channel);
                Assert.Null(result.Error);
                Assert.Equal(0, result.Root.Data[0]); // Uncommitted data should not exist
                
                db.Execute("SHUTDOWN", channel);
            });

            Assert.Null(ex); // WILL LIKELY FAIL - may throw exception or have corrupt data
        }

        public void Dispose()
        {
            // Clean up test databases
            try
            {
                if (Directory.Exists(_testDbDir))
                {
                    Directory.Delete(_testDbDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
