using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Application.Common;
using AutoCare.Application.DTOs;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using AutoCare.Domain.Enums;

namespace AutoCare.Application.UseCases.Invoices
{
    public class CalculateInvoiceTotalsUseCase
    {
        public InvoiceCalculationDto Execute(decimal servicesTotal, decimal sparePartsTotal, decimal discountAmount, decimal taxRatePercent = 0m, decimal paidAmount = 0m)
        {
            decimal safeServices = Math.Max(0m, servicesTotal);
            decimal safeParts = Math.Max(0m, sparePartsTotal);
            decimal subTotal = safeServices + safeParts;

            decimal safeDiscount = Math.Max(0m, discountAmount);
            if (safeDiscount > subTotal)
                safeDiscount = subTotal; // Invariant: discount cannot exceed subtotal

            decimal netBeforeTax = subTotal - safeDiscount;

            decimal safeTaxRate = Math.Max(0m, taxRatePercent);
            decimal taxAmount = decimal.Round(netBeforeTax * (safeTaxRate / 100m), 2, MidpointRounding.AwayFromZero);

            decimal totalAmount = netBeforeTax + taxAmount;

            decimal safePaid = Math.Max(0m, paidAmount);
            decimal remaining = Math.Max(0m, totalAmount - safePaid);

            return new InvoiceCalculationDto
            {
                ServicesTotal = safeServices,
                SparePartsTotal = safeParts,
                SubTotal = subTotal,
                DiscountAmount = safeDiscount,
                TaxRate = safeTaxRate,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                PaidAmount = safePaid,
                RemainingAmount = remaining
            };
        }
    }

    public class CreateFinalInvoiceUseCase : IUseCase<CreateFinalInvoiceRequest, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CalculateInvoiceTotalsUseCase _calculator;

        public CreateFinalInvoiceUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _calculator = new CalculateInvoiceTotalsUseCase();
        }

        public async Task<Result<string>> ExecuteAsync(CreateFinalInvoiceRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                return Result.Failure<string>("طلب إنشاء الفاتورة غير صالح.");

            if (string.IsNullOrWhiteSpace(request.RepairOrderNumber))
                return Result.Failure<string>("رقم كرت الصيانة مطلوب.");

            var order = await _unitOfWork.RepairOrders.GetByNumberAsync(request.RepairOrderNumber, cancellationToken);
            if (order == null)
                return Result.Failure<string>($"كرت الصيانة برقم '{request.RepairOrderNumber}' غير موجود.");

            var existingInvoice = await _unitOfWork.Invoices.GetByOrderNumberAsync(request.RepairOrderNumber, cancellationToken);
            if (existingInvoice != null)
                return Result.Failure<string>($"تمت فوترة كرت الصيانة '{request.RepairOrderNumber}' مسبقاً برقم فاتورة '{existingInvoice.InvoiceNumber}'.");

            var totals = _calculator.Execute(request.ServicesTotal, request.SparePartsTotal, request.DiscountAmount, request.TaxRate, request.PaidAmount);

            string invoiceNumber = await _unitOfWork.Invoices.GenerateNewInvoiceNumberAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                invoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                RepairOrderNumber = request.RepairOrderNumber,
                InvoiceDate = DateTime.Now,
                ServicesTotal = totals.ServicesTotal,
                SparePartsTotal = totals.SparePartsTotal,
                DiscountAmount = totals.DiscountAmount,
                TaxRate = totals.TaxRate,
                TaxAmount = totals.TaxAmount,
                TotalAmount = totals.TotalAmount,
                PaidAmount = totals.PaidAmount,
                RemainingAmount = totals.RemainingAmount,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? PaymentMethod.Cash.ToDbString() : request.PaymentMethod,
                CashierName = request.CashierName
            };

            await _unitOfWork.Invoices.AddAsync(invoice, cancellationToken);
            await _unitOfWork.RepairOrders.UpdateStatusAsync(request.RepairOrderNumber, RepairOrderStatus.Invoiced.ToDisplayArabic(), cancellationToken);

            return Result.Success(invoiceNumber);
        }
    }
}
