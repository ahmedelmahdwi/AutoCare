using System;
using System.Collections.Generic;

namespace C__project_Auto_Care.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Vehicle
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string Vin { get; set; }
        public string CustomerPhone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class RepairOrder
    {
        public int Id { get; set; }
        public string RepairOrderNumber { get; set; }
        public string PlateNumber { get; set; }
        public DateTime EntryDate { get; set; } = DateTime.Now;
        public DateTime? ExpectedDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string Status { get; set; } = "معلق"; // معلق, قيد الصيانة, مكتمل, ملغي
        public string Receptionist { get; set; }
        public string Complaint { get; set; }
        public int? Mileage { get; set; }
        public string FuelLevel { get; set; }
        public string InspectionNotes { get; set; }
    }

    public class RepairOrderServiceItem
    {
        public int Id { get; set; }
        public string RepairOrderNumber { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public int? TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public int WarrantyDays { get; set; }
        public string Notes { get; set; }
    }

    public class RepairOrderSparePartItem
    {
        public int Id { get; set; }
        public string RepairOrderNumber { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int WarrantyDays { get; set; }
    }

    public class ServiceItem
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public int WarrantyDays { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SparePartItem
    {
        public int Id { get; set; }
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public int QuantityInStock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int WarrantyDays { get; set; }
        public int MinStockLevel { get; set; } = 5;
    }

    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string RepairOrderNumber { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal ServicesTotal { get; set; }
        public decimal SparePartsTotal { get; set; }
        public decimal SubTotal => ServicesTotal + SparePartsTotal;
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentMethod { get; set; } = "نقدي";
        public string CashierName { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class PurchaseInvoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "نقدي";
        public string Notes { get; set; }
        public List<PurchaseInvoiceDetail> Details { get; set; } = new List<PurchaseInvoiceDetail>();
    }

    public class PurchaseInvoiceDetail
    {
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
    }

    public class Technician
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Specialization { get; set; }
        public decimal CommissionRate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AuditLogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Username { get; set; }
        public string Action { get; set; }
        public string TargetTable { get; set; }
        public string TargetId { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
    }
}
