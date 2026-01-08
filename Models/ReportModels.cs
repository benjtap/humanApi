using System;
using System.Collections.Generic;

namespace PaieApi.Models
{
    public class ExcelReportRequest
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<ReportShiftItem> Shifts { get; set; }
        public ReportTotals Totals { get; set; }
        public string EmailAddress { get; set; } // For "Send to Email"
    }

    public class ReportShiftItem
    {
        // We receive pre-formatted strings or raw values. 
        // Logic is better handled in C# if raw, but Frontend has formatting logic.
        // Let's take mixed approach: Values for calculation, Strings for display if needed.
        public DateTime Date { get; set; }
        public string ShiftType { get; set; }
        public string EntryTime { get; set; }
        public string ExitTime { get; set; }
        public double BreakDuration { get; set; } // Minutes
        public double Additions { get; set; }
        public double Deductions { get; set; }
        public string Comments { get; set; }
        public double Hours { get; set; } // Decimal hours
        public double Wage { get; set; }
    }

    public class ReportTotals
    {
        public double TotalHours { get; set; }
        public double TotalAdditions { get; set; }
        public double TotalDeductions { get; set; } // General deductions
        public double GrossWage { get; set; }
        
        // Tax Breakdown
        public double HealthTax { get; set; }
        public double NationalInsurance { get; set; }
        public double IncomeTax { get; set; }
        public double Pension { get; set; }
        public double StudyFund { get; set; }
        
        public double NetWage { get; set; }
    }
}
