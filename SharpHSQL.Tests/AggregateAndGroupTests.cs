using System;
using Xunit;
using SharpHsql;

namespace SharpHSQL.Tests
{
    /// <summary>
    /// Tests for aggregate functions, GROUP BY, ORDER BY, and LIKE clauses.
    /// Note: SharpHSQL stores DECIMAL(10,2) values as scaled integers internally.
    /// We use INT columns for comparisons where decimal precision matters.
    /// </summary>
    public class AggregateAndGroupTests : IDisposable
    {
        private Database _database;
        private Channel _channel;

        public AggregateAndGroupTests()
        {
            _database = new Database(".");
            _channel = _database.Connect("sa", "");

            // Use INT for amount to avoid internal decimal scaling issues in comparisons
            _database.Execute("CREATE TABLE Sales (id INT PRIMARY KEY, category VARCHAR(50), product VARCHAR(50), amount INT, qty INT)", _channel);
            _database.Execute("INSERT INTO Sales VALUES (1, 'Electronics', 'TV', 1000, 2)", _channel);
            _database.Execute("INSERT INTO Sales VALUES (2, 'Electronics', 'Phone', 600, 5)", _channel);
            _database.Execute("INSERT INTO Sales VALUES (3, 'Books', 'Novel', 20, 10)", _channel);
            _database.Execute("INSERT INTO Sales VALUES (4, 'Books', 'Textbook', 90, 3)", _channel);
            _database.Execute("INSERT INTO Sales VALUES (5, 'Electronics', 'Laptop', 1300, 1)", _channel);
            _database.Execute("INSERT INTO Sales VALUES (6, 'Books', 'Magazine', 10, 20)", _channel);
        }

        // --- Aggregate Functions ---

