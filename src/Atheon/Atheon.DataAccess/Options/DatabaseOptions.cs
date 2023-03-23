namespace Atheon.DataAccess.Options;

public class DatabaseOptions
{
    public static class SQLiteTypes
    {
        public static class INTEGER
        {
            public const string DEFAULT_VALUE = "INTEGER";
            public const string INT = "INT";
            public const string TINYINT = "TINYINT";
            public const string SMALLINT = "SMALLINT";
            public const string MEDIUMINT = "MEDIUMINT";
            public const string BIGINT = "BIGINT";
            public const string UNSIGNED_BIG_INT = "UNSIGNED BIG INT";
            public const string INT2 = "INT2";
            public const string INT8 = "INT8";
        }

        public static class TEXT
        {
            public const string DEFAULT_VALUE = "TEXT";
            public const string CHARACTER_20 = "CHARACTER(20)";
            public const string VARCHAR_255 = "VARCHAR(255)";
            public const string VARYING_CHARACTER_255 = "VARYING CHARACTER(255)";
            public const string NCHAR_55 = "NCHAR(55)";
            public const string NATIVE_CHARACTER_70 = "NATIVE CHARACTER(70)";
            public const string NVARCHAR_100 = "NVARCHAR(100)";
            public const string CLOB = "CLOB";
        }

        public const string BLOB = "BLOB";

        public class REAL
        {
            public const string DEFAULT_VALUE = "REAL";
            public const string DOUBLE = "DOUBLE";
            public const string DOUBLE_PRECISION = "DOUBLE PRECISION";
            public const string FLOAT = "FLOAT";
        }

        public class NUMERIC
        {
            public const string DEFAULT_VALUE = "NUMERIC";
            public const string DECIMAL_10_5 = "DECIMAL(10,5)";
            public const string BOOLEAN = "BOOLEAN";
            public const string DATE = "DATE";
            public const string DATETIME = "DATETIME";
        }
    }
    public const string SqliteKey = "Sqlite";

    public Dictionary<string, DatabaseOptionsEntry> Databases { get; set; }
    public Dictionary<string, DatabaseTableEntry> Tables { get; set; }
}
