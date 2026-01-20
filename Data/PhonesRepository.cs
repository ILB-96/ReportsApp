using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reports.Data
{
    public sealed class DuplicatePhoneException : Exception
    {
        public string Phone { get; }

        public DuplicatePhoneException(string phone)
            : base($"Phone already exists: {phone}")
        {
            Phone = phone;
        }
    }

    public sealed class PhonesRepository
    {
        private readonly AppDb _db;

        public PhonesRepository(AppDb db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            await _db.InitializeAsync(ct);

            await using var conn = await _db.OpenConnectionAsync(ct);
            await _db.ExecuteAsync(conn, """
                                         CREATE TABLE IF NOT EXISTS Phones (
                                             Phone TEXT NOT NULL PRIMARY KEY,
                                             CreatedUtc TEXT NOT NULL
                                         );
                                         """, ct);
        }

        public static string Normalize(string? raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : new string(raw.Where(char.IsDigit).ToArray());
        }

        public async Task<bool> ExistsAsync(string phoneRaw, CancellationToken ct = default)
        {
            var phone = Normalize(phoneRaw);
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            await using var conn = await _db.OpenConnectionAsync(ct);

            var result = await _db.ScalarAsync(
                conn,
                "SELECT 1 FROM Phones WHERE Phone = $phone LIMIT 1;",
                ct,
                [("$phone", phone)]);

            return result != null;
        }

        /// <summary>
        /// Inserts the phone. Throws DuplicatePhoneException if already exists.
        /// </summary>
        public async Task InsertAsync(string phoneRaw, CancellationToken ct = default)
        {
            var phone = Normalize(phoneRaw);
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone is empty/invalid after normalization.", nameof(phoneRaw));

            await using var conn = await _db.OpenConnectionAsync(ct);

            try
            {
                await _db.ExecuteAsync(conn, """
                                             INSERT INTO Phones (Phone, CreatedUtc)
                                             VALUES ($phone, $createdUtc);
                                             """,
                    ct,
                    [
                        ("$phone", phone),
                        ("$createdUtc", DateTime.UtcNow.ToString("O"))
                    ]);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                throw new DuplicatePhoneException(phone);
            }
        }
    }
}
