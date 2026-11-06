using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class VehicleHistoryViewModel : ViewModelBase
    {
        private readonly IVehicleHistoryService _historyService;
        private string _plateNumber;
        private string _title;

        public ObservableCollection<VehicleServiceHistoryRowDto> HistoryRecords { get; } =
            new ObservableCollection<VehicleServiceHistoryRowDto>();

        public string PlateNumber
        {
            get => _plateNumber;
            set => SetProperty(ref _plateNumber, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public VehicleHistoryViewModel(string plateNumber, IVehicleHistoryService historyService = null)
        {
            _historyService = historyService ?? AppServiceLocator.VehicleHistoryService;
            PlateNumber = plateNumber ?? "";
            Title = $"سجل الزيارات والصيانات السابقة للمركبة رقم ({PlateNumber})";

            CloseCommand = new RelayCommand(() => CloseAction?.Invoke());
            LoadHistory();
        }

        public void LoadHistory()
        {
            if (string.IsNullOrWhiteSpace(PlateNumber)) return;

            try
            {
                var list = _historyService.GetVehicleServiceHistoryList(PlateNumber);
                HistoryRecords.Clear();
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        HistoryRecords.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("VehicleHistoryViewModel.LoadHistory failed", ex);
            }
        }
    }
}
