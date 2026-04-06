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
        public string ServiceType { get; }


        public DuplicatePhoneException(string phone, string serviceType)
            : base($"Phone already exists for service type '{serviceType}': {phone}")
        {
            Phone = phone;
            ServiceType = serviceType;
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
                                             Phone TEXT NOT NULL,
                                             ServiceType TEXT NOT NULL,
                                             CreatedUtc TEXT NOT NULL,
                                             PRIMARY KEY (Phone, ServiceType)
                                         );
                                         """, ct);
        }

        private static string Normalize(string? raw)
        {
            var digits =  string.IsNullOrWhiteSpace(raw) ? string.Empty : new string(raw.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits))
                return string.Empty;
            
            if (digits.StartsWith("0"))
                return "972" + digits[1..];
            
            return digits;
        }

        public async Task<bool> ExistsAsync(string phoneRaw, string serviceType, CancellationToken ct = default)
        {
            var phone = Normalize(phoneRaw);
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            if (string.IsNullOrWhiteSpace(serviceType))
                throw new ArgumentException("Service type is required.", nameof(serviceType));

            await using var conn = await _db.OpenConnectionAsync(ct);

            var result = await _db.ScalarAsync(
                conn,
                """
                SELECT 1
                FROM Phones
                WHERE Phone = $phone AND ServiceType = $serviceType
                LIMIT 1;
                """,
                ct,
                [
                    ("$phone", phone),
                    ("$serviceType", serviceType)
                ]);

            return result != null;
        }

        /// <summary>
        /// Inserts the phone. Throws DuplicatePhoneException if already exists.
        /// </summary>
        public async Task InsertAsync(string phoneRaw, string serviceType, CancellationToken ct = default)
        {
            var phone = Normalize(phoneRaw);
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone is empty/invalid after normalization.", nameof(phoneRaw));

            if (string.IsNullOrWhiteSpace(serviceType))
                throw new ArgumentException("Service type is required.", nameof(serviceType));

            await using var conn = await _db.OpenConnectionAsync(ct);

            try
            {
                await _db.ExecuteAsync(conn, """
                                             INSERT INTO Phones (Phone, ServiceType, CreatedUtc)
                                             VALUES ($phone, $serviceType, $createdUtc);
                                             """,
                    ct,
                    [
                        ("$phone", phone),
                        ("$serviceType", serviceType),
                        ("$createdUtc", DateTime.UtcNow.ToString("O"))
                    ]);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new DuplicatePhoneException(phone, serviceType);
            }
        }
    }
}
