using System.Text;
using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class TimesheetExportService : ITimesheetExportService
{
    private readonly AttendanceDbContext _context;

    public TimesheetExportService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportToExcelAsync(TimesheetExportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var summaries = await GetExportDataAsync(filter, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"Bảng công {filter.Month:D2}-{filter.Year}");

        // 1. Tiêu đề báo cáo
        worksheet.Cell(1, 1).Value = $"BẢNG TỔNG HỢP CHẤM CÔNG THÁNG {filter.Month:D2}/{filter.Year}";
        worksheet.Range(1, 1, 1, 18).Merge().Style
            .Font.SetBold(true)
            .Font.SetFontSize(16)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // 2. Danh sách Header
        string[] headers =
        [
            "Mã NV", "Họ và Tên", "Phòng ban", "Năm", "Tháng",
            "Công chuẩn", "Công thực tế", "Giờ làm việc", "Nghỉ phép có lương",
            "Nghỉ không lương", "Vắng mặt", "Phút đi muộn", "Phút về sớm",
            "Lần đi muộn", "Lần về sớm", "Giờ OT", "Công tính lương", "Trạng thái"
        ];

        for (int col = 0; col < headers.Length; col++)
        {
            var cell = worksheet.Cell(3, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.SetBold(true);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1F497D"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        // 3. Đổ dữ liệu
        int row = 4;
        foreach (var item in summaries)
        {
            worksheet.Cell(row, 1).SetValue(item.Employee?.EmployeeCode ?? string.Empty);
            worksheet.Cell(row, 2).SetValue(item.Employee?.FullName ?? string.Empty);
            worksheet.Cell(row, 3).SetValue(item.Employee?.Department?.Name ?? string.Empty);
            worksheet.Cell(row, 4).SetValue(item.Year);
            worksheet.Cell(row, 5).SetValue(item.Month);
            worksheet.Cell(row, 6).SetValue(item.StandardWorkingDays);
            worksheet.Cell(row, 7).SetValue(item.ActualWorkingDays);
            worksheet.Cell(row, 8).SetValue(item.ActualWorkingHours);
            worksheet.Cell(row, 9).SetValue(item.PaidLeaveDays);
            worksheet.Cell(row, 10).SetValue(item.UnpaidLeaveDays);
            worksheet.Cell(row, 11).SetValue(item.AbsentDays);
            worksheet.Cell(row, 12).SetValue(item.LateMinutes);
            worksheet.Cell(row, 13).SetValue(item.EarlyMinutes);
            worksheet.Cell(row, 14).SetValue(item.LateOccurrences);
            worksheet.Cell(row, 15).SetValue(item.EarlyOccurrences);
            worksheet.Cell(row, 16).SetValue(item.OvertimeHours);
            worksheet.Cell(row, 17).SetValue(item.TotalPayableDays);
            worksheet.Cell(row, 18).SetValue(item.Status.ToString().ToUpper());

            // Định dạng số
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.0";
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.0";
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.0";
            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.0";
            worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0.0";
            worksheet.Cell(row, 16).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 17).Style.NumberFormat.Format = "#,##0.0";

            row++;
        }

        // 4. Viền bảng & Auto-fit độ rộng cột
        var dataRange = worksheet.Range(3, 1, Math.Max(row - 1, 3), 18);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportToCsvAsync(TimesheetExportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var summaries = await GetExportDataAsync(filter, cancellationToken);
        var sb = new StringBuilder();

        // Header CSV
        sb.AppendLine("\"Mã NV\",\"Họ và Tên\",\"Phòng ban\",\"Năm\",\"Tháng\",\"Công chuẩn\",\"Công thực tế\",\"Giờ làm việc\",\"Nghỉ phép có lương\",\"Nghỉ không lương\",\"Vắng mặt\",\"Phút đi muộn\",\"Phút về sớm\",\"Lần đi muộn\",\"Lần về sớm\",\"Giờ OT\",\"Công tính lương\",\"Trạng thái\"");

        foreach (var item in summaries)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Employee?.EmployeeCode ?? string.Empty),
                EscapeCsv(item.Employee?.FullName ?? string.Empty),
                EscapeCsv(item.Employee?.Department?.Name ?? string.Empty),
                item.Year,
                item.Month,
                item.StandardWorkingDays.ToString("F1"),
                item.ActualWorkingDays.ToString("F1"),
                item.ActualWorkingHours.ToString("F2"),
                item.PaidLeaveDays.ToString("F1"),
                item.UnpaidLeaveDays.ToString("F1"),
                item.AbsentDays.ToString("F1"),
                item.LateMinutes,
                item.EarlyMinutes,
                item.LateOccurrences,
                item.EarlyOccurrences,
                item.OvertimeHours.ToString("F2"),
                item.TotalPayableDays.ToString("F1"),
                EscapeCsv(item.Status.ToString().ToUpper())
            ));
        }

        // Đính kèm UTF-8 BOM (0xEF, 0xBB, 0xBF) để mở đúng tiếng Việt trên MS Excel
        var bom = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + contentBytes.Length];

        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);

        return result;
    }

    private async Task<List<MonthlyTimesheetSummary>> GetExportDataAsync(TimesheetExportFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.MonthlyTimesheetSummaries
            .Include(m => m.Employee)
                .ThenInclude(e => e!.Department)
            .AsNoTracking()
            .Where(m => m.Year == filter.Year && m.Month == filter.Month);

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(m => m.Employee != null && m.Employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<TimesheetStatus>(filter.Status, true, out var parsedStatus))
        {
            query = query.Where(m => m.Status == parsedStatus);
        }

        return await query
            .OrderBy(m => m.Employee != null ? m.Employee.DepartmentId : 0)
            .ThenBy(m => m.EmployeeId)
            .ToListAsync(cancellationToken);
    }

    private static string EscapeCsv(string field)
    {
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}