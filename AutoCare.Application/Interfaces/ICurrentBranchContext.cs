using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCare.Domain.Entities;

namespace AutoCare.Application.Interfaces
{
    public interface ICurrentBranchContext
    {
        int? CurrentBranchId { get; set; }
        string CurrentBranchName { get; }
        bool IsAllBranchesSelected { get; }
        bool IsAllBranches { get; }
        void SetBranch(int? branchId, string branchName = null);
        Task SetBranchAsync(int? branchId);
        event Action BranchChanged;
    }
}
