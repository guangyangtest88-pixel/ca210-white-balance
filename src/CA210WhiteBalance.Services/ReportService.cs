using System;
using System.Collections.Generic;
using System.IO;
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
                _logger.LogInformation("导出调试报告到文本文件: {FilePath}", filePath);

                // 简化版：导出为文本文件而不是Excel
                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("==========================================");
                    writer.WriteLine("     CA210白平衡调试报告");
                    writer.WriteLine("==========================================");
                    writer.WriteLine();
                    writer.WriteLine($"结果: {result?.ToString() ?? "无结果"}");
                    writer.WriteLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }

                _logger.LogInformation("报告导出成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报告导出失败");
                return false;
            }
        }

        public bool GenerateTestReport(string filePath, object result, string notes = null)
        {
            // 使用文本文件生成测试报告
            return ExportDebugResultToExcel(filePath, result);
        }
    }
}
