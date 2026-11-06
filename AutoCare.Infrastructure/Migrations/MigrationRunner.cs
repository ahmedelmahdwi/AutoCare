using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AutoCare.Infrastructure.Migrations
{
    public class MigrationRunner
    {
        private readonly string _connectionString;

        public MigrationRunner(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public void ApplyMigrations(IEnumerable<Tuple<string, string>> migrationScripts)
        {
            if (string.IsNullOrWhiteSpace(_connectionString)) return;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Ensure __SchemaMigrationsHistory table exists
                string initHistoryTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__SchemaMigrationsHistory')
                    BEGIN
                        CREATE TABLE __SchemaMigrationsHistory (
                            MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY,
                            AppliedAtUtc DATETIME NOT NULL DEFAULT GETUTCDATE(),
                            ScriptName NVARCHAR(250) NOT NULL
                        );
                    END";

                using (var cmd = new SqlCommand(initHistoryTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // 2. Fetch already applied migration IDs
                var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SqlCommand("SELECT MigrationId FROM __SchemaMigrationsHistory", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        applied.Add(reader.GetString(0));
                    }
                }

                // 3. Apply pending migrations inside a transaction
                foreach (var script in migrationScripts)
                {
                    string id = script.Item1;
                    string sql = script.Item2;

                    if (applied.Contains(id))
                        continue;

                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = new SqlCommand(sql, conn, trans))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            using (var logCmd = new SqlCommand("INSERT INTO __SchemaMigrationsHistory (MigrationId, ScriptName) VALUES (@Id, @Name)", conn, trans))
                            {
                                logCmd.Parameters.AddWithValue("@Id", id);
                                logCmd.Parameters.AddWithValue("@Name", id);
                                logCmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
        }
    }
}
