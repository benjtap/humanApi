using ClosedXML.Excel;
using PaieApi.Models;
using System.IO;

namespace PaieApi.Services
{
    public class ExcelService
    {
        public byte[] GenerateExcel(ExcelReportRequest request)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Report");
                ws.RightToLeft = true; // RTL Support

                // 1. Headers
                var headers = new[] { "תאריך", "משמרת", "כניסה", "יציאה", "הפסקה", "הוצאות", "הוספות", "הערות", "שעות", "שכר" };
                // Note: Image has specific order.
                // Image: Wage(Left), Hours, Comments, Additions, Expenses, Break, Exit, Entry, Shift, Date(Right)
                // In Excel RTL, Col A is Rightmost.
                // So Col A = Date.
                
                int col = 1;
                ws.Cell(1, col++).Value = "תאריך";
                ws.Cell(1, col++).Value = "משמרת";
                ws.Cell(1, col++).Value = "כניסה";
                ws.Cell(1, col++).Value = "יציאה";
                ws.Cell(1, col++).Value = "הפסקה";     
                ws.Cell(1, col++).Value = "הוצאות";
                ws.Cell(1, col++).Value = "הוספות";
                ws.Cell(1, col++).Value = "הערות";
                ws.Cell(1, col++).Value = "שעות";
                ws.Cell(1, col++).Value = "שכר";

                // Style Header
                var headerRange = ws.Range(1, 1, 1, 10);
                headerRange.Style.Fill.BackgroundColor = XLColor.Yellow;
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 2. Data
                int row = 2;
                foreach (var shift in request.Shifts)
                {
                    ws.Cell(row, 1).Value = shift.Date.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = shift.ShiftType;
                    ws.Cell(row, 3).Value = shift.EntryTime;
                    ws.Cell(row, 4).Value = shift.ExitTime;
                    ws.Cell(row, 5).Value = shift.BreakDuration; // Format 0 or 0:00?
                    ws.Cell(row, 6).Value = 0; // Expenses? Map Deductions here? No, breakdown calculates deductions.
                    ws.Cell(row, 7).Value = shift.Additions;
                    ws.Cell(row, 8).Value = shift.Comments;
                    
                    // Hours Format (Decimal to HH:mm)
                    TimeSpan ts = TimeSpan.FromHours(shift.Hours);
                    ws.Cell(row, 9).Value = $"{Math.Floor(ts.TotalHours):00}:{ts.Minutes:00}"; 
                    
                    ws.Cell(row, 10).Value = shift.Wage;
                    
                    // Style
                    ws.Range(row, 1, row, 10).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                     ws.Range(row, 1, row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    row++;
                }

                // 3. Totals Row
                ws.Cell(row, 7).Value = request.Totals.TotalAdditions;
                ws.Cell(row, 9).Value = FormatHours(request.Totals.TotalHours);
                ws.Cell(row, 10).Value = request.Totals.TotalAdditions + request.Totals.GrossWage; // Check logic

                var totalRange = ws.Range(row, 1, row, 10);
                totalRange.Style.Fill.BackgroundColor = XLColor.Yellow;
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Border.TopBorder = XLBorderStyleValues.Double;

                row += 2;

                // 4. Summary Tables (Left side usually in LTR, Right in RTL? Image shows totals on Left)
                // In RTL, Col A is Right. Image shows Summary table aligned to Left of page?
                // Let's put summary tables in Columns 1-3 (Right side) or match image?
                // Image 1 shows Summary Table on the Left (columns Wage/Hours/additions).
                // Wait, Image 1 is LTR? No, Hebrew is RTL.
                // Col A is usually Date (Right) in Hebrew Excel.
                // In Image 1, "Date" is on the Right. "Wage" is on the Left.
                // The Summary Tables are below the "Wage" column (Left).
                // So in code, this is Column 9, 10, etc?
                // Let's align Summary to Columns 8-10 (Left side).

                int summaryStartCol = 9; // Wage column area
                int summaryRow = row;

                // Gross Wage
                ws.Cell(summaryRow, summaryStartCol).Value = "שכר ברוטו";
                ws.Cell(summaryRow, summaryStartCol + 1).Value = request.Totals.GrossWage;
                summaryRow++;

                // Additions
                 ws.Cell(summaryRow, summaryStartCol).Value = "תוספות";
                 ws.Cell(summaryRow, summaryStartCol + 1).Value = request.Totals.TotalAdditions;
                 summaryRow++;
                
                // Deductions
                 ws.Cell(summaryRow, summaryStartCol).Value = "הורדות";
                 ws.Cell(summaryRow, summaryStartCol + 1).Value = request.Totals.TotalDeductions;
                 summaryRow += 2;

                 // Tax Breakdown
                 AddSummaryRow(ws, ref summaryRow, summaryStartCol, "מס בריאות", request.Totals.HealthTax);
                 AddSummaryRow(ws, ref summaryRow, summaryStartCol, "ביטוח לאומי", request.Totals.NationalInsurance);
                 AddSummaryRow(ws, ref summaryRow, summaryStartCol, "מס הכנסה", request.Totals.IncomeTax);
                 AddSummaryRow(ws, ref summaryRow, summaryStartCol, "פנסיה", request.Totals.Pension);
                 AddSummaryRow(ws, ref summaryRow, summaryStartCol, "קרן השתלמות", request.Totals.StudyFund);
                 
                 summaryRow++;
                 // Net Wage
                 ws.Cell(summaryRow, summaryStartCol).Value = "שכר נטו";
                 ws.Cell(summaryRow, summaryStartCol + 1).Value = request.Totals.NetWage;
                 ws.Cell(summaryRow, summaryStartCol).Style.Fill.BackgroundColor = XLColor.Red;
                 ws.Cell(summaryRow, summaryStartCol).Style.Font.FontColor = XLColor.White;
                 
                 // Auto fit
                 ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private void AddSummaryRow(IXLWorksheet ws, ref int row, int col, string label, double value)
        {
            ws.Cell(row, col).Value = label;
            ws.Cell(row, col + 1).Value = value;
            ws.Range(row, col, row, col+1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }
        
        private string FormatHours(double totalHours)
        {
             TimeSpan ts = TimeSpan.FromHours(totalHours);
             return $"{Math.Floor(ts.TotalHours):00}:{ts.Minutes:00}";
        }
    }
}
