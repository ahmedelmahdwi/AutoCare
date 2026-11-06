using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for SettleCommissionsView.
    /// Manages technician commission filtering, date ranges, and batch settlements.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class SettleCommissionsViewModel : INotifyPropertyChanged
    {
        private readonly ITechnicianService _technicianService;

        private int? _selectedTechnicianId = 0;
        private string _selectedStatus = "الكل";
        private DateTime? _startDate;
        private DateTime? _endDate;
        private SettlementRecordDto _selectedRow;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TechnicianSummaryDto> Technicians { get; } =
            new ObservableCollection<TechnicianSummaryDto>();

        public ObservableCollection<string> StatusList { get; } =
            new ObservableCollection<string> { "الكل", "معلق", "مسوى" };

        public ObservableCollection<SettlementRecordDto> Rows { get; } =
            new ObservableCollection<SettlementRecordDto>();

        public bool CanSettleCommissions => !string.Equals(Models.UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
        public System.Windows.Visibility SettlementButtonsVisibility => CanSettleCommissions ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public SettleCommissionsViewModel(ITechnicianService technicianService)
        {
            _technicianService = technicianService ?? throw new ArgumentNullException(nameof(technicianService));

            DateTime now = DateTime.Today;
            _startDate = new DateTime(now.Year, now.Month, 1);
            _endDate = now;
            _selectedStatus = "الكل";
            _selectedTechnicianId = 0;

            FilterCommand = new Commands.RelayCommand(() =>
            {
                LoadSettlements();
            });

            ShowPendingCommand = new Commands.RelayCommand(() =>
            {
                ShowAllPending();
            });

            SettleSelectedCommand = new Commands.RelayCommand(() =>
            {
                if (!CanSettleCommissions)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بتسوية العمولات وصرفها (اطلاع وتدقيق مالي فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                if (SelectedRow == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد سجل عمولة من الجدول لتسويته.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (SelectedRow.Status == "مسوى" || SelectedRow.RawStatus == "Paid")
                {
                    System.Windows.MessageBox.Show("هذه العمولة تم تسويتها وصرفها بالفعل.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show($"هل أنت متأكد من تسوية عمولة الفني '{SelectedRow.TechName}' بقيمة {SelectedRow.CommissionAmount} د.ل؟", "تأكيد التسوية", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;

                var res = SettleSingle(SelectedRow);
                if (res.Success)
                {
                    System.Windows.MessageBox.Show("تمت تسوية العمولة بنجاح.", "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            });

            SettleAllCommand = new Commands.RelayCommand(() =>
            {
                if (!CanSettleCommissions)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بتسوية العمولات وصرفها (اطلاع وتدقيق مالي فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                int pendingCount = 0;
                decimal totalPending = 0;
                foreach (var r in Rows)
                {
                    if (r.Status != "مسوى" && r.RawStatus != "Paid" && !string.IsNullOrWhiteSpace(r.SettlementId))
                    {
                        pendingCount++;
                        if (decimal.TryParse(r.CommissionAmount, out decimal amt)) totalPending += amt;
                    }
                }

                if (pendingCount == 0)
                {
                    System.Windows.MessageBox.Show("لا توجد عمولات معلقة للتسوية في القائمة المعروضة.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show($"هل أنت متأكد من تسوية وصرف جميع العمولات المعلقة المعروضة؟\nالعدد: {pendingCount} عمولة\nالإجمالي: {totalPending:N2} د.ل", "تأكيد تسوية العمولات", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;

                var res = SettleAll();
                if (res.Result.Success)
                {
                    System.Windows.MessageBox.Show($"تمت تسوية {res.SettledIds.Count} عمولة بنجاح.", "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Result.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            });

            LoadTechnicians();
            LoadSettlements();
        }

        public System.Windows.Input.ICommand FilterCommand { get; }
        public System.Windows.Input.ICommand ShowPendingCommand { get; }
        public System.Windows.Input.ICommand SettleSelectedCommand { get; }
        public System.Windows.Input.ICommand SettleAllCommand { get; }

        #region Properties
        public int? SelectedTechnicianId
        {
            get => _selectedTechnicianId;
            set
            {
                if (_selectedTechnicianId != value)
                {
                    _selectedTechnicianId = value;
                    OnPropertyChanged();
                    LoadSettlements();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged();
                    LoadSettlements();
                }
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    LoadSettlements();
                }
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    LoadSettlements();
                }
            }
        }

        public SettlementRecordDto SelectedRow
        {
            get => _selectedRow;
            set
            {
                _selectedRow = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public void LoadTechnicians()
        {
            try
            {
                var techs = _technicianService.GetActiveTechniciansList();
                Technicians.Clear();
                Technicians.Add(new TechnicianSummaryDto { TechnicianId = 0, FullName = "الكل" });

                if (techs != null)
                {
                    foreach (var t in techs)
                    {
                        Technicians.Add(t);
                    }
                }

                _selectedTechnicianId = 0;
                OnPropertyChanged(nameof(SelectedTechnicianId));
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("TechnicianService.LoadTechnicians failed", ex);
            }
        }

        public OperationResult ShowAllPending()
        {
            _startDate = null;
            _endDate = null;
            _selectedStatus = "معلق";
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnPropertyChanged(nameof(SelectedStatus));
            return LoadSettlements();
        }

        public OperationResult LoadSettlements()
        {
            if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
            {
                return OperationResult.Fail("تاريخ البداية يجب أن يكون قبل أو يساوي تاريخ النهاية.");
            }

            IsLoading = true;
            try
            {
                int? techId = (SelectedTechnicianId.HasValue && SelectedTechnicianId.Value > 0)
                    ? SelectedTechnicianId.Value
                    : (int?)null;

                string status = SelectedStatus ?? "";
                if (status == "الكل") status = "";

                DateTime? start = StartDate?.Date;
                DateTime? end = EndDate.HasValue ? EndDate.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null;

                var list = _technicianService.GetSettlementRecords(techId, status, start, end);
                Rows.Clear();
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        Rows.Add(item);
                    }
                }

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("Failed to load settlements", ex);
                return OperationResult.Fail("حدث خطأ أثناء استرجاع التسويات: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public OperationResult SettleSingle(SettlementRecordDto row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.SettlementId))
                return OperationResult.Fail("لم يتم تحديد أي عمولة.");

            if (row.Status == "مسوى" || row.RawStatus == "Paid")
                return OperationResult.Fail("هذه العمولة تم تسويتها بالفعل.");

            var res = _technicianService.SettleCommissions(new[] { row.SettlementId });
            if (res.Success)
            {
                LoadSettlements();
            }
            return res;
        }

        public (OperationResult Result, List<string> SettledIds, int? FirstTechId, List<SettlementRecordDto> SettledRecords) SettleAll()
        {
            var emptyResult = (OperationResult.Fail("لا توجد تسويات لعرضها."), new List<string>(), (int?)null, new List<SettlementRecordDto>());

            if (Rows.Count == 0)
                return emptyResult;

            var idsToSettle = new List<string>();
            int? firstTechId = null;
            var settledRecords = new List<SettlementRecordDto>();

            foreach (var r in Rows)
            {
                if (r.Status == "مسوى" || r.RawStatus == "Paid") continue;
                if (string.IsNullOrWhiteSpace(r.SettlementId)) continue;

                if (int.TryParse(r.TechId, out int tid) && tid > 0)
                {
                    if (firstTechId == null) firstTechId = tid;
                }

                settledRecords.Add(r);
                idsToSettle.Add(r.SettlementId);
            }

            if (idsToSettle.Count == 0)
            {
                return (OperationResult.Fail("جميع العمولات المعروضة مسواة بالفعل."), idsToSettle, firstTechId, settledRecords);
            }

            var opRes = _technicianService.SettleCommissions(idsToSettle);
            if (opRes.Success)
            {
                LoadSettlements();
            }

            return (opRes, idsToSettle, firstTechId, settledRecords);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
