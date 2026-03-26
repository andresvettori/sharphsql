using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for transaction (COMMIT and ROLLBACK) functionality
    /// </summary>
    public class TransactionTests : IDisposable
    {
        private Database _database;
        private Channel _channel;

        public TransactionTests()
        {
            _database = new Database(".");
            _channel = _database.Connect("sa", "");
            
            // Create test table
            _database.Execute("CREATE TABLE Orders (id INT PRIMARY KEY, product VARCHAR(50), amount DECIMAL(10,2))", _channel);
        }

        [Fact]
        public void CanCommitTransaction()
        {
            // Arrange - Enable manual commit (auto-commit is default)
            _database.Execute("SET AUTOCOMMIT FALSE", _channel);

            // Act
            _database.Execute("INSERT INTO Orders VALUES (1, 'Widget', 99.99)", _channel);
            _database.Execute("INSERT INTO Orders VALUES (2, 'Gadget', 49.99)", _channel);
            var commitResult = _database.Execute("COMMIT", _channel);

            // Assert
            Assert.NotNull(commitResult);
            Assert.Null(commitResult.Error);
            
            // Verify data persisted
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Orders", _channel);
            Assert.Equal(2, selectResult.Root.Data[0]);
        }

        [Fact]
        public void CanRollbackTransaction()
        {
            // Arrange
            _database.Execute("SET AUTOCOMMIT FALSE", _channel);
            _database.Execute("INSERT INTO Orders VALUES (1, 'Widget', 99.99)", _channel);

            // Act
            var rollbackResult = _database.Execute("ROLLBACK", _channel);

            // Assert
            Assert.NotNull(rollbackResult);
            Assert.Null(rollbackResult.Error);
            
            // Verify data was not persisted
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Orders", _channel);
            Assert.Equal(0, selectResult.Root.Data[0]);
        }

        [Fact]
        public void AutoCommitInsertImmediatelyVisible()
        {
            // Arrange - Auto-commit is the default mode
            _database.Execute("SET AUTOCOMMIT TRUE", _channel);

            // Act
            _database.Execute("INSERT INTO Orders VALUES (1, 'Widget', 99.99)", _channel);

            // Assert - Data is immediately visible without explicit commit
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Orders", _channel);
            Assert.Equal(1, selectResult.Root.Data[0]);
        }

        [Fact]
        public void CanCommitMultipleOperationsInTransaction()
        {
            // Arrange
            _database.Execute("SET AUTOCOMMIT FALSE", _channel);

            // Act - Multiple operations in one transaction
            _database.Execute("INSERT INTO Orders VALUES (1, 'Product1', 10.00)", _channel);
            _database.Execute("INSERT INTO Orders VALUES (2, 'Product2', 20.00)", _channel);
            _database.Execute("INSERT INTO Orders VALUES (3, 'Product3', 30.00)", _channel);
            _database.Execute("DELETE FROM Orders WHERE id = 2", _channel);
            var commitResult = _database.Execute("COMMIT", _channel);

            // Assert
            Assert.NotNull(commitResult);
            Assert.Null(commitResult.Error);
            
            // Verify only 2 rows remain
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Orders", _channel);
            Assert.Equal(2, selectResult.Root.Data[0]);
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                _database.Execute("SET AUTOCOMMIT TRUE", _channel);
                _database.Execute("DROP TABLE Orders", _channel);
                _database.Execute("SHUTDOWN", _channel);
            }
            _database = null;
            _channel = null;
        }
    }
}
