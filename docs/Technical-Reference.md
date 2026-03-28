# SharpHSQL Technical Reference

Complete technical documentation for SharpHSQL, including all supported SQL commands, built-in functions, data types, and API reference.

## Table of Contents

- [SQL Command Reference](#sql-command-reference)
  - [Data Definition Language (DDL)](#data-definition-language-ddl)
  - [Data Manipulation Language (DML)](#data-manipulation-language-dml)
  - [Transaction Control](#transaction-control)
  - [Other Commands](#other-commands)
- [Data Types](#data-types)
- [Built-in Functions](#built-in-functions)
  - [Mathematical Functions](#mathematical-functions)
  - [String Functions](#string-functions)
  - [Date and Time Functions](#date-and-time-functions)
  - [System Functions](#system-functions)
  - [Aggregate Functions](#aggregate-functions)
  - [Conversion Functions](#conversion-functions)
- [ADO.NET Provider API](#adonet-provider-api)
- [Native API Reference](#native-api-reference)
- [Connection Strings](#connection-strings)
- [Error Handling](#error-handling)

---

## SQL Command Reference

### Data Definition Language (DDL)

#### CREATE TABLE

Creates a new table in the database.

**Syntax:**
```sql
CREATE TABLE table_name (
    column1 datatype [constraints],
    column2 datatype [constraints],
    ...
    [PRIMARY KEY (column_name)]
)
```

**Examples:**
```sql
-- Simple table
CREATE TABLE Users (
    Id INT,
    Name VARCHAR(100),
    Email VARCHAR(100)
)

-- Table with primary key
CREATE TABLE Products (
    ProductId INT PRIMARY KEY,
    ProductName VARCHAR(200),
    Price DECIMAL(10,2),
    InStock BIT
)

-- Table with multiple columns and constraints
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY,
    CustomerId INT,
    OrderDate DATE,
    Total DECIMAL(12,2),
    Status VARCHAR(20)
)
```

**Supported Data Types:**
- Numeric: `TINYINT`, `SMALLINT`, `INTEGER` (or `INT`), `BIGINT`, `DECIMAL(p,s)`, `DOUBLE`, `FLOAT`, `REAL`
- String: `CHAR(n)`, `VARCHAR(n)`, `LONGVARCHAR`
- Binary: `BINARY`, `VARBINARY`, `LONGVARBINARY`
- Date/Time: `DATE`, `TIME`, `TIMESTAMP`
- Boolean: `BIT` (TRUE/FALSE)

#### DROP TABLE

Removes a table and all its data from the database.

**Syntax:**
```sql
DROP TABLE table_name
```

**Examples:**
```sql
DROP TABLE Users
DROP TABLE TempData
```

#### ALTER TABLE

Modifies an existing table structure by adding or removing columns.

**Syntax:**
```sql
-- Add column
ALTER TABLE table_name ADD COLUMN column_name datatype

-- Delete column
ALTER TABLE table_name DELETE COLUMN column_name
```

**Examples:**
```sql
-- Add a new column
ALTER TABLE Users ADD COLUMN PhoneNumber VARCHAR(20)

-- Add multiple columns (execute separately)
ALTER TABLE Products ADD COLUMN CategoryId INT
ALTER TABLE Products ADD COLUMN Description VARCHAR(500)

-- Remove a column
ALTER TABLE Users DELETE COLUMN PhoneNumber
```

**Notes:**
- When adding a column, existing rows will have NULL values for the new column
- When deleting a column, all data in that column is permanently lost
- Primary key columns cannot be deleted if they are in use
- Indexes on deleted columns are automatically removed

#### CREATE INDEX

Creates an index on one or more columns to improve query performance.

**Syntax:**
```sql
CREATE [UNIQUE] INDEX index_name ON table_name (column1 [, column2, ...])
```

**Examples:**
```sql
-- Simple index on single column
CREATE INDEX idx_user_email ON Users (Email)

-- Composite index on multiple columns
CREATE INDEX idx_order_customer_date ON Orders (CustomerId, OrderDate)

-- Unique index
CREATE UNIQUE INDEX idx_product_code ON Products (ProductCode)
```

**Notes:**
- Indexes improve SELECT performance but slow down INSERT, UPDATE, and DELETE operations
- Primary keys automatically have a unique index
- Use indexes on columns frequently used in WHERE clauses and JOIN conditions

---

### Data Manipulation Language (DML)

#### SELECT

Retrieves data from one or more tables.

**Basic Syntax:**
```sql
SELECT [DISTINCT] column1, column2, ...
FROM table_name
[WHERE condition]
[GROUP BY column1, column2, ...]
[HAVING condition]
[ORDER BY column1 [ASC|DESC], ...]
[UNION | UNION ALL | INTERSECT | EXCEPT select_statement]
```

**Examples:**

**Simple SELECT:**
```sql
-- All columns
SELECT * FROM Users

-- Specific columns
SELECT Id, Name, Email FROM Users

-- With WHERE clause
SELECT * FROM Products WHERE Price > 100

-- With multiple conditions
SELECT * FROM Orders 
WHERE Status = 'Pending' AND Total > 500
```

**DISTINCT:**
```sql
-- Remove duplicates
SELECT DISTINCT Country FROM Customers

-- Distinct on multiple columns
SELECT DISTINCT City, State FROM Addresses
```

**LIMIT (SharpHSQL extension):**
```sql
-- Limit number of results
SELECT LIMIT 0, 10 * FROM Users  -- First 10 rows
SELECT LIMIT 10, 20 * FROM Users -- Rows 11-30
```

**ORDER BY:**
```sql
-- Ascending order (default)
SELECT * FROM Products ORDER BY Price

-- Descending order
SELECT * FROM Products ORDER BY Price DESC

-- Multiple columns
SELECT * FROM Users ORDER BY LastName ASC, FirstName ASC

-- Order by column position
SELECT Name, Price FROM Products ORDER BY 2 DESC
```

**GROUP BY and Aggregates:**
```sql
-- Count by category
SELECT CategoryId, COUNT(*) as ProductCount
FROM Products
GROUP BY CategoryId

-- Sum with multiple aggregates
SELECT 
    CustomerId,
    COUNT(*) as OrderCount,
    SUM(Total) as TotalSpent,
    AVG(Total) as AvgOrderValue
FROM Orders
GROUP BY CustomerId

-- With HAVING clause
SELECT CategoryId, AVG(Price) as AvgPrice
FROM Products
GROUP BY CategoryId
HAVING AVG(Price) > 50
```

**Joins:**
```sql
-- INNER JOIN
SELECT u.Name, o.OrderDate, o.Total
FROM Users u
INNER JOIN Orders o ON u.Id = o.UserId

-- LEFT OUTER JOIN
SELECT u.Name, o.OrderDate, o.Total
FROM Users u
LEFT OUTER JOIN Orders o ON u.Id = o.UserId

-- Multiple joins
SELECT 
    o.OrderId,
    u.Name as CustomerName,
    p.ProductName,
    od.Quantity
FROM Orders o
INNER JOIN Users u ON o.UserId = u.Id
INNER JOIN OrderDetails od ON o.OrderId = od.OrderId
INNER JOIN Products p ON od.ProductId = p.Id
```

**Subqueries:**
```sql
-- Subquery in WHERE
SELECT * FROM Products
WHERE CategoryId IN (SELECT Id FROM Categories WHERE Active = TRUE)

-- Subquery in SELECT
SELECT 
    Name,
    (SELECT COUNT(*) FROM Orders WHERE UserId = u.Id) as OrderCount
FROM Users u

-- Subquery in FROM
SELECT CategoryName, AvgPrice
FROM (
    SELECT c.Name as CategoryName, AVG(p.Price) as AvgPrice
    FROM Categories c
    INNER JOIN Products p ON c.Id = p.CategoryId
    GROUP BY c.Name
) as CategoryAverages
WHERE AvgPrice > 100
```

**Set Operations:**
```sql
-- UNION (removes duplicates)
SELECT Name FROM Customers
UNION
SELECT Name FROM Suppliers

-- UNION ALL (keeps duplicates)
SELECT Email FROM Users
UNION ALL
SELECT Email FROM Contacts

-- INTERSECT (returns common rows)
SELECT ProductId FROM OrderDetails WHERE OrderId = 1
INTERSECT
SELECT ProductId FROM OrderDetails WHERE OrderId = 2

-- EXCEPT/MINUS (returns rows in first query not in second)
SELECT Email FROM AllUsers
EXCEPT
SELECT Email FROM ActiveUsers
```

**SELECT INTO:**
```sql
-- Create new table from query
SELECT * INTO UserBackup FROM Users WHERE Active = TRUE

-- Create summary table
SELECT 
    CategoryId,
    COUNT(*) as ProductCount,
    AVG(Price) as AvgPrice
INTO CategorySummary
FROM Products
GROUP BY CategoryId
```

#### INSERT

Adds new rows to a table.

**Syntax:**
```sql
-- Insert with VALUES
INSERT INTO table_name [(column1, column2, ...)]
VALUES (value1, value2, ...)

-- Insert from SELECT
INSERT INTO table_name [(column1, column2, ...)]
SELECT columns FROM source_table [WHERE condition]
```

**Examples:**
```sql
-- Insert all columns
INSERT INTO Users VALUES (1, 'John Doe', 'john@example.com', TRUE)

-- Insert specific columns
INSERT INTO Users (Id, Name, Email)
VALUES (2, 'Jane Smith', 'jane@example.com')

-- Insert multiple rows (execute separately)
INSERT INTO Products VALUES (1, 'Widget', 29.99, TRUE)
INSERT INTO Products VALUES (2, 'Gadget', 49.99, TRUE)
INSERT INTO Products VALUES (3, 'Doohickey', 19.99, FALSE)

-- Insert from SELECT
INSERT INTO ArchivedOrders
SELECT * FROM Orders WHERE OrderDate < '2020-01-01'

-- Insert specific columns from SELECT
INSERT INTO UserSummary (UserId, OrderCount, TotalSpent)
SELECT 
    UserId,
    COUNT(*) as OrderCount,
    SUM(Total) as TotalSpent
FROM Orders
GROUP BY UserId
```

#### UPDATE

Modifies existing rows in a table.

**Syntax:**
```sql
UPDATE table_name
SET column1 = value1, column2 = value2, ...
[WHERE condition]
```

**Examples:**
```sql
-- Update single column
UPDATE Users
SET Email = 'newemail@example.com'
WHERE Id = 1

-- Update multiple columns
UPDATE Products
SET Price = 39.99, InStock = TRUE
WHERE ProductId = 10

-- Update with calculation
UPDATE Products
SET Price = Price * 1.10
WHERE CategoryId = 5

-- Update with subquery
UPDATE Users
SET TotalOrders = (
    SELECT COUNT(*) FROM Orders WHERE UserId = Users.Id
)

-- Update all rows (use with caution!)
UPDATE Products
SET InStock = FALSE
```

#### DELETE

Removes rows from a table.

**Syntax:**
```sql
DELETE FROM table_name
[WHERE condition]
```

**Examples:**
```sql
-- Delete specific row
DELETE FROM Users WHERE Id = 1

-- Delete with multiple conditions
DELETE FROM Orders
WHERE Status = 'Cancelled' AND OrderDate < '2020-01-01'

-- Delete with subquery
DELETE FROM OrderDetails
WHERE OrderId IN (
    SELECT OrderId FROM Orders WHERE Status = 'Cancelled'
)

-- Delete all rows (use with caution!)
DELETE FROM TempData
```

---

### Transaction Control

Transactions ensure data integrity by grouping multiple operations into a single atomic unit.

#### BEGIN TRANSACTION

**Syntax (via ADO.NET):**
```csharp
var transaction = connection.BeginTransaction();
```

**Syntax (via Native API):**
```sql
SET AUTOCOMMIT FALSE
```

#### COMMIT TRANSACTION

**Syntax (via ADO.NET):**
```csharp
transaction.Commit();
```

**Syntax (via Native API):**
```sql
COMMIT
```

#### ROLLBACK TRANSACTION

**Syntax (via ADO.NET):**
```csharp
transaction.Rollback();
```

**Syntax (via Native API):**
```sql
ROLLBACK
```

**Example:**
```csharp
using var connection = new SharpHsqlConnection("Initial Catalog=.;User Id=sa;Pwd=");
connection.Open();

using var transaction = connection.BeginTransaction();
try
{
    var cmd = connection.CreateCommand();
    
    cmd.CommandText = "UPDATE Accounts SET Balance = Balance - 100 WHERE Id = 1";
    cmd.ExecuteNonQuery();
    
    cmd.CommandText = "UPDATE Accounts SET Balance = Balance + 100 WHERE Id = 2";
    cmd.ExecuteNonQuery();
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

### Other Commands

#### DECLARE Variables

Declare and use variables in SQL scripts.

**Syntax:**
```sql
DECLARE @variable_name datatype
SET @variable_name = value
```

**Example:**
```sql
DECLARE @userId INT
SET @userId = 1

SELECT * FROM Orders WHERE UserId = @userId
```

#### CALL

Execute a function and return its result.

**Syntax:**
```sql
CALL function_name(arguments)
```

**Example:**
```sql
CALL USER()
CALL DATABASE()
CALL IDENTITY()
```

#### DISCONNECT

Disconnect the current session (Native API).

**Syntax:**
```sql
DISCONNECT
```

---

## Data Types

### Numeric Types

| Type | Size | Range | Description |
|------|------|-------|-------------|
| `TINYINT` | 1 byte | 0 to 255 | Unsigned 8-bit integer |
| `SMALLINT` | 2 bytes | -32,768 to 32,767 | 16-bit integer |
| `INTEGER` / `INT` | 4 bytes | -2,147,483,648 to 2,147,483,647 | 32-bit integer |
| `BIGINT` | 8 bytes | -2^63 to 2^63-1 | 64-bit integer |
| `DECIMAL(p,s)` | Variable | Depends on precision | Fixed-point decimal |
| `DOUBLE` | 8 bytes | ±1.7E±308 | Double-precision floating-point |
| `FLOAT` / `REAL` | 4 bytes | ±3.4E±38 | Single-precision floating-point |

**Examples:**
```sql
CREATE TABLE Numbers (
    TinyNum TINYINT,
    SmallNum SMALLINT,
    IntNum INTEGER,
    BigNum BIGINT,
    DecNum DECIMAL(10,2),
    DoubleNum DOUBLE,
    FloatNum FLOAT
)
```

### String Types

| Type | Description |
|------|-------------|
| `CHAR(n)` | Fixed-length character string |
| `VARCHAR(n)` | Variable-length character string |
| `LONGVARCHAR` | Long variable-length string |

**Examples:**
```sql
CREATE TABLE Strings (
    FixedCode CHAR(10),          -- Always 10 characters
    Name VARCHAR(100),            -- Up to 100 characters
    Description LONGVARCHAR       -- Large text
)
```

### Binary Types

| Type | Description |
|------|-------------|
| `BINARY` | Fixed-length binary data |
| `VARBINARY` | Variable-length binary data |
| `LONGVARBINARY` | Long variable-length binary data |

### Date and Time Types

| Type | Description | Format |
|------|-------------|--------|
| `DATE` | Date only | YYYY-MM-DD |
| `TIME` | Time only | HH:MM:SS |
| `TIMESTAMP` | Date and time | YYYY-MM-DD HH:MM:SS |

**Examples:**
```sql
CREATE TABLE Events (
    EventDate DATE,
    EventTime TIME,
    CreatedAt TIMESTAMP
)

INSERT INTO Events VALUES ('2026-03-27', '20:30:00', '2026-03-27 20:30:00')
```

### Boolean Type

| Type | Values |
|------|--------|
| `BIT` | `TRUE` or `FALSE` |

**Examples:**
```sql
CREATE TABLE Users (
    Id INT,
    Active BIT,
    IsAdmin BIT
)

INSERT INTO Users VALUES (1, TRUE, FALSE)
SELECT * FROM Users WHERE Active = TRUE
```

---

## Built-in Functions

### Mathematical Functions

#### ABS(n)
Returns the absolute value of a number.
```sql
SELECT ABS(-15)        -- Returns 15
SELECT ABS(42.5)       -- Returns 42.5
```

#### ACOS(n)
Returns the arc cosine (in radians).
```sql
SELECT ACOS(0.5)       -- Returns 1.0471975511966 (π/3)
```

#### ASIN(n)
Returns the arc sine (in radians).
```sql
SELECT ASIN(0.5)       -- Returns 0.523598775598299 (π/6)
```

#### ATAN(n)
Returns the arc tangent (in radians).
```sql
SELECT ATAN(1)         -- Returns 0.785398163397448 (π/4)
```

#### ATAN2(y, x)
Returns the arc tangent of y/x (in radians).
```sql
SELECT ATAN2(1, 1)     -- Returns 0.785398163397448
```

#### CEILING(n)
Returns the smallest integer >= n.
```sql
SELECT CEILING(42.1)   -- Returns 43
SELECT CEILING(-42.9)  -- Returns -42
```

#### COS(n)
Returns the cosine of n (in radians).
```sql
SELECT COS(0)          -- Returns 1
SELECT COS(PI())       -- Returns -1
```

#### COT(n)
Returns the cotangent of n.
```sql
SELECT COT(1)          -- Returns 0.642092615934331
```

#### DEGREES(n)
Converts radians to degrees.
```sql
SELECT DEGREES(PI())   -- Returns 180
```

#### EXP(n)
Returns e raised to the power of n.
```sql
SELECT EXP(1)          -- Returns 2.71828182845905 (e)
SELECT EXP(2)          -- Returns 7.38905609893065
```

#### FLOOR(n)
Returns the largest integer <= n.
```sql
SELECT FLOOR(42.9)     -- Returns 42
SELECT FLOOR(-42.1)    -- Returns -43
```

#### LOG(n)
Returns the natural logarithm of n.
```sql
SELECT LOG(2.71828)    -- Returns ~1 (ln(e))
```

#### LOG10(n)
Returns the base-10 logarithm of n.
```sql
SELECT LOG10(100)      -- Returns 2
SELECT LOG10(1000)     -- Returns 3
```

#### MOD(n, m)
Returns the remainder of n divided by m.
```sql
SELECT MOD(10, 3)      -- Returns 1
SELECT MOD(20, 7)      -- Returns 6
```

#### PI()
Returns the value of π (pi).
```sql
SELECT PI()            -- Returns 3.14159265358979
```

#### POWER(x, y) / POW(x, y)
Returns x raised to the power of y.
```sql
SELECT POWER(2, 3)     -- Returns 8
SELECT POW(10, 2)      -- Returns 100
```

#### RADIANS(n)
Converts degrees to radians.
```sql
SELECT RADIANS(180)    -- Returns 3.14159265358979 (π)
```

#### RAND(seed)
Returns a random number between 0 and 1.
```sql
SELECT RAND(0)         -- Returns random number
SELECT RAND(12345)     -- Returns random number with seed
```

#### ROUND(n, decimals)
Rounds n to specified decimal places.
```sql
SELECT ROUND(42.567, 2)    -- Returns 42.57
SELECT ROUND(42.567, 0)    -- Returns 43
SELECT ROUND(42.567, -1)   -- Returns 40
```

#### SIGN(n)
Returns the sign of n (-1, 0, or 1).
```sql
SELECT SIGN(-42)       -- Returns -1
SELECT SIGN(0)         -- Returns 0
SELECT SIGN(42)        -- Returns 1
```

#### SIN(n)
Returns the sine of n (in radians).
```sql
SELECT SIN(0)          -- Returns 0
SELECT SIN(PI()/2)     -- Returns 1
```

#### SQRT(n)
Returns the square root of n.
```sql
SELECT SQRT(16)        -- Returns 4
SELECT SQRT(2)         -- Returns 1.41421356237309
```

#### TAN(n)
Returns the tangent of n (in radians).
```sql
SELECT TAN(0)          -- Returns 0
SELECT TAN(PI()/4)     -- Returns 1
```

#### TRUNCATE(n, decimals)
Truncates n to specified decimal places.
```sql
SELECT TRUNCATE(42.567, 2)  -- Returns 42.56
SELECT TRUNCATE(42.567, 0)  -- Returns 42
```

#### BITAND(n, m)
Performs bitwise AND operation.
```sql
SELECT BITAND(12, 10)  -- Returns 8 (1100 & 1010 = 1000)
```

#### BITOR(n, m)
Performs bitwise OR operation.
```sql
SELECT BITOR(12, 10)   -- Returns 14 (1100 | 1010 = 1110)
```

---

### String Functions

#### ASCII(str)
Returns the ASCII code of the first character.
```sql
SELECT ASCII('A')      -- Returns 65
SELECT ASCII('Hello')  -- Returns 72
```

#### CHAR(n) / CHARACTER(n)
Returns the character for ASCII code n.
```sql
SELECT CHAR(65)        -- Returns 'A'
SELECT CHAR(72)        -- Returns 'H'
```

#### CONCAT(str1, str2)
Concatenates two strings.
```sql
SELECT CONCAT('Hello', ' World')  -- Returns 'Hello World'
SELECT 'Hello' || ' World'        -- Returns 'Hello World' (alternative)
```

#### DIFFERENCE(str1, str2)
Returns the difference in SOUNDEX values.
```sql
SELECT DIFFERENCE('Smith', 'Smythe')  -- Returns similarity score
```

#### INSERT(str, start, length, newstr)
Inserts newstr into str at position start, replacing length characters.
```sql
SELECT INSERT('Hello World', 7, 5, 'There')  -- Returns 'Hello There'
```

#### LCASE(str) / LOWER(str)
Converts string to lowercase.
```sql
SELECT LCASE('HELLO')     -- Returns 'hello'
SELECT LOWER('WORLD')     -- Returns 'world'
```

#### LEFT(str, length)
Returns leftmost length characters.
```sql
SELECT LEFT('Hello World', 5)  -- Returns 'Hello'
```

#### LENGTH(str)
Returns the length of a string.
```sql
SELECT LENGTH('Hello')    -- Returns 5
SELECT LENGTH('')         -- Returns 0
```

#### LOCATE(search, str, start)
Returns the position of search in str (starting from start).
```sql
SELECT LOCATE('o', 'Hello World', 1)   -- Returns 5
SELECT LOCATE('o', 'Hello World', 6)   -- Returns 8
```

#### LTRIM(str)
Removes leading spaces.
```sql
SELECT LTRIM('  Hello')   -- Returns 'Hello'
```

#### REPEAT(str, count)
Repeats string count times.
```sql
SELECT REPEAT('*', 5)     -- Returns '*****'
```

#### REPLACE(str, from, to)
Replaces all occurrences of from with to.
```sql
SELECT REPLACE('Hello World', 'World', 'There')  -- Returns 'Hello There'
```

#### RIGHT(str, length)
Returns rightmost length characters.
```sql
SELECT RIGHT('Hello World', 5)  -- Returns 'World'
```

#### RTRIM(str)
Removes trailing spaces.
```sql
SELECT RTRIM('Hello  ')   -- Returns 'Hello'
```

#### SOUNDEX(str)
Returns the SOUNDEX code of a string.
```sql
SELECT SOUNDEX('Smith')   -- Returns 'S530'
SELECT SOUNDEX('Smythe')  -- Returns 'S530'
```

#### SPACE(n)
Returns a string of n spaces.
```sql
SELECT SPACE(5)           -- Returns '     '
SELECT 'A' || SPACE(3) || 'B'  -- Returns 'A   B'
```

#### SUBSTRING(str, start, length)
Extracts a substring.
```sql
SELECT SUBSTRING('Hello World', 7, 5)  -- Returns 'World'
SELECT SUBSTRING('Hello World', 7, 0)  -- Returns 'World' (to end)
```

#### TRIM(str, trimchar, leading, trailing)
Removes characters from string.
```sql
SELECT TRIM('  Hello  ', ' ', TRUE, TRUE)   -- Returns 'Hello'
SELECT TRIM('  Hello  ', ' ', TRUE, FALSE)  -- Returns 'Hello  '
```

#### UCASE(str) / UPPER(str)
Converts string to uppercase.
```sql
SELECT UCASE('hello')     -- Returns 'HELLO'
SELECT UPPER('world')     -- Returns 'WORLD'
```

---

### Date and Time Functions

#### CURDATE()
Returns the current date.
```sql
SELECT CURDATE()          -- Returns current date (e.g., '2026-03-27')
```

#### CURTIME()
Returns the current time.
```sql
SELECT CURTIME()          -- Returns current time (e.g., '20:30:45')
```

#### NOW()
Returns the current date and time.
```sql
SELECT NOW()              -- Returns current timestamp
```

#### DATEDIFF(datepart, date1, date2)
Returns the difference between two dates.

**Date parts:** `year`/`yy`, `month`/`mm`, `day`/`dd`, `hour`/`hh`, `minute`/`mi`, `second`/`ss`, `millisecond`/`ms`

```sql
SELECT DATEDIFF('day', '2026-01-01', '2026-03-27')      -- Returns days difference
SELECT DATEDIFF('month', '2026-01-01', '2026-03-27')    -- Returns months difference
SELECT DATEDIFF('year', '2020-01-01', '2026-03-27')     -- Returns years difference
```

#### DAY(date) / DAYOFMONTH(date)
Returns the day of the month (1-31).
```sql
SELECT DAY('2026-03-27')          -- Returns 27
SELECT DAYOFMONTH('2026-03-27')   -- Returns 27
```

#### DAYNAME(date)
Returns the name of the day.
```sql
SELECT DAYNAME('2026-03-27')      -- Returns day name
```

#### DAYOFWEEK(date)
Returns the day of week (0-6, where 0 is Sunday).
```sql
SELECT DAYOFWEEK('2026-03-27')    -- Returns day number
```

#### DAYOFYEAR(date)
Returns the day of the year (1-366).
```sql
SELECT DAYOFYEAR('2026-03-27')    -- Returns day number in year
```

#### HOUR(time)
Returns the hour (0-23).
```sql
SELECT HOUR('20:30:45')           -- Returns 20
SELECT HOUR(NOW())                -- Returns current hour
```

#### MINUTE(time)
Returns the minute (0-59).
```sql
SELECT MINUTE('20:30:45')         -- Returns 30
```

#### MONTH(date)
Returns the month (1-12).
```sql
SELECT MONTH('2026-03-27')        -- Returns 3
```

#### MONTHNAME(date)
Returns the name of the month.
```sql
SELECT MONTHNAME('2026-03-27')    -- Returns 'March'
```

#### QUARTER(date)
Returns the quarter (1-4).
```sql
SELECT QUARTER('2026-03-27')      -- Returns 1
SELECT QUARTER('2026-07-15')      -- Returns 3
```

#### SECOND(time)
Returns the second (0-59).
```sql
SELECT SECOND('20:30:45')         -- Returns 45
```

#### WEEK(date)
Returns the week of the year.
```sql
SELECT WEEK('2026-03-27')         -- Returns week number
```

#### YEAR(date)
Returns the year.
```sql
SELECT YEAR('2026-03-27')         -- Returns 2026
```

---

### System Functions

#### DATABASE()
Returns the current database name.
```sql
SELECT DATABASE()         -- Returns database name
```

#### USER()
Returns the current user name.
```sql
SELECT USER()             -- Returns 'sa' or current user
```

#### IDENTITY()
Returns the last auto-generated identity value.
```sql
INSERT INTO Users VALUES (NULL, 'Auto ID', 'auto@example.com')
SELECT IDENTITY()         -- Returns last inserted ID
```

#### EXISTS(table_name)
Checks if a table exists.
```sql
SELECT EXISTS('Users')    -- Returns TRUE if table exists
```

---

### Aggregate Functions

#### COUNT(*)
Counts the number of rows.
```sql
SELECT COUNT(*) FROM Users
SELECT COUNT(*) FROM Orders WHERE Status = 'Pending'
```

#### COUNT(column)
Counts non-NULL values in a column.
```sql
SELECT COUNT(Email) FROM Users
SELECT COUNT(DISTINCT CategoryId) FROM Products
```

#### SUM(column)
Returns the sum of values.
```sql
SELECT SUM(Total) FROM Orders
SELECT SUM(Quantity * Price) FROM OrderDetails
```

#### AVG(column)
Returns the average of values.
```sql
SELECT AVG(Price) FROM Products
SELECT AVG(Total) FROM Orders WHERE Status = 'Completed'
```

#### MIN(column)
Returns the minimum value.
```sql
SELECT MIN(Price) FROM Products
SELECT MIN(OrderDate) FROM Orders
```

#### MAX(column)
Returns the maximum value.
```sql
SELECT MAX(Price) FROM Products
SELECT MAX(OrderDate) FROM Orders
```

**Examples with GROUP BY:**
```sql
SELECT 
    CategoryId,
    COUNT(*) as ProductCount,
    AVG(Price) as AvgPrice,
    MIN(Price) as MinPrice,
    MAX(Price) as MaxPrice,
    SUM(Stock) as TotalStock
FROM Products
GROUP BY CategoryId
```

---

### Conversion Functions

#### CONVERT(expression, datatype)
Converts an expression to a specified data type.
```sql
SELECT CONVERT('123', INTEGER)        -- Returns 123
SELECT CONVERT(123.45, INTEGER)       -- Returns 123
SELECT CONVERT('2026-03-27', DATE)    -- Returns date
```

#### CAST(expression AS datatype)
SQL standard conversion function.
```sql
SELECT CAST('123' AS INTEGER)         -- Returns 123
SELECT CAST(123.45 AS INTEGER)        -- Returns 123
SELECT CAST('2026-03-27' AS DATE)     -- Returns date
```

#### IFNULL(expression, replacement)
Returns replacement if expression is NULL.
```sql
SELECT IFNULL(Email, 'No Email') FROM Users
SELECT IFNULL(Price, 0) FROM Products
```

#### CASEWHEN(condition, true_value, false_value)
Returns true_value if condition is true, otherwise false_value.
```sql
SELECT CASEWHEN(Price > 100, 'Expensive', 'Affordable') FROM Products
SELECT CASEWHEN(Stock > 0, 'In Stock', 'Out of Stock') FROM Products
```

---

## ADO.NET Provider API

### SharpHsqlConnection

Represents a connection to a SharpHSQL database.

**Constructor:**
```csharp
var connection = new SharpHsqlConnection();
var connection = new SharpHsqlConnection(connectionString);
```

**Properties:**
- `ConnectionString` - Gets or sets the connection string
- `State` - Gets the current state of the connection
- `Database` - Gets the name of the current database

**Methods:**
- `Open()` - Opens the connection
- `Close()` - Closes the connection
- `CreateCommand()` - Creates a new SharpHsqlCommand
- `BeginTransaction()` - Starts a new transaction
- `BeginTransaction(IsolationLevel)` - Starts a transaction with isolation level

**Example:**
```csharp
using var connection = new SharpHsqlConnection("Initial Catalog=mydb;User Id=sa;Pwd=");
connection.Open();
Console.WriteLine($"Connected to: {connection.Database}");
```

### SharpHsqlCommand

Represents a SQL statement to execute.

**Constructor:**
```csharp
var cmd = new SharpHsqlCommand();
var cmd = new SharpHsqlCommand(sql, connection);
```

**Properties:**
- `CommandText` - Gets or sets the SQL statement
- `Connection` - Gets or sets the connection
- `Parameters` - Gets the parameter collection

**Methods:**
- `ExecuteNonQuery()` - Executes command, returns rows affected
- `ExecuteScalar()` - Executes command, returns first column of first row
- `ExecuteReader()` - Executes command, returns a data reader

**Example:**
```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM Users";
int count = (int)cmd.ExecuteScalar();
```

### SharpHsqlParameter

Represents a parameter for a SharpHsqlCommand.

**Constructor:**
```csharp
var param = new SharpHsqlParameter(
    "@name",                    // Parameter name
    DbType.String,              // Data type
    0,                          // Size
    ParameterDirection.Input,   // Direction
    false,                      // IsNullable
    0,                          // Precision
    0,                          // Scale
    null,                       // Source column
    DataRowVersion.Current,     // Source version
    "value"                     // Value
);
```

**Example:**
```csharp
cmd.Parameters.Add(new SharpHsqlParameter("@id", DbType.Int32, 0, 
    ParameterDirection.Input, false, 0, 0, null, DataRowVersion.Current, 1));
```

### SharpHsqlReader

Provides forward-only access to query results.

**Methods:**
- `Read()` - Advances to the next record
- `GetInt32(ordinal)` - Gets integer value
- `GetString(ordinal)` - Gets string value
- `GetDecimal(ordinal)` - Gets decimal value
- `GetBoolean(ordinal)` - Gets boolean value
- `GetDateTime(ordinal)` - Gets date/time value
- `GetValue(ordinal)` - Gets value as object
- `IsDBNull(ordinal)` - Checks if value is NULL
- `GetName(ordinal)` - Gets column name
- `Close()` - Closes the reader

**Example:**
```csharp
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    int id = reader.GetInt32(0);
    string name = reader.GetString(1);
    Console.WriteLine($"{id}: {name}");
}
```

### SharpHsqlTransaction

Represents a database transaction.

**Methods:**
- `Commit()` - Commits the transaction
- `Rollback()` - Rolls back the transaction

**Example:**
```csharp
using var transaction = connection.BeginTransaction();
try
{
    // Execute commands
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### SharpHsqlDataAdapter

Fills DataSets and updates data sources.

**Constructor:**
```csharp
var adapter = new SharpHsqlDataAdapter(sql, connection);
var adapter = new SharpHsqlDataAdapter(command);
```

**Methods:**
- `Fill(DataSet)` - Fills a DataSet
- `Fill(DataTable)` - Fills a DataTable

**Example:**
```csharp
var adapter = new SharpHsqlDataAdapter("SELECT * FROM Users", connection);
var dataSet = new DataSet();
adapter.Fill(dataSet);
```

---

## Native API Reference

### Database

Represents a SharpHSQL database instance.

**Constructor:**
```csharp
var database = new Database(path);
```

**Parameters:**
- `path`: `"."` for in-memory, or file path for persistent database

**Methods:**
- `Connect(username, password)` - Opens a connection, returns Channel
- `Execute(sql, channel)` - Executes SQL, returns Result

**Example:**
```csharp
var database = new Database("./mydb");
var channel = database.Connect("sa", "");
var result = database.Execute("SELECT * FROM Users", channel);
```

### Channel

Represents a connection session.

**Properties:**
- `Database` - Gets the database instance
- `UserName` - Gets the connected user name
- `LastIdentity` - Gets last auto-generated ID
- `MaxRows` - Gets/sets max rows to return

**Example:**
```csharp
var channel = database.Connect("sa", "");
Console.WriteLine($"User: {channel.UserName}");
```

### Result

Contains the results of a query.

**Properties:**
- `Root` - Gets the first Record
- `ColumnCount` - Gets number of columns
- `Error` - Gets error message (if any)
- `UpdateCount` - Gets rows affected (for INSERT/UPDATE/DELETE)

**Methods:**
- `Size` - Gets number of rows

**Example:**
```csharp
var result = database.Execute("SELECT * FROM Users", channel);
var record = result.Root;
while (record != null)
{
    var row = record.Data;
    Console.WriteLine($"ID: {row[0]}, Name: {row[1]}");
    record = record.Next;
}
```

---

## Connection Strings

### Format
```
Initial Catalog=database_path;User Id=username;Pwd=password
```

### Examples
```csharp
// In-memory database
"Initial Catalog=.;User Id=sa;Pwd="

// File-based (relative path)
"Initial Catalog=./mydb;User Id=sa;Pwd="

// File-based (absolute path - Windows)
"Initial Catalog=C:\\Data\\mydb;User Id=sa;Pwd="

// File-based (absolute path - Unix/Mac)
"Initial Catalog=/var/data/mydb;User Id=sa;Pwd="
```

---

## Error Handling

### ADO.NET Provider

```csharp
try
{
    using var connection = new SharpHsqlConnection(connectionString);
    connection.Open();
    
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT * FROM NonExistentTable";
    using var reader = cmd.ExecuteReader();
}
catch (SharpHsqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
    foreach (SharpHsqlError error in ex.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"General error: {ex.Message}");
}
```

### Native API

```csharp
var result = database.Execute(sql, channel);

if (result.Error != null && !string.IsNullOrEmpty(result.Error))
{
    throw new Exception($"SQL Error: {result.Error}");
}
```

---

## Best Practices

1. **Always use parameters** for user input to prevent SQL injection
2. **Use transactions** for multiple related operations
3. **Dispose resources** with `using` statements
4. **Create indexes** on frequently queried columns
5. **Use connection pooling** implicitly (handled by SharpHSQL)
6. **Test with in-memory databases** for unit tests
7. **Use appropriate data types** to optimize storage
8. **Handle NULL values** explicitly in queries
9. **Batch operations** within transactions for better performance
10. **Monitor file-based database** size and performance

---

For more examples, see the [Quick Start Guide](QuickStart.md) and the [test suite](../SharpHSQL.Tests/).
