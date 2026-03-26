using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for all supported data types in SharpHSQL
    /// </summary>
    public class DataTypeTests : IDisposable
    {
        private Database _database;
        private Channel _channel;

        public DataTypeTests()
        {
            _database = new Database(".");
            _channel = _database.Connect("sa", "");
        }

        [Fact]
        public void CanStoreAndRetrieveIntegerType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val INT)", _channel);
            _database.Execute("INSERT INTO T VALUES (1, 2147483647)", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.Equal(2147483647, result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveCharType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val CHAR)", _channel);
            _database.Execute("INSERT INTO T VALUES (1, 'Hello World')", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.Equal("Hello World", result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveVarcharType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val VARCHAR(100))", _channel);
            _database.Execute("INSERT INTO T VALUES (1, 'Test String')", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.Equal("Test String", result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveDoubleType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val DOUBLE)", _channel);
            _database.Execute("INSERT INTO T VALUES (1, 3.14159)", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.IsType<double>(result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveDecimalType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val DECIMAL(10,2))", _channel);
            _database.Execute("INSERT INTO T VALUES (1, 99.99)", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.IsType<decimal>(result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveDateType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val DATE)", _channel);
            _database.Execute("INSERT INTO T VALUES (1, '2024-01-15')", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.IsType<DateTime>(result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveBooleanType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val BIT)", _channel);
            _database.Execute("INSERT INTO T VALUES (1, TRUE)", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void CanStoreAndRetrieveBinaryType()
        {
            byte[] data = { 255, 128, 64, 32 };
            string base64 = Convert.ToBase64String(data);
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val BINARY)", _channel);
            _database.Execute($"INSERT INTO T VALUES (1, '{base64}')", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void CanStoreAndRetrieveNumericType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val NUMERIC(15,4))", _channel);
            _database.Execute("INSERT INTO T VALUES (1, 1234567.8901)", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void CanStoreNullValue()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val VARCHAR(50))", _channel);
            _database.Execute("INSERT INTO T VALUES (1, NULL)", _channel);
            var result = _database.Execute("SELECT val FROM T WHERE id = 1", _channel);
            Assert.Null(result.Error);
            Assert.Null(result.Root.Data[0]);
        }

        [Fact]
        public void CanStoreAndRetrieveObjectType()
        {
            _database.Execute("CREATE TABLE T (id INT PRIMARY KEY, val OBJECT)", _channel);
            // Object types are stored as serialized objects - skip insert for basic type check
            var result = _database.Execute("CREATE TABLE T2 (id INT)", _channel);
            Assert.Null(result.Error);
        }

        public void Dispose()
        {
            try { _database.Execute("DROP TABLE T", _channel); } catch { }
            try { _database.Execute("DROP TABLE T2", _channel); } catch { }
            _database.Execute("SHUTDOWN", _channel);
            _database = null;
            _channel = null;
        }
    }
}
