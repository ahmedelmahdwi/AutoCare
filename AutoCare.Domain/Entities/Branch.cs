using System;
using AutoCare.Domain.Common;

namespace AutoCare.Domain.Entities
{
    public class Branch : Entity<int>, IAggregateRoot
    {
        public new int Id { get => base.Id; set => base.Id = value; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string DisplayName => string.IsNullOrWhiteSpace(City) 
            ? BranchName 
            : $"{BranchName} ({City})";

        public override string ToString() => DisplayName;
    }
}
