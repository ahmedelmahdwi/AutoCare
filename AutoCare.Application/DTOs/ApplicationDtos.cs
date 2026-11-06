using System;
using System.Collections.Generic;

namespace AutoCare.Application.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VehicleDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string Vin { get; set; }
        public string CustomerPhone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RepairOrderDto
    {
        public int Id { get; set; }
        public string RepairOrderNumber { get; set; }
        public string PlateNumber { get; set; }
        public DateTime EntryDate { get; set; }
        public string Status { get; set; }
        public string Receptionist { get; set; }
        public string Complaint { get; set; }
        public int? Mileage { get; set; }
        public string FuelLevel { get; set; }
        public decimal TotalServicesAmount { get; set; }
        public decimal TotalSparePartsAmount { get; set; }
        public decimal GrandTotal => TotalServicesAmount + TotalSparePartsAmount;
    }

    public class InvoiceCalculationDto
    {
        public decimal ServicesTotal { get; set; }
        public decimal SparePartsTotal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    public class CreateRepairOrderRequest
    {
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string PlateNumber { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string Vin { get; set; }
        public string Complaint { get; set; }
        public int? Mileage { get; set; }
        public string FuelLevel { get; set; }
        public string InspectionNotes { get; set; }
        public string Receptionist { get; set; }
    }

    public class CreateFinalInvoiceRequest
    {
        public string RepairOrderNumber { get; set; }
        public decimal ServicesTotal { get; set; }
        public decimal SparePartsTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string CashierName { get; set; }
    }
}
