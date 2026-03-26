using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for basic database connection functionality
    /// </summary>
    public class DatabaseConnectionTests : IDisposable
    {
        private Database _database;

        public DatabaseConnectionTests()
        {
            // Create an in-memory database for testing
            _database = new Database(".");
        }

        [Fact]
        public void CanCreateDatabase()
        {
            // Arrange & Act
            var db = new Database(".");

            // Assert
            Assert.NotNull(db);
        }

        [Fact]
        public void CanConnectWithDefaultUser()
        {
            // Arrange & Act
            var channel = _database.Connect("sa", "");

            // Assert
            Assert.NotNull(channel);
        }

        [Fact]
        public void CanConnectAndDisconnect()
        {
            // Arrange
            var channel = _database.Connect("sa", "");

            // Act
            _database.Execute("DISCONNECT", channel);

            // Assert - should not throw
            Assert.NotNull(channel);
        }

        [Fact]
        public void CanExecuteSimpleQuery()
        {
            // Arrange
            var channel = _database.Connect("sa", "");

            // Act
            var result = _database.Execute("SELECT 1 FROM SYSTEM_TABLES WHERE 1=0", channel);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
        }

        public void Dispose()
        {
            _database = null;
        }
    }
}
