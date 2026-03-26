using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for table creation, dropping, and schema operations
    /// </summary>
    public class TableOperationsTests : IDisposable
    {
        private Database _database;
        private Channel _channel;

        public TableOperationsTests()
        {
            _database = new Database(".");
            _channel = _database.Connect("sa", "");
        }

        [Fact]
        public void CanCreateTable()
        {
            // Arrange
            var createTableSql = "CREATE TABLE TestTable (id INT PRIMARY KEY, name VARCHAR(50))";

            // Act
            var result = _database.Execute(createTableSql, _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
        }

        [Fact]
        public void CanDropTable()
        {
            // Arrange
            _database.Execute("CREATE TABLE TestTable (id INT PRIMARY KEY)", _channel);

            // Act
            var result = _database.Execute("DROP TABLE TestTable", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
        }

        [Fact]
        public void CanCreateTableWithMultipleColumns()
        {
            // Arrange
            var createTableSql = @"CREATE TABLE Users (
                id INT PRIMARY KEY,
                username VARCHAR(50),
                email VARCHAR(100),
                age INT,
                created DATE
            )";

            // Act
            var result = _database.Execute(createTableSql, _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
        }

        [Fact]
        public void CanCreateTableWithAutoIncrement()
        {
            // Arrange
            var createTableSql = "CREATE TABLE AutoTable (id INT IDENTITY PRIMARY KEY, value VARCHAR(50))";

            // Act
            var result = _database.Execute(createTableSql, _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
        }

        [Fact]
        public void DropNonExistentTable_ReturnsError()
        {
            // Act
            var result = _database.Execute("DROP TABLE NonExistentTable", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                _database.Execute("SHUTDOWN", _channel);
            }
            _database = null;
            _channel = null;
        }
    }
}
