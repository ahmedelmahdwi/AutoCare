using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Application.Common;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using AutoCare.Domain.ValueObjects;

namespace AutoCare.Application.UseCases.CustomersVehicles
{
    public class RegisterCustomerAndVehicleRequest
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string PlateNumber { get; set; }
        public string CarName { get; set; }
        public string CarModel { get; set; }
        public string Vin { get; set; }
    }

    public class RegisterCustomerAndVehicleUseCase : IUseCase<RegisterCustomerAndVehicleRequest, Result<Tuple<int, int>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCustomerAndVehicleUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<Tuple<int, int>>> ExecuteAsync(RegisterCustomerAndVehicleRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                return Result.Failure<Tuple<int, int>>("بيانات التسجيل مطلوبة.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure<Tuple<int, int>>("اسم العميل مطلوب.");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || request.PhoneNumber.Trim().Length < 10)
                return Result.Failure<Tuple<int, int>>("رقم هاتف العميل غير صالح (يجب ألا يقل عن 10 أرقام).");

            if (!PlateNumber.TryCreate(request.PlateNumber, out var plate, out var plateError))
                return Result.Failure<Tuple<int, int>>(plateError);

            if (!string.IsNullOrWhiteSpace(request.Vin) && !VinNumber.TryCreate(request.Vin, out _, out var vinError))
                return Result.Failure<Tuple<int, int>>(vinError);

            // Register or fetch customer
            var existingCustomer = await _unitOfWork.Customers.GetByPhoneAsync(request.PhoneNumber.Trim(), cancellationToken);
            int customerId;
            if (existingCustomer != null)
            {
                customerId = existingCustomer.Id;
            }
            else
            {
                var newCustomer = new Customer
                {
                    FullName = request.FullName.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Email = request.Email,
                    Address = request.Address,
                    CreatedAt = DateTime.Now
                };
                customerId = await _unitOfWork.Customers.AddAsync(newCustomer, cancellationToken);
            }

            // Register or update vehicle
            var existingVehicle = await _unitOfWork.Vehicles.GetByPlateAsync(plate.Value, cancellationToken);
            int vehicleId;
            if (existingVehicle != null)
            {
                vehicleId = existingVehicle.Id;
            }
            else
            {
                var newVehicle = new Vehicle
                {
                    PlateNumber = plate.Value,
                    CarName = request.CarName,
                    CarModel = request.CarModel,
                    Vin = request.Vin,
                    CustomerPhone = request.PhoneNumber.Trim(),
                    CreatedAt = DateTime.Now
                };
                vehicleId = await _unitOfWork.Vehicles.AddAsync(newVehicle, cancellationToken);
            }

            return Result.Success(Tuple.Create(customerId, vehicleId));
        }
    }
}
