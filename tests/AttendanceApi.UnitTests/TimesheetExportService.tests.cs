using System.Text;
using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.DTOs.MonthlyTimesheet
{
    public class TimesheetExportFilterDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public long? DepartmentId { get; set; }
    }
}

namespace AttendanceApi.Services
{
    public class TimesheetExportService
    {
        private readonly AttendanceDbContext _context;

        public TimesheetExportService(AttendanceDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportToExcelAsync(TimesheetExportFilterDto filter)
        {
            var summaries = await _context.MonthlyTimesheetSummaries
                .Include(m => m.Employee)
                .AsNoTracking()
                .Where(m => m.Year == filter.Year && m.Month == filter.Month)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bảng Công Tháng");
            worksheet.Cell(1, 1).Value = $"BẢNG TỔNG HỢP CHẤM CÔNG THÁNG {filter.Month:D2}/{filter.Year}";

            int row = 4;
            foreach (var item in summaries)
            {
                worksheet.Cell(row, 1).Value = item.Employee?.EmployeeCode ?? string.Empty;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportToCsvAsync(TimesheetExportFilterDto filter)
        {
            var summaries = await _context.MonthlyTimesheetSummaries
                .Include(m => m.Employee)
                .AsNoTracking()
                .Where(m => m.Year == filter.Year && m.Month == filter.Month)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("\"Mã NV\",\"Họ và Tên\"");

            foreach (var item in summaries)
            {
                sb.AppendLine($"\"{item.Employee?.EmployeeCode}\",\"{item.Employee?.FullName}\"");
            }

            var bom = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[bom.Length + contentBytes.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);
            return result;
        }
    }
}

namespace AttendanceApi.UnitTests.Services
{
    public class TimesheetExportServiceTests
    {
        private AttendanceDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AttendanceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AttendanceDbContext(options);
        }

        [Fact]
        public async Task ExportToExcelAsync_ValidData_ShouldReturnNonEmptyExcelBytes()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var dept = new Department { Code = "IT", Name = "Công Nghệ Thông Tin" };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();

            var emp = new Employee { EmployeeCode = "EMP01", FullName = "Nguyen Van A", DepartmentId = dept.Id };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            context.MonthlyTimesheetSummaries.Add(new MonthlyTimesheetSummary
            {
                EmployeeId = emp.Id,
                Year = 2026,
                Month = 8,
                StandardWorkingDays = 22.0m,
                ActualWorkingDays = 21.0m,
                TotalPayableDays = 21.0m,
                Status = TimesheetStatus.Finalized
            });
            await context.SaveChangesAsync();

            var service = new TimesheetExportService(context);
            var filter = new TimesheetExportFilterDto { Year = 2026, Month = 8 };

            // Act
            var bytes = await service.ExportToExcelAsync(filter);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Đọc lại kiểm tra cấu trúc file Excel
            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            Assert.NotNull(worksheet);
            Assert.Equal("BẢNG TỔNG HỢP CHẤM CÔNG THÁNG 08/2026", worksheet.Cell(1, 1).GetString());
            Assert.Equal("EMP01", worksheet.Cell(4, 1).GetString());
        }

        [Fact]
        public async Task ExportToCsvAsync_ValidData_ShouldIncludeUtf8BomAndHeaderRow()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var emp = new Employee { EmployeeCode = "EMP01", FullName = "Nguyen Van A" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            context.MonthlyTimesheetSummaries.Add(new MonthlyTimesheetSummary
            {
                EmployeeId = emp.Id,
                Year = 2026,
                Month = 8,
                ActualWorkingDays = 22.0m,
                TotalPayableDays = 22.0m,
                Status = TimesheetStatus.Draft
            });
            await context.SaveChangesAsync();

            var service = new TimesheetExportService(context);
            var filter = new TimesheetExportFilterDto { Year = 2026, Month = 8 };

            // Act
            var bytes = await service.ExportToCsvAsync(filter);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length >= 3);

            // Kiểm tra 3 byte đầu tiên là UTF-8 BOM: 0xEF, 0xBB, 0xBF
            Assert.Equal(0xEF, bytes[0]);
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xBF, bytes[2]);

            var csvString = Encoding.UTF8.GetString(bytes);
            Assert.Contains("\"Mã NV\"", csvString);
            Assert.Contains("\"Họ và Tên\"", csvString);
            Assert.Contains("EMP01", csvString);
        }
    }
}