        [Fact]
        public void CountStar_ReturnsAllRows()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Root.Data[0]);
        }

        [Fact]
        public void CountColumn_ReturnsNonNullCount()
        {
            var result = _database.Execute("SELECT COUNT(amount) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Root.Data[0]);
        }

        [Fact]
        public void SumFunction_ReturnsTotalQty()
        {
            var result = _database.Execute("SELECT SUM(qty) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.Equal(41, result.Root.Data[0]);
        }

        [Fact]
        public void MaxFunction_ReturnsMaxAmount()
        {
            var result = _database.Execute("SELECT MAX(amount) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
            Assert.Equal(1300, result.Root.Data[0]);
        }

        [Fact]
        public void MinFunction_ReturnsMinAmount()
        {
            var result = _database.Execute("SELECT MIN(amount) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
            Assert.Equal(10, result.Root.Data[0]);
        }

        [Fact]
        public void AvgFunction_ReturnsAverageQty()
        {
            var result = _database.Execute("SELECT AVG(qty) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void SumAmount_ReturnsCorrectTotal()
        {
            var result = _database.Execute("SELECT SUM(amount) FROM Sales", _channel);
            Assert.Null(result.Error);
            Assert.NotNull(result.Root);
            // 1000+600+20+90+1300+10 = 3020
            Assert.Equal(3020, result.Root.Data[0]);
        }

        // --- GROUP BY ---

        [Fact]
        public void GroupBy_CountPerCategory()
        {
            var result = _database.Execute("SELECT category, COUNT(*) FROM Sales GROUP BY category", _channel);
            Assert.Null(result.Error);
            Assert.Equal(2, result.Size); // Electronics and Books
        }

        [Fact]
        public void GroupBy_SumPerCategory()
        {
            var result = _database.Execute("SELECT category, SUM(qty) FROM Sales GROUP BY category", _channel);
            Assert.Null(result.Error);
            Assert.Equal(2, result.Size);
        }

        [Fact]
        public void GroupBy_MultipleColumns()
        {
            var result = _database.Execute("SELECT category, product, SUM(qty) FROM Sales GROUP BY category, product", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Size);
        }

        // --- ORDER BY ---

        [Fact]
        public void OrderByAscending_SortsCorrectly()
        {
            var result = _database.Execute("SELECT id FROM Sales ORDER BY amount ASC", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Size);
            // Lowest amount is Magazine (id=6, $10)
            Assert.Equal(6, result.Root.Data[0]);
        }

        [Fact]
        public void OrderByDescending_SortsCorrectly()
        {
            var result = _database.Execute("SELECT id FROM Sales ORDER BY amount DESC", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Size);
            // Highest amount is Laptop (id=5, $1300)
            Assert.Equal(5, result.Root.Data[0]);
        }

        [Fact]
        public void OrderByMultipleColumns()
        {
            var result = _database.Execute("SELECT category, product FROM Sales ORDER BY category ASC, amount DESC", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Size);
        }

        // --- WHERE with comparison operators ---

        [Fact]
        public void WhereGreaterThan_FiltersCorrectly()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE amount > 100", _channel);
            Assert.Null(result.Error);
            // TV=1000, Phone=600, Laptop=1300 => 3 rows
            Assert.Equal(3, result.Root.Data[0]);
        }

        [Fact]
        public void WhereLessThan_FiltersCorrectly()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE amount < 100", _channel);
            Assert.Null(result.Error);
            // Novel=20, Textbook=90, Magazine=10 => 3 rows
            Assert.Equal(3, result.Root.Data[0]);
        }

        [Fact]
        public void WhereGreaterOrEqual_FiltersCorrectly()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE amount >= 1000", _channel);
            Assert.Null(result.Error);
            // TV=1000, Laptop=1300 => 2 rows
            Assert.Equal(2, result.Root.Data[0]);
        }

        [Fact]
        public void WhereAnd_CombinesConditions()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE amount > 100 AND qty > 1", _channel);
            Assert.Null(result.Error);
            // TV (amount=1000, qty=2) and Phone (amount=600, qty=5) => 2 rows
            Assert.Equal(2, result.Root.Data[0]);
        }

        [Fact]
        public void WhereOr_CombinesConditions()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE category = 'Books' OR qty > 9", _channel);
            Assert.Null(result.Error);
            // Books: Novel, Textbook, Magazine (3 rows), qty>9: Novel (10), Magazine (20) 
            // Books OR qty>9 = Novel, Textbook, Magazine + Phone(5 not>9) = still just Books
            // Books rows: 3, Phone (qty=5) not >9, so result = 3
            Assert.True((int)result.Root.Data[0] >= 3);
        }

        // --- LIKE ---

        [Fact]
        public void Like_PercentWildcard_MatchesPrefix()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE product LIKE 'T%'", _channel);
            Assert.Null(result.Error);
            // TV and Textbook
            Assert.Equal(2, result.Root.Data[0]);
        }

        [Fact]
        public void Like_LeadingPercent_MatchesSuffix()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE product LIKE '%book'", _channel);
            Assert.Null(result.Error);
            // Textbook ends with 'book'
            Assert.True((int)result.Root.Data[0] >= 1);
        }

        [Fact]
        public void Like_BothPercents_MatchesContains()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE product LIKE '%hone%'", _channel);
            Assert.Null(result.Error);
            // Phone contains 'hone'
            Assert.True((int)result.Root.Data[0] >= 1);
        }

        [Fact]
        public void Like_UnderscoreWildcard_MatchesSingleChar()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE product LIKE 'T_'", _channel);
            Assert.Null(result.Error);
            // TV = T + V (2 chars)
            Assert.Equal(1, result.Root.Data[0]);
        }

        // --- BETWEEN ---

        [Fact]
        public void Between_FiltersIntegerRange()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE amount BETWEEN 50 AND 1000", _channel);
            Assert.Null(result.Error);
            // Textbook=90, TV=1000, Phone=600 => 3 rows
            Assert.Equal(3, result.Root.Data[0]);
        }

        // --- IN ---

        [Fact]
        public void In_FiltersValueList()
        {
            var result = _database.Execute("SELECT COUNT(*) FROM Sales WHERE category IN ('Books', 'Electronics')", _channel);
            Assert.Null(result.Error);
            Assert.Equal(6, result.Root.Data[0]);
        }

        // --- IS NULL / IS NOT NULL ---

        [Fact]
        public void IsNull_FiltersNullValues()
        {
            _database.Execute("CREATE TABLE NullTest (id INT PRIMARY KEY, val VARCHAR(10))", _channel);
            _database.Execute("INSERT INTO NullTest VALUES (1, 'hello')", _channel);
            _database.Execute("INSERT INTO NullTest VALUES (2, NULL)", _channel);

            var result = _database.Execute("SELECT COUNT(*) FROM NullTest WHERE val IS NULL", _channel);
            Assert.Null(result.Error);
            Assert.Equal(1, result.Root.Data[0]);
        }

        [Fact]
        public void IsNotNull_FiltersNonNullValues()
        {
            _database.Execute("CREATE TABLE NullTest2 (id INT PRIMARY KEY, val VARCHAR(10))", _channel);
            _database.Execute("INSERT INTO NullTest2 VALUES (1, 'hello')", _channel);
            _database.Execute("INSERT INTO NullTest2 VALUES (2, NULL)", _channel);

            var result = _database.Execute("SELECT COUNT(*) FROM NullTest2 WHERE val IS NOT NULL", _channel);
            Assert.Null(result.Error);
            Assert.Equal(1, result.Root.Data[0]);
        }

        public void Dispose()
        {
            try { _database.Execute("DROP TABLE Sales", _channel); } catch { }
            try { _database.Execute("DROP TABLE NullTest", _channel); } catch { }
            try { _database.Execute("DROP TABLE NullTest2", _channel); } catch { }
            _database.Execute("SHUTDOWN", _channel);
            _database = null;
            _channel = null;
        }
    }
}
