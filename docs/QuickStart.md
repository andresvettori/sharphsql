# SharpHSQL Quick Start Guide

Get up and running with SharpHSQL in just 5 minutes! This guide covers the essentials for both ADO.NET and Native API approaches.

## Table of Contents

- [Installation](#installation)
- [ADO.NET Quick Start](#adonet-quick-start)
- [Native API Quick Start](#native-api-quick-start)
- [Connection Strings](#connection-strings)
- [Basic CRUD Operations](#basic-crud-operations)
- [Working with Transactions](#working-with-transactions)
- [Common Patterns](#common-patterns)

## Installation

Install SharpHSQL via NuGet:

```bash
dotnet add package SharpHsql
```

Or using Package Manager Console:

```powershell
Install-Package SharpHsql
```

## ADO.NET Quick Start

The ADO.NET provider is the recommended approach for most applications as it follows standard .NET database patterns.

### Step 1: Add Using Statements

```csharp
using System.Data;
using System.Data.Hsql;
```

### Step 2: Create and Open Connection

```csharp
// In-memory database
var connectionString = "Initial Catalog=.;User Id=sa;Pwd=";

// File-based database (persists to disk)
// var connectionString = "Initial Catalog=./mydb;User Id=sa;Pwd=";

using var connection = new SharpHsqlConnection(connectionString);
connection.Open();
```

### Step 3: Create a Table

```csharp
using var createCmd = connection.CreateCommand();
createCmd.CommandText = @"
    CREATE TABLE Customers (
        Id INT PRIMARY KEY,
        Name VARCHAR(100),
        Email VARCHAR(100),
        Balance DECIMAL(10,2),
        Active BIT,
        CreatedDate DATE
    )";
createCmd.ExecuteNonQuery();
```

### Step 4: Insert Data

```csharp
using var insertCmd = connection.CreateCommand();
insertCmd.CommandText = "INSERT INTO Customers VALUES (@id, @name, @email, @balance, @active, @created)";

// Add parameters
insertCmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1));
insertCmd.Parameters.Add(new SharpHsqlParameter("@name", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "John Smith"));
insertCmd.Parameters.Add(new SharpHsqlParameter("@email", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "john@example.com"));
insertCmd.Parameters.Add(new SharpHsqlParameter("@balance", DbType.Decimal, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1500.50m));
insertCmd.Parameters.Add(new SharpHsqlParameter("@active", DbType.Boolean, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, true));
insertCmd.Parameters.Add(new SharpHsqlParameter("@created", DbType.DateTime, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, DateTime.Now));

insertCmd.ExecuteNonQuery();
```

### Step 5: Query Data

```csharp
using var selectCmd = connection.CreateCommand();
selectCmd.CommandText = "SELECT * FROM Customers WHERE Active = TRUE";

using var reader = selectCmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"ID: {reader.GetInt32(0)}");
    Console.WriteLine($"Name: {reader.GetString(1)}");
    Console.WriteLine($"Email: {reader.GetString(2)}");
    Console.WriteLine($"Balance: {reader.GetDecimal(3)}");
    Console.WriteLine($"Active: {reader.GetBoolean(4)}");
    Console.WriteLine($"Created: {reader.GetDateTime(5)}");
    Console.WriteLine("---");
}
```

## Native API Quick Start

The Native API provides direct access to SharpHSQL's internal database engine for advanced scenarios.

### Step 1: Add Using Statement

```csharp
using SharpHsql;
```

### Step 2: Create Database and Connect

```csharp
// In-memory database
var database = new Database(".");

// File-based database
// var database = new Database("./mydb");

var channel = database.Connect("sa", "");
```

### Step 3: Create a Table

```csharp
var sql = @"CREATE TABLE Products (
    Id INT PRIMARY KEY,
    Name VARCHAR(100),
    Price DECIMAL(10,2),
    Stock INT
)";

var result = database.Execute(sql, channel);

// Check for errors
if (result.Error != null && result.Error != string.Empty)
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### Step 4: Insert Data

```csharp
var insertSql = "INSERT INTO Products VALUES (1, 'Widget', 29.99, 100)";
database.Execute(insertSql, channel);

insertSql = "INSERT INTO Products VALUES (2, 'Gadget', 49.99, 50)";
database.Execute(insertSql, channel);
```

### Step 5: Query Data

```csharp
var selectSql = "SELECT * FROM Products WHERE Stock > 0";
var result = database.Execute(selectSql, channel);

// Process results
var record = result.Root;
while (record != null)
{
    var row = record.Data;
    Console.WriteLine($"ID: {row[0]}, Name: {row[1]}, Price: {row[2]}, Stock: {row[3]}");
    record = record.Next;
}
```

### Step 6: Disconnect

```csharp
database.Execute("DISCONNECT", channel);
```

## Connection Strings

### In-Memory Database
Data is stored in memory and lost when the application closes.

```csharp
"Initial Catalog=.;User Id=sa;Pwd="
```

### File-Based Database
Data persists to disk in the specified directory.

```csharp
"Initial Catalog=./mydata;User Id=sa;Pwd="
"Initial Catalog=C:\\Data\\mydb;User Id=sa;Pwd="
```

### Default Credentials
- **Username**: `sa` (system administrator)
- **Password**: (empty string)

## Basic CRUD Operations

### Create (INSERT)

```csharp
// Simple insert
cmd.CommandText = "INSERT INTO Users VALUES (1, 'Alice', 'alice@example.com')";
cmd.ExecuteNonQuery();

// Insert with parameters (recommended)
cmd.CommandText = "INSERT INTO Users VALUES (@id, @name, @email)";
cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 2));
cmd.Parameters.Add(new SharpHsqlParameter("@name", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "Bob"));
cmd.Parameters.Add(new SharpHsqlParameter("@email", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "bob@example.com"));
cmd.ExecuteNonQuery();

// Insert from SELECT
cmd.CommandText = "INSERT INTO UserBackup SELECT * FROM Users WHERE Active = TRUE";
cmd.ExecuteNonQuery();
```

### Read (SELECT)

```csharp
// Simple select
cmd.CommandText = "SELECT * FROM Users";
using var reader = cmd.ExecuteReader();

// Select with WHERE
cmd.CommandText = "SELECT Name, Email FROM Users WHERE Id = @id";
cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1));

// Select with JOIN
cmd.CommandText = @"
    SELECT u.Name, o.OrderDate, o.Total
    FROM Users u
    INNER JOIN Orders o ON u.Id = o.UserId
    WHERE o.Total > 100";

// Select with aggregate
cmd.CommandText = "SELECT COUNT(*) FROM Users";
int count = (int)cmd.ExecuteScalar();
```

### Update (UPDATE)

```csharp
// Simple update
cmd.CommandText = "UPDATE Users SET Email = 'newemail@example.com' WHERE Id = 1";
int rowsAffected = cmd.ExecuteNonQuery();

// Update with parameters
cmd.CommandText = "UPDATE Users SET Name = @name, Email = @email WHERE Id = @id";
cmd.Parameters.Add(new SharpHsqlParameter("@name", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "Alice Updated"));
cmd.Parameters.Add(new SharpHsqlParameter("@email", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "alice.new@example.com"));
cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1));
cmd.ExecuteNonQuery();

// Update multiple rows
cmd.CommandText = "UPDATE Products SET Stock = Stock + 10 WHERE Stock < 20";
cmd.ExecuteNonQuery();
```

### Delete (DELETE)

```csharp
// Delete specific row
cmd.CommandText = "DELETE FROM Users WHERE Id = @id";
cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1));
int deleted = cmd.ExecuteNonQuery();

