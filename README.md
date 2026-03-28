# SharpHSQL - Embedded SQL Database for .NET

[![License](https://img.shields.io/badge/License-BSD%203--Clause-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/SharpHsql.svg)](https://www.nuget.org/packages/SharpHsql/)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-green.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

**SharpHSQL** is a lightweight, embedded SQL database engine for .NET, ported from the popular Java HSQLDB (HyperSQL Database). It provides a complete SQL database solution with both in-memory and file-based storage options, perfect for applications that need a self-contained database without external dependencies.

## ✨ Features

- **🚀 Lightweight & Embedded** - No external database server required, runs entirely within your .NET application
- **💾 Dual Storage Modes** - Choose between in-memory (ultra-fast) or file-based (persistent) storage
- **🔒 ACID Compliant** - Full transaction support with commit and rollback capabilities
- **📊 Rich SQL Support** - Comprehensive SQL syntax including JOINs, subqueries, aggregates, and more
- **🔌 Dual API** - Native API for advanced control or ADO.NET provider for standard database access
- **⚡ High Performance** - Optimized for speed with efficient indexing and caching
- **🎯 .NET Standard 2.0** - Compatible with .NET Core, .NET 5+, .NET Framework 4.6.1+, Xamarin, and more
- **📝 100+ Built-in Functions** - Extensive library of math, string, date/time, and system functions

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package SharpHsql.Core
```

Or via NuGet Package Manager Console:

```powershell
Install-Package SharpHsql.Core
```

## 🚀 Quick Start

### Using ADO.NET Provider (Recommended)

```csharp
using System.Data.Hsql;

// Create connection
using var connection = new SharpHsqlConnection("Initial Catalog=mydb;User Id=sa;Pwd=");
connection.Open();

// Create table
using var createCmd = new SharpHsqlCommand(
    "CREATE TABLE Users (Id INT PRIMARY KEY, Name VARCHAR(50), Email VARCHAR(100))", 
    connection);
createCmd.ExecuteNonQuery();

// Insert data
using var insertCmd = new SharpHsqlCommand(
    "INSERT INTO Users VALUES (@id, @name, @email)", 
    connection);
insertCmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1));
insertCmd.Parameters.Add(new SharpHsqlParameter("@name", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "John Doe"));
insertCmd.Parameters.Add(new SharpHsqlParameter("@email", DbType.String, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "john@example.com"));
insertCmd.ExecuteNonQuery();

// Query data
using var selectCmd = new SharpHsqlCommand("SELECT * FROM Users", connection);
using var reader = selectCmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"ID: {reader.GetInt32(0)}, Name: {reader.GetString(1)}");
}
```

### Using Native API

```csharp
using SharpHsql;

// Create database
var database = new Database("mydb");

// Connect
var channel = database.Connect("sa", "");

// Execute commands
var result = database.Execute("CREATE TABLE Products (Id INT, Name VARCHAR(50))", channel);
result = database.Execute("INSERT INTO Products VALUES (1, 'Widget')", channel);
result = database.Execute("SELECT * FROM Products", channel);

// Process results
while (result.Root != null)
{
    var row = result.Root.Data;
    Console.WriteLine($"ID: {row[0]}, Name: {row[1]}");
    result.Root = result.Root.Next;
}

// Disconnect
database.Execute("DISCONNECT", channel);
```

## 💡 Common Usage Examples

### Working with Transactions

```csharp
using var connection = new SharpHsqlConnection("Initial Catalog=mydb;User Id=sa;Pwd=");
connection.Open();

// Begin transaction
using var transaction = connection.BeginTransaction();

try
{
    var cmd = new SharpHsqlCommand("INSERT INTO Accounts VALUES (1, 1000)", connection);
    cmd.ExecuteNonQuery();
    
    cmd.CommandText = "UPDATE Accounts SET Balance = Balance - 100 WHERE Id = 1";
    cmd.ExecuteNonQuery();
    
    // Commit transaction
    transaction.Commit();
}
catch
{
    // Rollback on error
    transaction.Rollback();
    throw;
}
```

### Using DataAdapter for DataSets

```csharp
using var connection = new SharpHsqlConnection("Initial Catalog=mydb;User Id=sa;Pwd=");
connection.Open();

