using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace SmartParking.Api.Data
{
    public sealed class ParkingDb
    {
        private static readonly object WriteGate = new object();
        private readonly string _connectionString;

        public string DatabasePath { get; }

        public ParkingDb(string databasePath)
        {
            DatabasePath = databasePath;
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? ".");
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databasePath,
                ForeignKeys = true,
                JournalMode = SQLiteJournalModeEnum.Wal,
                Version = 3,
                DefaultTimeout = 5
            };
            _connectionString = builder.ToString();
        }

        public SQLiteConnection Open()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }
            return connection;
        }

        public static SQLiteCommand Command(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params object[] args)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            for (var i = 0; i < args.Length; i++)
            {
                command.Parameters.AddWithValue("@p" + i, args[i] ?? DBNull.Value);
            }
            return command;
        }

        public List<T> Query<T>(string sql, Func<IDataRecord, T> map, params object[] args)
        {
            using (var connection = Open())
            using (var command = Command(connection, null, sql, args))
            using (var reader = command.ExecuteReader())
            {
                var rows = new List<T>();
                while (reader.Read())
                {
                    rows.Add(map(reader));
                }
                return rows;
            }
        }

        public T QuerySingle<T>(string sql, Func<IDataRecord, T> map, params object[] args)
        {
            using (var connection = Open())
            using (var command = Command(connection, null, sql, args))
            using (var reader = command.ExecuteReader())
            {
                return reader.Read() ? map(reader) : default;
            }
        }

        public object Scalar(string sql, params object[] args)
        {
            using (var connection = Open())
            using (var command = Command(connection, null, sql, args))
            {
                return command.ExecuteScalar();
            }
        }

        public int Execute(string sql, params object[] args)
        {
            lock (WriteGate)
            {
                using (var connection = Open())
                using (var command = Command(connection, null, sql, args))
                {
                    return command.ExecuteNonQuery();
                }
            }
        }

        public T Write<T>(Func<SQLiteConnection, SQLiteTransaction, T> work)
        {
            lock (WriteGate)
            {
                using (var connection = Open())
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var result = work(connection, transaction);
                        transaction.Commit();
                        return result;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static long LastInsertId(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            using (var command = Command(connection, transaction, "SELECT last_insert_rowid()"))
            {
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        public static T QueryOne<T>(SQLiteConnection connection, SQLiteTransaction transaction, string sql, Func<IDataRecord, T> map, params object[] args)
        {
            using (var command = Command(connection, transaction, sql, args))
            using (var reader = command.ExecuteReader())
            {
                return reader.Read() ? map(reader) : default;
            }
        }

        public static int Exec(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params object[] args)
        {
            using (var command = Command(connection, transaction, sql, args))
            {
                return command.ExecuteNonQuery();
            }
        }

        public static object Scalar(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params object[] args)
        {
            using (var command = Command(connection, transaction, sql, args))
            {
                return command.ExecuteScalar();
            }
        }
    }
}
