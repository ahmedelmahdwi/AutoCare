using System;
using System.Collections.Generic;
using C__project_Auto_Care.Core.Entities;

namespace C__project_Auto_Care.Core.DTOs
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static OperationResult Ok(string message = "تمت العملية بنجاح.") =>
            new OperationResult { Success = true, Message = message };

        public static OperationResult Fail(string message) =>
            new OperationResult { Success = false, Message = message };
    }

    public class OperationResult<T> : OperationResult
    {
        public T Data { get; set; }

        public static OperationResult<T> Ok(T data, string message = "تمت العملية بنجاح.") =>
            new OperationResult<T> { Success = true, Message = message, Data = data };

        public new static OperationResult<T> Fail(string message) =>
            new OperationResult<T> { Success = false, Message = message, Data = default(T) };
    }

    public class InvoiceCalculationResult
    {
        public decimal ServicesTotal { get; set; }
        public decimal SparePartsTotal { get; set; }
        public decimal SubTotal => ServicesTotal + SparePartsTotal;
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalDueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string TafqeetArabicWords { get; set; }
        public string ZatcaQrCodeBase64 { get; set; }
    }

    public class RepairOrderDetailsDto
    {
        public RepairOrder Order { get; set; }
        public Customer Customer { get; set; }
        public Vehicle Vehicle { get; set; }
        public List<RepairOrderServiceItem> Services { get; set; } = new List<RepairOrderServiceItem>();
        public List<RepairOrderSparePartItem> SpareParts { get; set; } = new List<RepairOrderSparePartItem>();

        public decimal ServicesTotal
        {
            get
            {
                decimal sum = 0;
                foreach (var s in Services) sum += s.Price;
                return sum;
            }
        }

        public decimal SparePartsTotal
        {
            get
            {
                decimal sum = 0;
                foreach (var p in SpareParts) sum += p.TotalPrice;
                return sum;
            }
        }

        public decimal GrandTotal => ServicesTotal + SparePartsTotal;
    }

    public class VehicleVisitHistoryDto
    {
        public string RepairOrderNumber { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string Status { get; set; }
        public int? Mileage { get; set; }
        public string Complaint { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> ServicesPerformed { get; set; } = new List<string>();
        public List<string> PartsReplaced { get; set; } = new List<string>();
    }

    public class VehicleWithCustomerDto
    {
        public string PlateNumber { get; set; }
        public string Vin { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerName { get; set; }
        public string Notes { get; set; }
    }

    public class CompletedOrderWithoutInvoiceDto
    {
        public string PhoneNumber { get; set; }
        public string CustomerName { get; set; }
        public string VIN { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string CompletionDate { get; set; }
        public string RoNumber { get; set; }
    }

    public class IssuedInvoiceSummaryDto
    {
        public string InvoiceNumber { get; set; }
        public string RepairOrderNumber { get; set; }
        public string InvoiceDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string PlateNumber { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaidAmountDisplay => PaidAmount.ToString("N2") + " د.ل";
        public decimal DiscountAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string CreatedByUsername { get; set; }
    }

    public class SettlementRecordDto
    {
        public string SettlementId { get; set; }
        public string TechId { get; set; }
        public string TechName { get; set; }
        public string RoNumber { get; set; }
        public string CompletionDate { get; set; }
        public string SettledDate => CompletionDate;
        public string ServiceTotal { get; set; }
        public string CommissionRate { get; set; }
        public string CommissionAmount { get; set; }
        public string Status { get; set; }
        public string RawStatus { get; set; }
    }

    public class TechnicianSummaryDto
    {
        public int TechnicianId { get; set; }
        public string FullName { get; set; }
    }

    public class SupplierSummaryDto
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Balance { get; set; }
        public string BalanceDisplay => (decimal.TryParse(Balance, out var b) ? b.ToString("N2") : Balance) + " د.ل";
        public string CreatedAt { get; set; }
    }

    public class PurchaseInvoiceSummaryDto
    {
        public string PurchaseInvoiceNumber { get; set; }
        public string InvoiceDate { get; set; }
        public string SupplierName { get; set; }
        public string TotalAmount { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
    }

    public class PurchaseInvoiceDetailDto
    {
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string UnitCostDisplay => UnitCost.ToString("N2") + " د.ل";
        public string TotalCostDisplay => TotalCost.ToString("N2") + " د.ل";
    }

    public class PurchaseItemRowDto : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private string _partCode;
        private string _partName;
        private int _quantity = 1;
        private decimal _unitCost;

        public string PartCode { get => _partCode; set { _partCode = value; OnPropertyChanged(); } }
        public string PartName { get => _partName; set { _partName = value; OnPropertyChanged(); } }
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCost));
                OnPropertyChanged(nameof(TotalCostDisplay));
            }
        }
        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                _unitCost = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCost));
                OnPropertyChanged(nameof(TotalCostDisplay));
                OnPropertyChanged(nameof(UnitCostDisplay));
            }
        }
        public decimal TotalCost => Quantity * UnitCost;
        public string UnitCostDisplay => UnitCost.ToString("N2") + " د.ل";
        public string TotalCostDisplay => TotalCost.ToString("N2") + " د.ل";

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propName));
        }
    }

    public class SparePartRowDto
    {
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public string Qty { get; set; }
        public string BuyingPrice { get; set; }
        public string SellingPrice { get; set; }
        public string MinStock { get; set; }
        public string Category { get; set; }
    }

    public class ServiceRowDto
    {
        public string ServiceCode { get; set; }
        public string ServiceName { get; set; }
        public string StandardPrice { get; set; }
        public string Category { get; set; }
    }

    public class CustomerVehicleRowDto
    {
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string PlateNumber { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string EntryDate { get; set; }
        public string Status { get; set; }
        public string Complaint { get; set; }
        public string RoNumber { get; set; }
    }

    public class UserListItemDto
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string RoleDisplay { get; set; }
        public bool IsActive { get; set; }
        public string StatusDisplay => IsActive ? "مفعل" : "معطل";
        public int BranchId { get; set; } = 1;
        public string BranchName { get; set; } = "الفرع الرئيسي";
    }

    public class CarStatusRowDto
    {
        public string PlateNumber { get; set; }
        public string VIN { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string EntryDate { get; set; }
        public string Status { get; set; }
        public string RawStatus { get; set; }
        public string RoNumber { get; set; }
        public string RepairOrderNumber => RoNumber;
    }

    public class TechnicianRowDto
    {
        public int TechId { get; set; }
        public string TechIdStr => TechId.ToString();
        public string TechName { get; set; }
        public string Specialization { get; set; }
        public decimal CommissionPercent { get; set; }
        public string Commission => CommissionPercent.ToString("F2") + " %";
        public bool IsActive { get; set; }
        public string StatusDisplay => IsActive ? "نشط" : "معطل";
        public int BranchId { get; set; } = 1;
        public string BranchName { get; set; } = "الفرع الرئيسي";
    }

    public class AuditLogRowDto
    {
        public string Timestamp { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string TargetTable { get; set; }
        public string RecordKey { get; set; }
        public string Details { get; set; }
    }

    public class AuditQueryResultDto
    {
        public List<AuditLogRowDto> Logs { get; set; } = new List<AuditLogRowDto>();
        public int TotalCount { get; set; }
        public int TodayCount { get; set; }
        public string LastLogTime { get; set; } = "-";
    }

    public class VehicleServiceHistoryRowDto
    {
        public string RoNumber { get; set; }
        public string EntryDate { get; set; }
        public string Mileage { get; set; }
        public string Status { get; set; }
        public string Complaint { get; set; }
        public string InvoiceNumber { get; set; }
        public string PaymentMethod { get; set; }
        public string GrandTotal { get; set; }
    }

    public class ServiceCatalogItemDto
    {
        public string ServiceCode { get; set; }
        public string ServiceName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
    }

    public class SparePartCatalogItemDto
    {
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
    }

    public class OrderServiceItemDto
    {
        public string ServiceCode { get; set; }
        public string ServiceName { get; set; }
        public decimal UnitPrice { get; set; }
        public string TechnicianName { get; set; }
        public int TechnicianId { get; set; }
        public decimal CommissionPercent { get; set; }
    }

    public class OrderSparePartItemDto
    {
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class InventoryReportItemDto
    {
        public string PartCode { get; set; }
        public string PartNumber => PartCode;
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public int QuantityInStock => Quantity;
        public int MinimumStock { get; set; }
        public int MinStockLevel => MinimumStock;
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalValue => Quantity * BuyingPrice;
        public string TotalValueDisplay => TotalValue.ToString("N2");
        public string BuyingPriceDisplay => BuyingPrice.ToString("N2");
        public string SellingPriceDisplay => SellingPrice.ToString("N2");
        public string Category { get; set; }
        public string Location { get; set; }
        public bool IsLowStock => Quantity <= MinimumStock;
        public string Status => Quantity == 0 ? "نفذ المخزون" : (IsLowStock ? "منخفض" : "متوفر");
    }

    public class InventoryReportSummaryDto
    {
        public List<InventoryReportItemDto> Items { get; set; } = new List<InventoryReportItemDto>();
        public int TotalTypes => Items.Count;
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }

    public class FinancialReportItemDto
    {
        public string InvoiceNumber { get; set; }
        public string RepairOrderNumber { get; set; }
        public string InvoiceDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CreatedBy { get; set; }
        public decimal ServicesTotal { get; set; }
        public decimal PartsTotal { get; set; }
        public decimal SparePartsTotal => PartsTotal;
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAmount => GrandTotal;
        public string PaymentMethod { get; set; }

        public string ServicesTotalDisplay => ServicesTotal.ToString("N2");
        public string PartsTotalDisplay => PartsTotal.ToString("N2");
        public string SparePartsTotalDisplay => PartsTotal.ToString("N2");
        public string DiscountAmountDisplay => DiscountAmount.ToString("N2");
        public string TaxAmountDisplay => TaxAmount.ToString("N2");
        public string GrandTotalDisplay => GrandTotal.ToString("N2");
        public string TotalAmountDisplay => GrandTotal.ToString("N2");
    }

    public class FinancialReportSummaryDto
    {
        public List<FinancialReportItemDto> Invoices { get; set; } = new List<FinancialReportItemDto>();
        public int InvoiceCount => Invoices.Count;
        public decimal TotalServices { get; set; }
        public decimal TotalParts { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class TopServiceItemDto
    {
        public string ServiceCode { get; set; }
        public string ServiceName { get; set; }
        public int Count { get; set; }
        public string CountDisplay => Count.ToString();
        public decimal TotalRevenue { get; set; }
        public string TotalRevenueDisplay => TotalRevenue.ToString("N2") + " د.ل";
    }

    public class TopPartItemDto
    {
        public string PartCode { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public int QuantitySold
        {
            get => Quantity;
            set => Quantity = value;
        }
        public string QuantitySoldDisplay => Quantity.ToString();
        public decimal TotalRevenue { get; set; }
        public int StockLeft { get; set; }
        public string TotalRevenueDisplay => TotalRevenue.ToString("N2") + " د.ل";
    }

    public class FinalInvoicePrintDto
    {
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PaymentMethod { get; set; }
        public RepairOrderDetailsDto OrderDetails { get; set; }
    }

    public class RecycleBinItemDto
    {
        public string ItemType { get; set; } // "عميل" | "مركبة" | "موعد"
        public string Key { get; set; }      // PhoneNumber, PlateNumber, AppointmentNumber
        public string DisplayName { get; set; }
        public string Details { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string FormattedDate => DeletedAt.HasValue ? DeletedAt.Value.ToString("yyyy-MM-dd HH:mm") : "-";
        public bool CanPurge { get; set; }
        public string StatusReason { get; set; }

        public string TypeBadgeColor
        {
            get
            {
                if (ItemType == "عميل") return "#0284C7";
                if (ItemType == "مركبة") return "#10B981";
                if (ItemType == "موعد") return "#F59E0B";
                return "#64748B";
            }
        }

        public string StatusBadgeColor => CanPurge ? "#10B981" : "#EF4444";
        public string StatusBadgeBackground => CanPurge ? "#ECFDF5" : "#FEF2F2";
        public string StatusBadgeBorder => CanPurge ? "#A7F3D0" : "#FECACA";
    }
}








