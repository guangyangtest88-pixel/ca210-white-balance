using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.Services
{
    /// <summary>
    /// 报告服务接口
    /// </summary>
    public interface IReportService
    {
        /// <summary>导出测量数据到CSV（使用object类型以支持Mock数据）</summary>
        bool ExportToCsv(string filePath, List<object> data);

        /// <summary>导出调试结果到Excel（使用object类型以支持Mock数据）</summary>
        bool ExportDebugResultToExcel(string filePath, object result);

        /// <summary>生成测试报告</summary>
        bool GenerateTestReport(string filePath, object result, string notes = null);
    }

    /// <summary>
    /// 报告服务实现（简化版，用于GitHub Actions编译）
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ILogger<ReportService> _logger;

        public ReportService(ILogger<ReportService> logger)
        {
            _logger = logger;
        }

        public bool ExportToCsv(string filePath, List<object> data)
        {
            try
            {
                _logger.LogInformation("导出CSV文件: {FilePath}", filePath);

                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    // CSV表头（通用格式）
                    writer.WriteLine("时间,数据");

                    // 数据行
                    foreach (var item in data)
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{item}");
                    }
                }

                _logger.LogInformation("CSV导出成功，记录数: {Count}", data.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV导出失败");
                return false;
            }
        }

        public bool ExportDebugResultToExcel(string filePath, object result)
        {
            try
            {
                _logger.LogInformation("导出Excel文件: {FilePath}", filePath);

                using (var workbook = new XLWorkbook())
                {
                    // 调试结果表
                    var resultSheet = workbook.Worksheets.Add("调试结果");

                    // 标题
                    resultSheet.Cell("B1").Value = "CA210白平衡调试报告";
                    resultSheet.Cell("B1").Style.Font.Bold = true;
                    resultSheet.Cell("B1").Style.Font.FontSize = 16;

                    // 结果信息
                    int row = 3;
                    resultSheet.Cell(row, 1).Value = "结果:";
                    resultSheet.Cell(row, 2).Value = result?.ToString() ?? "无结果";
                    row++;
                    resultSheet.Cell(row, 1).Value = "导出时间:";
                    resultSheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // 调整列宽
                    resultSheet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }

                _logger.LogInformation("Excel导出成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel导出失败");
                return false;
            }
        }

        public bool GenerateTestReport(string filePath, object result, string notes = null)
        {
            // 使用Excel生成测试报告
            return ExportDebugResultToExcel(filePath, result);
        }
    }
}
