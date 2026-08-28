using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/timesheets/export")]
public class TimesheetExportController : ControllerBase
{
    private readonly ITimesheetExportService _exportService;

    public TimesheetExportController(ITimesheetExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportExcel([FromQuery] TimesheetExportFilterDto filter, CancellationToken cancellationToken)
    {
        var fileBytes = await _exportService.ExportToExcelAsync(filter, cancellationToken);
        var fileName = $"BaoCaoBangCong_{filter.Month:D2}_{filter.Year}.xlsx";
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(fileBytes, contentType, fileName);
    }

    [HttpGet("csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv([FromQuery] TimesheetExportFilterDto filter, CancellationToken cancellationToken)
    {
        var fileBytes = await _exportService.ExportToCsvAsync(filter, cancellationToken);
        var fileName = $"BaoCaoBangCong_{filter.Month:D2}_{filter.Year}.csv";
        const string contentType = "text/csv; charset=utf-8";

        return File(fileBytes, contentType, fileName);
    }
}