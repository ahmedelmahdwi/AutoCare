using System;

namespace AutoCare.Application.DTOs
{
    /// <summary>
    /// DTO for monthly revenue bar chart data.
    /// Pure Application layer entity without UI dependencies.
    /// </summary>
    public class MonthlyRevenueDto
    {
        public string MonthPeriod { get; set; } = "";
        public string MonthNameArabic { get; set; } = "";
        public decimal TotalRevenue { get; set; }
        public string TotalRevenueFormatted => $"{TotalRevenue:N2} د.ل";
        public string ShortRevenueDisplay => TotalRevenue > 0 ? $"{TotalRevenue:N0}" : "0";
        public double BarHeight { get; set; }
    }

    /// <summary>
    /// DTO for repair order status distribution.
    /// </summary>
    public class OrderStatusDistributionDto
    {
        public string Status { get; set; } = "";
        public string StatusArabic { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string PercentageFormatted => $"{Percentage:0.#}%";
        public string ColorHex { get; set; } = "#3B82F6";
    }

    /// <summary>
    /// DTO for top demanded workshop services.
    /// </summary>
    public class TopServiceDto
    {
        public int Rank { get; set; }
        public string ServiceName { get; set; } = "";
        public int UsageCount { get; set; }
        public decimal TotalIncome { get; set; }
        public string TotalIncomeFormatted => $"{TotalIncome:N2} د.ل";
    }
}