var adapter = new SharpHsqlDataAdapter("SELECT * FROM Users", connection);
var dataSet = new DataSet();
adapter.Fill(dataSet);

// Work with DataSet
foreach (DataRow row in dataSet.Tables[0].Rows)
{
    Console.WriteLine(row["Name"]);
}
```

### In-Memory Database

```csharp
// In-memory database (data lost when application closes)
var database = new Database(".");
var channel = database.Connect("sa", "");

// Create and use tables
database.Execute("CREATE TABLE TempData (Id INT, Value VARCHAR(50))", channel);
database.Execute("INSERT INTO TempData VALUES (1, 'Temporary')", channel);
```

### File-Based Database

```csharp
// File-based database (data persists to disk)
var database = new Database("./mydata");
var channel = database.Connect("sa", "");

// Data is automatically saved to disk
database.Execute("CREATE TABLE Users (Id INT PRIMARY KEY, Name VARCHAR(50))", channel);
database.Execute("INSERT INTO Users VALUES (1, 'Alice')", channel);

// Data will be available on next application start
```

## 📚 Documentation

- **[Quick Start Guide](docs/QuickStart.md)** - Get up and running in 5 minutes
- **[Technical Reference](docs/Technical-Reference.md)** - Complete SQL syntax and API documentation
- **[CHANGELOG](CHANGELOG.md)** - Version history and release notes
- **[CONTRIBUTING](CONTRIBUTING.md)** - How to contribute to the project

## 🎯 Supported SQL Features

### Data Definition Language (DDL)
- `CREATE TABLE` - Create tables with various data types
- `DROP TABLE` - Remove tables
- `ALTER TABLE` - Add/remove columns
- `CREATE INDEX` - Create indexes for performance

### Data Manipulation Language (DML)
- `SELECT` - Query data with WHERE, JOIN, GROUP BY, ORDER BY
- `INSERT` - Add new rows (with VALUES or SELECT)
- `UPDATE` - Modify existing rows
- `DELETE` - Remove rows

### Advanced Features
- **Joins**: INNER JOIN, LEFT OUTER JOIN
- **Subqueries**: In SELECT, FROM, and WHERE clauses
- **Set Operations**: UNION, UNION ALL, INTERSECT, EXCEPT
- **Aggregates**: COUNT, SUM, AVG, MIN, MAX
- **Functions**: 100+ built-in functions for math, strings, dates, and more

### Data Types
- **Numeric**: TINYINT, SMALLINT, INTEGER, BIGINT, DECIMAL, DOUBLE, FLOAT
- **String**: CHAR, VARCHAR, LONGVARCHAR
- **Binary**: BINARY, VARBINARY, LONGVARBINARY
- **Date/Time**: DATE, TIME, TIMESTAMP
- **Other**: BIT (boolean), NULL

## 🔧 Building from Source

### Prerequisites
- .NET SDK 6.0 or higher
- Git

### Build Steps

```bash
# Clone the repository
git clone https://github.com/andresvettori/sharphsql.git
cd sharphsql/sourceCode/src/Main/Source/SharpHSQL

# Build the solution
./build.ps1

# Build and create NuGet package
./build.ps1 -CreatePackage

# Build, run tests, and create package
./build.ps1 -Configuration Release -CreatePackage
```

The build script uses GitVersion for automatic semantic versioning based on your Git history.

## 🧪 Running Tests

```bash
dotnet test SharpHSQL.Tests/SharpHSQL.Tests.csproj
```

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) for details on how to submit pull requests, report issues, and contribute to the project.

## 📄 License

SharpHSQL is licensed under the **BSD 3-Clause License**. See [LICENSE](LICENSE) file for details.

This project is based on HypersonicSQL, originally developed by Thomas Mueller.

## 🌟 Acknowledgments

- Original HSQLDB Java project: [http://hsqldb.org/](http://hsqldb.org/)
- C# port contributors: Mark Tutt, Andrés G Vettori, and community contributors

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/andresvettori/sharphsql/issues)
- **Discussions**: [GitHub Discussions](https://github.com/andresvettori/sharphsql/discussions)

## 🗺️ Roadmap

- [ ] .NET 8.0 optimizations
- [ ] Additional SQL functions
- [ ] Performance improvements
- [ ] Enhanced documentation and examples
- [ ] Community-requested features

---

**Made with ❤️ by Andrés G Vettori and contributors**
