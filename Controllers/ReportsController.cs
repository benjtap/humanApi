using Microsoft.AspNetCore.Mvc;
using PaieApi.Models;
using PaieApi.Services;
using System.IO;

namespace PaieApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ExcelService _excelService;

        public ReportsController(ExcelService excelService)
        {
            _excelService = excelService;
        }

        [HttpPost("excel-view")]
        public IActionResult GenerateExcelForView([FromBody] ExcelReportRequest request)
        {
            if (request == null) return BadRequest("Invalid request");

            try
            {
                var fileContent = _excelService.GenerateExcel(request);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{request.Month}_{request.Year}.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating report: {ex.Message}");
            }
        }

        [HttpPost("excel-email")]
        public IActionResult EmailExcelReport([FromBody] ExcelReportRequest request)
        {
            if (string.IsNullOrEmpty(request.EmailAddress)) return BadRequest("Email required");

            try
            {
                var fileContent = _excelService.GenerateExcel(request);
                
                // TODO: Implement Email Service (SendGrid / SMTP)
                // For now, simply simulating success
                Console.WriteLine($"[Mock Email] Sending Report_{request.Month}_{request.Year}.xlsx to {request.EmailAddress}");
                
                return Ok(new { message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending email: {ex.Message}");
            }
        }
    }
}