// Delete with condition
cmd.CommandText = "DELETE FROM Users WHERE Active = FALSE";
cmd.ExecuteNonQuery();

// Delete all rows
cmd.CommandText = "DELETE FROM TempData";
cmd.ExecuteNonQuery();
```

## Working with Transactions

### Basic Transaction

```csharp
using var connection = new SharpHsqlConnection("Initial Catalog=.;User Id=sa;Pwd=");
connection.Open();

using var transaction = connection.BeginTransaction();

try
{
    var cmd = connection.CreateCommand();
    
    cmd.CommandText = "INSERT INTO Accounts VALUES (1, 1000)";
    cmd.ExecuteNonQuery();
    
    cmd.CommandText = "UPDATE Accounts SET Balance = Balance - 100 WHERE Id = 1";
    cmd.ExecuteNonQuery();
    
    // Commit if all succeeded
    transaction.Commit();
}
catch (Exception ex)
{
    // Rollback on error
    transaction.Rollback();
    Console.WriteLine($"Transaction failed: {ex.Message}");
    throw;
}
```

### Transaction with Isolation Level

```csharp
using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
// ... perform operations
transaction.Commit();
```

## Common Patterns

### Using DataAdapter

```csharp
var adapter = new SharpHsqlDataAdapter("SELECT * FROM Users", connection);
var dataSet = new DataSet();
adapter.Fill(dataSet);

