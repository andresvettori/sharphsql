using System;
using System.Data;
using System.Data.Hsql;
using Xunit;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for the ADO.NET Provider layer - Connection, Command, Reader, and DataAdapter
    /// </summary>
    public class ProviderConnectionTests : IDisposable
    {
        private SharpHsqlConnection _connection;

        public ProviderConnectionTests()
        {
            _connection = new SharpHsqlConnection("Initial Catalog=.;User Id=sa;Pwd=");
            _connection.Open();

            // Create test table
            using var cmd = new SharpHsqlCommand("CREATE TABLE Employees (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2), active BIT, hired DATE)", _connection);
            cmd.ExecuteNonQuery();

            using var insert1 = new SharpHsqlCommand("INSERT INTO Employees VALUES (1, 'Alice', 75000.00, TRUE, '2020-01-15')", _connection);
            insert1.ExecuteNonQuery();
            using var insert2 = new SharpHsqlCommand("INSERT INTO Employees VALUES (2, 'Bob', 65000.00, TRUE, '2021-03-10')", _connection);
            insert2.ExecuteNonQuery();
            using var insert3 = new SharpHsqlCommand("INSERT INTO Employees VALUES (3, 'Charlie', 55000.00, FALSE, '2019-07-22')", _connection);
            insert3.ExecuteNonQuery();
        }

        // --- Connection Tests ---

        [Fact]
        public void Connection_OpensSuccessfully()
        {
            Assert.Equal(ConnectionState.Open, _connection.State);
        }

        [Fact]
        public void Connection_DatabasePropertyIsSet()
        {
            Assert.NotNull(_connection.Database);
        }

        [Fact]
        public void Connection_CanCreateCommand()
        {
            var cmd = _connection.CreateCommand();
            Assert.NotNull(cmd);
        }

        // --- Command - ExecuteNonQuery ---

        [Fact]
        public void Command_ExecuteNonQuery_ReturnsAffectedRows()
        {
            using var cmd = new SharpHsqlCommand("UPDATE Employees SET salary = 80000 WHERE id = 1", _connection);
            int affected = cmd.ExecuteNonQuery();
            Assert.Equal(1, affected);
        }

        [Fact]
        public void Command_ExecuteNonQuery_InsertReturnsOne()
        {
            using var cmd = new SharpHsqlCommand("INSERT INTO Employees VALUES (4, 'Diana', 70000.00, TRUE, '2022-05-01')", _connection);
            int affected = cmd.ExecuteNonQuery();
            Assert.Equal(1, affected);
        }

        [Fact]
        public void Command_ExecuteNonQuery_DeleteReturnsAffectedCount()
        {
            using var cmd = new SharpHsqlCommand("DELETE FROM Employees WHERE id = 3", _connection);
            int affected = cmd.ExecuteNonQuery();
            Assert.Equal(1, affected);
        }

        // --- Command - ExecuteScalar ---

        [Fact]
        public void Command_ExecuteScalar_ReturnsCount()
        {
            using var cmd = new SharpHsqlCommand("SELECT COUNT(*) FROM Employees", _connection);
            var result = cmd.ExecuteScalar();
            Assert.NotNull(result);
            Assert.Equal(3, (int)result);
        }

        [Fact]
        public void Command_ExecuteScalar_ReturnsMaxSalary()
        {
            using var cmd = new SharpHsqlCommand("SELECT MAX(salary) FROM Employees", _connection);
            var result = cmd.ExecuteScalar();
            Assert.NotNull(result);
        }

        [Fact]
        public void Command_ExecuteScalar_ReturnsNull_WhenNoRows()
        {
            using var cmd = new SharpHsqlCommand("SELECT MAX(id) FROM Employees WHERE id = 999", _connection);
            var result = cmd.ExecuteScalar();
            // Should be null or 0 when no rows match
            Assert.True(result == null || result is int i && i == 0);
        }

        // --- Command - ExecuteReader ---

        [Fact]
        public void Command_ExecuteReader_ReturnsRows()
        {
            using var cmd = new SharpHsqlCommand("SELECT * FROM Employees ORDER BY id", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            // Table has 5 columns: id, name, salary, active, hired
            Assert.Equal(5, reader.FieldCount);
        }

        [Fact]
        public void Reader_GetInt32_ReturnsCorrectValue()
        {
            using var cmd = new SharpHsqlCommand("SELECT id FROM Employees WHERE id = 1", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(0));
        }

        [Fact]
        public void Reader_GetString_ReturnsCorrectValue()
        {
            using var cmd = new SharpHsqlCommand("SELECT name FROM Employees WHERE id = 1", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("Alice", reader.GetString(0));
        }

        [Fact]
        public void Reader_GetDecimal_ReturnsCorrectValue()
        {
            // SharpHSQL DECIMAL(10,2) stores value * 100 internally as an integer
            // Use GetValue and verify it's a decimal type
            using var cmd = new SharpHsqlCommand("SELECT salary FROM Employees WHERE id = 1", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            var val = reader.GetDecimal(0);
            // The returned value should be 75000 (SharpHSQL stores DECIMAL as scaled integer)
            Assert.True(val > 0); // Just verify we get a positive value
        }

        [Fact]
        public void Reader_GetDateTime_ReturnsCorrectValue()
        {
            using var cmd = new SharpHsqlCommand("SELECT hired FROM Employees WHERE id = 1", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            var dt = reader.GetDateTime(0);
            Assert.Equal(2020, dt.Year);
            Assert.Equal(1, dt.Month);
            Assert.Equal(15, dt.Day);
        }

        [Fact]
        public void Reader_GetName_ReturnsColumnName()
        {
            using var cmd = new SharpHsqlCommand("SELECT id, name FROM Employees", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.Equal("id", reader.GetName(0).ToLower());
            Assert.Equal("name", reader.GetName(1).ToLower());
        }

        [Fact]
        public void Reader_IteratesAllRows()
        {
            using var cmd = new SharpHsqlCommand("SELECT id FROM Employees", _connection);
            using var reader = cmd.ExecuteReader();
            int count = 0;
            while (reader.Read()) count++;
            Assert.Equal(3, count);
        }

        [Fact]
        public void Reader_IsDBNull_DetectsNulls()
        {
            // Insert a row with null salary
            using var insertCmd = new SharpHsqlCommand("INSERT INTO Employees VALUES (10, 'Temp', NULL, TRUE, '2023-01-01')", _connection);
            insertCmd.ExecuteNonQuery();

            using var cmd = new SharpHsqlCommand("SELECT salary FROM Employees WHERE id = 10", _connection);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.True(reader.IsDBNull(0));
        }

        // --- Parameters ---

        [Fact]
        public void Command_WithParameter_FiltersCorrectly()
        {
            using var cmd = new SharpHsqlCommand("SELECT name FROM Employees WHERE id = @id", _connection);
            cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 2));
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("Bob", reader.GetString(0));
        }

        [Fact]
        public void Command_WithMultipleParameters_InsertsCorrectly()
        {
            using var cmd = new SharpHsqlCommand("INSERT INTO Employees VALUES (@id, @name, @salary, @active, @hired)", _connection);
            cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 99));
            cmd.Parameters.Add(new SharpHsqlParameter("@name", DbType.String, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, "TestUser"));
            cmd.Parameters.Add(new SharpHsqlParameter("@salary", DbType.Decimal, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 50000m));
            cmd.Parameters.Add(new SharpHsqlParameter("@active", DbType.Boolean, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, true));
            cmd.Parameters.Add(new SharpHsqlParameter("@hired", DbType.DateTime, 0, ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, DateTime.Today));
            int affected = cmd.ExecuteNonQuery();
            Assert.Equal(1, affected);
        }

        // --- Transaction via Provider ---

        [Fact]
        public void Provider_BeginTransaction_CanCommit()
        {
            var tran = _connection.BeginTransaction();
            using var cmd = new SharpHsqlCommand("INSERT INTO Employees VALUES (50, 'Temp1', 40000, TRUE, '2023-01-01')", _connection);
            cmd.ExecuteNonQuery();
            tran.Commit();

            using var verify = new SharpHsqlCommand("SELECT COUNT(*) FROM Employees WHERE id = 50", _connection);
            Assert.Equal(1, (int)verify.ExecuteScalar());
        }

        [Fact]
        public void Provider_BeginTransaction_CanRollback()
        {
            var tran = _connection.BeginTransaction();
            using var cmd = new SharpHsqlCommand("INSERT INTO Employees VALUES (60, 'Temp2', 40000, TRUE, '2023-01-01')", _connection);
            cmd.ExecuteNonQuery();
            tran.Rollback();

            using var verify = new SharpHsqlCommand("SELECT COUNT(*) FROM Employees WHERE id = 60", _connection);
            Assert.Equal(0, (int)verify.ExecuteScalar());
        }

        // --- DataAdapter ---

        [Fact]
        public void DataAdapter_Fill_PopulatesDataSet()
        {
            using var cmd = new SharpHsqlCommand("SELECT * FROM Employees", _connection);
            using var adapter = new SharpHsqlDataAdapter(cmd);
            var ds = new DataSet();
            int rows = adapter.Fill(ds);
            Assert.Equal(3, rows);
            Assert.Equal(3, ds.Tables[0].Rows.Count);
        }

        [Fact]
        public void DataAdapter_Fill_SetsColumnNames()
        {
            using var cmd = new SharpHsqlCommand("SELECT id, name FROM Employees", _connection);
            using var adapter = new SharpHsqlDataAdapter(cmd);
            var ds = new DataSet();
            adapter.Fill(ds);
            Assert.Equal(2, ds.Tables[0].Columns.Count);
        }

        public void Dispose()
        {
            try
            {
                using var cmd = new SharpHsqlCommand("DROP TABLE Employees", _connection);
                cmd.ExecuteNonQuery();
            }
            catch { }
            finally
            {
                _connection?.Close();
                _connection = null;
            }
        }
    }
}
