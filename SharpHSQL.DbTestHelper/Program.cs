using System;
using SharpHsql;

namespace SharpHSQL.DbTestHelper
{
    /// <summary>
    /// Helper executable for reliability tests. Can be killed mid-operation to simulate crashes.
    /// Usage: SharpHSQL.DbTestHelper.exe <command> <dbPath> [options]
    /// Commands:
    ///   create-and-write <dbPath> <rows> - Creates DB, writes N rows, exits WITHOUT shutdown
    ///   create-write-shutdown <dbPath> <rows> - Creates DB, writes N rows, proper SHUTDOWN
    ///   write-loop <dbPath> - Opens DB and writes in infinite loop until killed
    ///   read-verify <dbPath> <expectedRows> - Opens DB and verifies row count
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: SharpHSQL.DbTestHelper <command> <dbPath> [options]");
                return 1;
            }

            string command = args[0];
            string dbPath = args[1];

            try
            {
                switch (command.ToLower())
                {
                    case "create-and-write":
                        return CreateAndWrite(dbPath, args.Length > 2 ? int.Parse(args[2]) : 100, shutdown: false);
                    
                    case "create-write-shutdown":
                        return CreateAndWrite(dbPath, args.Length > 2 ? int.Parse(args[2]) : 100, shutdown: true);
                    
                    case "write-loop":
                        return WriteLoop(dbPath);
                    
                    case "read-verify":
                        return ReadVerify(dbPath, args.Length > 2 ? int.Parse(args[2]) : 0);
                    
                    case "create-table-only":
                        return CreateTableOnly(dbPath, shutdown: args.Length > 2 && args[2] == "shutdown");
                    
                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: {ex.Message}");
                Console.Error.WriteLine($"StackTrace: {ex.StackTrace}");
                return 2;
            }
        }

        static int CreateAndWrite(string dbPath, int rowCount, bool shutdown)
        {
            Console.WriteLine($"Creating database at: {dbPath}");
            var db = new Database(dbPath);
            var channel = db.Connect("sa", "");

            Console.WriteLine("Creating table...");
            var result = db.Execute("CREATE TABLE TestData (id INT PRIMARY KEY, value VARCHAR(100), timestamp TIMESTAMP)", channel);
            if (result.Error != null)
            {
                Console.Error.WriteLine($"CREATE TABLE error: {result.Error}");
                return 3;
            }

            Console.WriteLine($"Writing {rowCount} rows...");
            for (int i = 1; i <= rowCount; i++)
            {
                var sql = $"INSERT INTO TestData VALUES ({i}, 'Data_{i}', NOW())";
                result = db.Execute(sql, channel);
                if (result.Error != null)
                {
                    Console.Error.WriteLine($"INSERT error at row {i}: {result.Error}");
                    return 4;
                }
                
                // Progress indicator
                if (i % 10 == 0)
                {
                    Console.WriteLine($"  Wrote {i}/{rowCount} rows");
                }
            }

            Console.WriteLine($"Wrote {rowCount} rows successfully");

            if (shutdown)
            {
                Console.WriteLine("Executing SHUTDOWN...");
                db.Execute("SHUTDOWN", channel);
                Console.WriteLine("SHUTDOWN complete");
            }
            else
            {
                Console.WriteLine("Exiting WITHOUT shutdown (simulating crash)");
            }

            return 0;
        }

        static int WriteLoop(string dbPath)
        {
            Console.WriteLine($"Opening database at: {dbPath} for infinite write loop");
            var db = new Database(dbPath);
            var channel = db.Connect("sa", "");

            // Check if table exists, create if not
            var checkResult = db.Execute("SELECT COUNT(*) FROM TestData", channel);
            if (checkResult.Error != null)
            {
                Console.WriteLine("Creating table (didn't exist)...");
                db.Execute("CREATE TABLE TestData (id INT PRIMARY KEY, value VARCHAR(100), timestamp TIMESTAMP)", channel);
            }

            Console.WriteLine("Starting infinite write loop (kill this process to simulate crash)...");
            int counter = 1;
            while (true)
            {
                var sql = $"INSERT INTO TestData VALUES ({counter}, 'Loop_{counter}', NOW())";
                var result = db.Execute(sql, channel);
                
                if (result.Error != null)
                {
                    // If duplicate key, increment and try again
                    counter++;
                    continue;
                }

                if (counter % 10 == 0)
                {
                    Console.WriteLine($"  Wrote row {counter}");
                }

                counter++;
                System.Threading.Thread.Sleep(10); // Small delay to ensure we can kill it mid-write
            }
        }

        static int ReadVerify(string dbPath, int expectedRows)
        {
            Console.WriteLine($"Opening database at: {dbPath}");
            var db = new Database(dbPath);
            var channel = db.Connect("sa", "");

            Console.WriteLine("Reading row count...");
            var result = db.Execute("SELECT COUNT(*) FROM TestData", channel);
            
            if (result.Error != null)
            {
                Console.Error.WriteLine($"SELECT error: {result.Error}");
                return 3;
            }

            int actualRows = (int)result.Root.Data[0];
            Console.WriteLine($"Row count: {actualRows}");

            if (expectedRows > 0 && actualRows != expectedRows)
            {
                Console.Error.WriteLine($"MISMATCH: Expected {expectedRows} rows, found {actualRows}");
                return 5;
            }

            Console.WriteLine("Executing SHUTDOWN...");
            db.Execute("SHUTDOWN", channel);
            return 0;
        }

        static int CreateTableOnly(string dbPath, bool shutdown)
        {
            Console.WriteLine($"Creating database at: {dbPath}");
            var db = new Database(dbPath);
            var channel = db.Connect("sa", "");

            Console.WriteLine("Creating table...");
            var result = db.Execute("CREATE TABLE TestData (id INT PRIMARY KEY, value VARCHAR(100))", channel);
            if (result.Error != null)
            {
                Console.Error.WriteLine($"CREATE TABLE error: {result.Error}");
                return 3;
            }

            Console.WriteLine("Table created");

            if (shutdown)
            {
                Console.WriteLine("Executing SHUTDOWN...");
                db.Execute("SHUTDOWN", channel);
            }
            else
            {
                Console.WriteLine("Exiting WITHOUT shutdown");
            }

            return 0;
        }
    }
}