// Bind to DataGridView, modify data, etc.
foreach (DataRow row in dataSet.Tables[0].Rows)
{
    Console.WriteLine(row["Name"]);
}
```

### Checking if Table Exists

```csharp
cmd.CommandText = "SELECT EXISTS('Users')";
bool exists = (bool)cmd.ExecuteScalar();

if (!exists)
{
    cmd.CommandText = "CREATE TABLE Users (Id INT PRIMARY KEY, Name VARCHAR(50))";
    cmd.ExecuteNonQuery();
}
```

### Getting Last Insert ID

```csharp
cmd.CommandText = "INSERT INTO Users VALUES (NULL, 'Auto ID User', 'auto@example.com')";
cmd.ExecuteNonQuery();

cmd.CommandText = "SELECT IDENTITY()";
int lastId = (int)cmd.ExecuteScalar();
Console.WriteLine($"Last inserted ID: {lastId}");
```

### Bulk Insert

```csharp
using var transaction = connection.BeginTransaction();

try
{
    var cmd = connection.CreateCommand();
    cmd.CommandText = "INSERT INTO Users VALUES (@id, @name, @email)";
    
    for (int i = 1; i <= 1000; i++)
    {
        cmd.Parameters.Clear();
        cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
            ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, i));
        cmd.Parameters.Add(new SharpHsqlParameter("@name", DbType.String, 0, 
            ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, $"User{i}"));
        cmd.Parameters.Add(new SharpHsqlParameter("@email", DbType.String, 0, 
            ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, $"user{i}@example.com"));
        cmd.ExecuteNonQuery();
    }
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Using Parameters with IN clause

```csharp
// For small lists, build the IN clause dynamically
var ids = new[] { 1, 2, 3, 4, 5 };
var inClause = string.Join(",", ids);
cmd.CommandText = $"SELECT * FROM Users WHERE Id IN ({inClause})";
```

## Next Steps

- Read the [Technical Reference](Technical-Reference.md) for complete SQL syntax documentation
- Explore the [test suite](../SharpHSQL.Tests/) for more examples
- Check out [CHANGELOG.md](../CHANGELOG.md) for version history

## Tips and Best Practices

1. **Use Parameters**: Always use parameterized queries to prevent SQL injection
2. **Transactions**: Wrap multiple operations in transactions for consistency
3. **Dispose Resources**: Use `using` statements to ensure proper cleanup
4. **File Paths**: Use absolute paths for file-based databases in production
5. **Error Handling**: Always check `result.Error` when using the Native API
6. **Performance**: Create indexes on frequently queried columns
7. **Testing**: Use in-memory databases (`.`) for unit tests

## Troubleshooting

### Connection Issues
```csharp
// Ensure the database path exists for file-based databases
var dbPath = "./mydb";
if (!Directory.Exists(Path.GetDirectoryName(dbPath)))
{
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
}
```

### Query Errors
```csharp
// Check for errors in results
if (result.Error != null && !string.IsNullOrEmpty(result.Error))
{
    throw new Exception($"SQL Error: {result.Error}");
}
```

---

**Need more help?** Check the [Technical Reference](Technical-Reference.md) or open an issue on [GitHub](https://github.com/andresvettori/sharphsql/issues).
