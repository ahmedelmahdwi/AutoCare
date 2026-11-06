using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Application.Common;
using AutoCare.Application.DTOs;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using AutoCare.Domain.Enums;
using AutoCare.Domain.ValueObjects;

namespace AutoCare.Application.UseCases.RepairOrders
{
    public class CreateRepairOrderUseCase : IUseCase<CreateRepairOrderRequest, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateRepairOrderUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<string>> ExecuteAsync(CreateRepairOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                return Result.Failure<string>("الطلب لا يمكن أن يكون فارغاً.");

            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                return Result.Failure<string>("رقم هاتف العميل مطلوب.");

            if (!PlateNumber.TryCreate(request.PlateNumber, out var plate, out var plateError))
                return Result.Failure<string>(plateError);

            if (!string.IsNullOrWhiteSpace(request.Vin) && !VinNumber.TryCreate(request.Vin, out _, out var vinError))
                return Result.Failure<string>(vinError);

            bool hasActive = await _unitOfWork.RepairOrders.HasActiveOrderForPlateAsync(plate.Value, cancellationToken);
            if (hasActive)
                return Result.Failure<string>($"توجد بالفعل بطاقة صيانة جارية (نشطة) لهذه السيارة برقم اللوحة '{plate.Value}'.");

            string roNumber = await _unitOfWork.RepairOrders.GenerateNewOrderNumberAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(roNumber))
                roNumber = "RO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            var order = new RepairOrder
            {
                RepairOrderNumber = roNumber,
                PlateNumber = plate.Value,
                EntryDate = DateTime.Now,
                Status = RepairOrderStatus.Pending.ToDisplayArabic(),
                Receptionist = request.Receptionist,
                Complaint = request.Complaint,
                Mileage = request.Mileage,
                FuelLevel = request.FuelLevel,
                InspectionNotes = request.InspectionNotes
            };

            await _unitOfWork.RepairOrders.AddAsync(order, cancellationToken);
            return Result.Success(roNumber);
        }
    }

    public class CompleteRepairOrderUseCase : IUseCase<string, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompleteRepairOrderUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result> ExecuteAsync(string roNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roNumber))
                return Result.Failure("رقم كرت الصيانة مطلوب.");

            var order = await _unitOfWork.RepairOrders.GetByNumberAsync(roNumber, cancellationToken);
            if (order == null)
                return Result.Failure($"كرت الصيانة برقم '{roNumber}' غير موجود.");

            if (order.Status == RepairOrderStatus.Completed.ToDisplayArabic() || order.Status == "مكتمل")
                return Result.Failure("كرت الصيانة مكتمل بالفعل.");

            await _unitOfWork.RepairOrders.UpdateStatusAsync(roNumber, RepairOrderStatus.Completed.ToDisplayArabic(), cancellationToken);
            return Result.Success();
        }
    }

    public class AddServiceToOrderUseCase : IUseCase<Tuple<string, int, int>, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddServiceToOrderUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result> ExecuteAsync(Tuple<string, int, int> request, CancellationToken cancellationToken = default)
        {
            string roNumber = request.Item1;
            int serviceId = request.Item2;
            int technicianId = request.Item3;

            if (string.IsNullOrWhiteSpace(roNumber))
                return Result.Failure("رقم كرت الصيانة مطلوب.");

            if (serviceId <= 0)
                return Result.Failure("معرف الخدمة غير صالح.");

            if (technicianId <= 0)
                return Result.Failure("يجب اختيار فني مسؤول عن تنفيذ الخدمة.");

            var order = await _unitOfWork.RepairOrders.GetByNumberAsync(roNumber, cancellationToken);
            if (order == null)
                return Result.Failure($"كرت الصيانة برقم '{roNumber}' غير موجود.");

            return Result.Success();
        }
    }

    public class AddSparePartToOrderUseCase : IUseCase<Tuple<string, string, int>, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddSparePartToOrderUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result> ExecuteAsync(Tuple<string, string, int> request, CancellationToken cancellationToken = default)
        {
            string roNumber = request.Item1;
            string partCode = request.Item2;
            int quantity = request.Item3;

            if (string.IsNullOrWhiteSpace(roNumber))
                return Result.Failure("رقم كرت الصيانة مطلوب.");

            if (string.IsNullOrWhiteSpace(partCode))
                return Result.Failure("رمز أو رقم قطعة الغيار مطلوب.");

            if (quantity <= 0)
                return Result.Failure("الكمية المطلوبة يجب أن تكون أكبر من الصفر.");

            bool deducted = await _unitOfWork.SpareParts.DeductStockAtomicAsync(partCode, quantity, cancellationToken);
            if (!deducted)
                return Result.Failure($"الكمية المتوفرة في المخزون لقطعة الغيار '{partCode}' غير كافية.");

            return Result.Success();
        }
    }
}
