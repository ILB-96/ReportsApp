using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Reports.Data
{
    public sealed class AppDb
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public AppDb(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

            _connectionString = $"Data Source={_dbPath};Cache=Shared";
        }

        public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }

        /// <summary>
        /// Runs schema creation once per app lifetime.
        /// Add future CREATE TABLE statements here or via repositories.
        /// </summary>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            await _initLock.WaitAsync(ct);
            try
            {
                if (_initialized) return;

                await using var conn = await OpenConnectionAsync(ct);

                // Pragmas: safe defaults for a local desktop app
                await ExecuteAsync(conn, "PRAGMA foreign_keys = ON;", ct);
                await ExecuteAsync(conn, "PRAGMA journal_mode = WAL;", ct);

                // (Optional) create a schema version table for future migrations
                await ExecuteAsync(conn, @"
                                                CREATE TABLE IF NOT EXISTS __schema_version (
                                                    Version INTEGER NOT NULL PRIMARY KEY
                                                );", ct);

                // Put core tables here if you like, or call repo.EnsureCreatedAsync from composition root

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<int> ExecuteAsync(SqliteConnection conn, string sql, CancellationToken ct = default,
            (string, string)[]? parameters = null)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (parameters == null) return await cmd.ExecuteNonQueryAsync(ct);
            
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<object?> ScalarAsync(SqliteConnection conn, string sql, CancellationToken ct = default,
            (string, string phone)[]? parameters = null)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (parameters != null)
            {
                foreach (var (name, value) in parameters)
                    cmd.Parameters.AddWithValue(name, (object?)value ?? DBNull.Value);
            }

            return await cmd.ExecuteScalarAsync(ct);
        }
    }
}
