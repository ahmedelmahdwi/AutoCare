using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Core.Services
{
    public class CurrentBranchContext : ICurrentBranchContext
    {
        private readonly IDbHelper _dbHelper;
        private int? _currentBranchId = null;
        private string _currentBranchName = "كل الفروع (مجمّع)";

        public event Action BranchChanged;

        public int? CurrentBranchId
        {
            get => _currentBranchId;
            set
            {
                if (_currentBranchId != value)
                {
                    _currentBranchId = value;
                    _ = UpdateBranchNameAsync();
                    BranchChanged?.Invoke();
                }
            }
        }

        public string CurrentBranchName => _currentBranchName;

        public bool IsAllBranchesSelected => !_currentBranchId.HasValue || _currentBranchId.Value <= 0;
        public bool IsAllBranches => IsAllBranchesSelected;

        public CurrentBranchContext(IDbHelper dbHelper = null)
        {
            _dbHelper = dbHelper ?? AppServiceLocator.DbHelper;
        }

        public void SetBranch(int? branchId, string branchName = null)
        {
            _currentBranchId = (branchId.HasValue && branchId.Value > 0) ? branchId : null;
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                _currentBranchName = branchName;
            }
            else if (IsAllBranchesSelected)
            {
                _currentBranchName = "كل الفروع (مجمّع)";
            }
            else
            {
                _currentBranchName = $"فرع {_currentBranchId}";
            }
            BranchChanged?.Invoke();
        }

        public async Task SetBranchAsync(int? branchId)
        {
            if (_currentBranchId != branchId)
            {
                _currentBranchId = branchId;
                await UpdateBranchNameAsync();
                BranchChanged?.Invoke();
            }
        }

        public async Task<IReadOnlyList<Branch>> GetAllBranchesAsync()
        {
            var list = new List<Branch>();
            string sql = "SELECT * FROM Branches WHERE IsActive = 1 ORDER BY Id ASC";

            try
            {
                string connStr = _dbHelper?.GetConnectionString() ?? AppServiceLocator.DbHelper?.GetConnectionString();
                if (string.IsNullOrEmpty(connStr)) return list;
                using (var conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new Branch
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                BranchCode = reader["BranchCode"]?.ToString() ?? "",
                                BranchName = reader["BranchName"]?.ToString() ?? "",
                                City = reader["City"]?.ToString() ?? "",
                                Address = reader["Address"] is DBNull ? "" : reader["Address"]?.ToString() ?? "",
                                PhoneNumber = reader["PhoneNumber"] is DBNull ? "" : reader["PhoneNumber"]?.ToString() ?? "",
                                ManagerName = reader["ManagerName"] is DBNull ? "" : reader["ManagerName"]?.ToString() ?? "",
                                IsActive = reader["IsActive"] is DBNull || Convert.ToBoolean(reader["IsActive"]),
                                CreatedAt = reader["CreatedAt"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }
            catch
            {
                // Fallback default
                if (list.Count == 0)
                {
                    list.Add(new Branch { Id = 1, BranchCode = "BR001", BranchName = "الفرع الرئيسي", City = "طرابلس" });
                }
            }

            return list;
        }

        private async Task UpdateBranchNameAsync()
        {
            if (IsAllBranchesSelected)
            {
                _currentBranchName = "كل الفروع (مجمّع)";
                return;
            }

            string sql = "SELECT BranchName, City FROM Branches WHERE Id = @Id";
            try
            {
                string connStr = _dbHelper?.GetConnectionString() ?? AppServiceLocator.DbHelper?.GetConnectionString();
                if (string.IsNullOrEmpty(connStr)) return;
                using (var conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", _currentBranchId.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string name = reader["BranchName"]?.ToString() ?? "الفرع";
                                string city = reader["City"]?.ToString() ?? "";
                                _currentBranchName = string.IsNullOrWhiteSpace(city) ? name : $"{name} ({city})";
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore fallback
            }

            _currentBranchName = $"فرع {_currentBranchId}";
        }
    }
}
