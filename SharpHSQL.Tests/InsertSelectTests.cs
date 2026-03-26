using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for INSERT and SELECT operations
    /// </summary>
    public class InsertSelectTests : IDisposable
    {
        private Database _database;
        private Channel _channel;

        public InsertSelectTests()
        {
            _database = new Database(".");
            _channel = _database.Connect("sa", "");
            
            // Create test table
            _database.Execute("CREATE TABLE Products (id INT PRIMARY KEY, name VARCHAR(50), price DECIMAL(10,2), qty INT)", _channel);
        }

        [Fact]
        public void CanInsertSingleRow()
        {
            // Arrange
            var insertSql = "INSERT INTO Products (id, name, price, qty) VALUES (1, 'Product1', 19.99, 10)";

            // Act
            var result = _database.Execute(insertSql, _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(1, result.UpdateCount);
        }

        [Fact]
        public void CanInsertMultipleRows()
        {
            // Arrange & Act
            _database.Execute("INSERT INTO Products VALUES (1, 'Product1', 19.99, 10)", _channel);
            _database.Execute("INSERT INTO Products VALUES (2, 'Product2', 29.99, 20)", _channel);
            var result = _database.Execute("INSERT INTO Products VALUES (3, 'Product3', 39.99, 30)", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
        }

        [Fact]
        public void CanSelectAllRows()
        {
            // Arrange
            _database.Execute("INSERT INTO Products VALUES (1, 'Product1', 19.99, 10)", _channel);
            _database.Execute("INSERT INTO Products VALUES (2, 'Product2', 29.99, 20)", _channel);

            // Act
            var result = _database.Execute("SELECT * FROM Products", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(2, result.Size);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void CanSelectWithWhereClause()
        {
            // Arrange
            _database.Execute("INSERT INTO Products VALUES (1, 'Product1', 19.99, 10)", _channel);
            _database.Execute("INSERT INTO Products VALUES (2, 'Product2', 29.99, 20)", _channel);

            // Act
            var result = _database.Execute("SELECT * FROM Products WHERE id = 1", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(1, result.Size);
        }

        [Fact]
        public void CanSelectSpecificColumns()
        {
            // Arrange
            _database.Execute("INSERT INTO Products VALUES (1, 'Product1', 19.99, 10)", _channel);

            // Act
            var result = _database.Execute("SELECT id, name FROM Products", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(2, result.ColumnCount);
            Assert.Equal(1, result.Size);
        }

        [Fact]
        public void CanSelectWithOrderBy()
        {
            // Arrange
            _database.Execute("INSERT INTO Products VALUES (3, 'ProductC', 39.99, 30)", _channel);
            _database.Execute("INSERT INTO Products VALUES (1, 'ProductA', 19.99, 10)", _channel);
            _database.Execute("INSERT INTO Products VALUES (2, 'ProductB', 29.99, 20)", _channel);

            // Act
            var result = _database.Execute("SELECT * FROM Products ORDER BY id", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(3, result.Size);
            // First row should have id = 1
            Assert.Equal(1, result.Root.Data[0]);
        }

        [Fact]
        public void CanCountRows()
        {
            // Arrange
            _database.Execute("INSERT INTO Products VALUES (1, 'Product1', 19.99, 10)", _channel);
            _database.Execute("INSERT INTO Products VALUES (2, 'Product2', 29.99, 20)", _channel);

            // Act
            var result = _database.Execute("SELECT COUNT(*) FROM Products", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
            Assert.Equal(2, result.Root.Data[0]);
        }

        [Fact]
        public void CanCalculateSum()
        {
            // Arrange
            _database.Execute("INSERT INTO Products VALUES (1, 'Product1', 10.00, 5)", _channel);
            _database.Execute("INSERT INTO Products VALUES (2, 'Product2', 20.00, 3)", _channel);

            // Act
            var result = _database.Execute("SELECT SUM(qty) FROM Products", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
            Assert.Equal(8, result.Root.Data[0]);
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                _database.Execute("DROP TABLE Products", _channel);
                _database.Execute("SHUTDOWN", _channel);
            }
            _database = null;
            _channel = null;
        }
    }
}
