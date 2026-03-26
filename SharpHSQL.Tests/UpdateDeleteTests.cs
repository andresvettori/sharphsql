using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for UPDATE and DELETE operations
    /// </summary>
    public class UpdateDeleteTests : IDisposable
    {
        private Database _database;
        private Channel _channel;

        public UpdateDeleteTests()
        {
            _database = new Database(".");
            _channel = _database.Connect("sa", "");
            
            // Create test table and insert initial data
            _database.Execute("CREATE TABLE Inventory (id INT PRIMARY KEY, item VARCHAR(50), quantity INT, price DECIMAL(10,2))", _channel);
            _database.Execute("INSERT INTO Inventory VALUES (1, 'Widget', 100, 9.99)", _channel);
            _database.Execute("INSERT INTO Inventory VALUES (2, 'Gadget', 50, 19.99)", _channel);
            _database.Execute("INSERT INTO Inventory VALUES (3, 'Doohickey', 75, 14.99)", _channel);
        }

        [Fact]
        public void CanUpdateSingleRow()
        {
            // Act
            var result = _database.Execute("UPDATE Inventory SET quantity = 150 WHERE id = 1", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(1, result.UpdateCount);
            
            // Verify the update
            var selectResult = _database.Execute("SELECT quantity FROM Inventory WHERE id = 1", _channel);
            Assert.Equal(150, selectResult.Root.Data[0]);
        }

        [Fact]
        public void CanUpdateMultipleColumns()
        {
            // Act
            var result = _database.Execute("UPDATE Inventory SET quantity = 200, price = 12.99 WHERE id = 2", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            
            // Verify the update
            var selectResult = _database.Execute("SELECT quantity, price FROM Inventory WHERE id = 2", _channel);
            Assert.Equal(200, selectResult.Root.Data[0]);
        }

        [Fact]
        public void CanUpdateMultipleRows()
        {
            // Act
            var result = _database.Execute("UPDATE Inventory SET price = 9.99 WHERE quantity > 50", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.True(result.UpdateCount >= 2);
        }

        [Fact]
        public void CanDeleteSingleRow()
        {
            // Act
            var result = _database.Execute("DELETE FROM Inventory WHERE id = 1", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(1, result.UpdateCount);
            
            // Verify deletion
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Inventory", _channel);
            Assert.Equal(2, selectResult.Root.Data[0]);
        }

        [Fact]
        public void CanDeleteWithWhereClause()
        {
            // Act
            var result = _database.Execute("DELETE FROM Inventory WHERE quantity < 60", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            
            // Verify remaining rows
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Inventory", _channel);
            Assert.True((int)selectResult.Root.Data[0] <= 2);
        }

        [Fact]
        public void CanDeleteAllRows()
        {
            // Act
            var result = _database.Execute("DELETE FROM Inventory", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            
            // Verify all rows deleted
            var selectResult = _database.Execute("SELECT COUNT(*) FROM Inventory", _channel);
            Assert.Equal(0, selectResult.Root.Data[0]);
        }

        [Fact]
        public void UpdateNonExistentRow_ReturnsZeroUpdateCount()
        {
            // Act
            var result = _database.Execute("UPDATE Inventory SET quantity = 999 WHERE id = 999", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(0, result.UpdateCount);
        }

        [Fact]
        public void DeleteNonExistentRow_ReturnsZeroUpdateCount()
        {
            // Act
            var result = _database.Execute("DELETE FROM Inventory WHERE id = 999", _channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(0, result.UpdateCount);
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                _database.Execute("DROP TABLE Inventory", _channel);
                _database.Execute("SHUTDOWN", _channel);
            }
            _database = null;
            _channel = null;
        }
    }
}
