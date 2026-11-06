using System;
using System.Collections.ObjectModel;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;

namespace C__project_Auto_Care.ViewModels
{
    public class TopServicesPartsViewModel
    {
        private readonly IRepairOrderService _roService;
        private readonly IInventoryService _invService;

        public ObservableCollection<TopServiceItemDto> TopServices { get; } = new ObservableCollection<TopServiceItemDto>();
        public ObservableCollection<TopPartItemDto> TopSpareParts { get; } = new ObservableCollection<TopPartItemDto>();

        public int TotalServiceCount { get; private set; }
        public decimal TotalServiceRevenue { get; private set; }
        public string MostRequestedServiceName { get; private set; } = "لا يوجد";
        public string MostRequestedPartName { get; private set; } = "لا يوجد";

        public TopServicesPartsViewModel(IRepairOrderService roService = null, IInventoryService invService = null)
        {
            _roService = roService ?? AppServiceLocator.RepairOrderService;
            _invService = invService ?? AppServiceLocator.InventoryService;
        }

        public void LoadReport(DateTime startDate, DateTime endDate, int topN, string category)
        {
            TopServices.Clear();
            TopSpareParts.Clear();

            int totalCount = 0;
            decimal totalRev = 0;
            string topSvcName = "";
            string topPrtName = "";

            if (category == "الكل" || category == "الخدمات")
            {
                var services = _roService.GetTopServices(startDate, endDate, topN);
                if (services != null && services.Count > 0)
                {
                    topSvcName = services[0].ServiceName;
                    foreach (var s in services)
                    {
                        TopServices.Add(s);
                        totalCount += s.Count;
                        totalRev += s.TotalRevenue;
                    }
                }
            }

            if (category == "الكل" || category == "قطع الغيار")
            {
                var parts = _invService.GetTopSpareParts(startDate, endDate, topN);
                if (parts != null && parts.Count > 0)
                {
                    topPrtName = parts[0].PartName;
                    foreach (var p in parts)
                    {
                        TopSpareParts.Add(p);
                    }
                }
            }

            TotalServiceCount = totalCount;
            TotalServiceRevenue = totalRev;
            MostRequestedServiceName = string.IsNullOrEmpty(topSvcName) ? "لا يوجد" : topSvcName;
            MostRequestedPartName = string.IsNullOrEmpty(topPrtName) ? "لا يوجد" : topPrtName;
        }
    }
}
