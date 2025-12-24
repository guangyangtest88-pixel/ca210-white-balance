using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CA210WhiteBalance.Core.Models;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.Services
{
    /// <summary>
    /// 报告服务接口
    /// </summary>
    public interface IReportService
    {
        /// <summary>导出测量数据到CSV</summary>
        bool ExportToCsv(string filePath, List<CA210Data> data);

        /// <summary>导出调试结果到Excel</summary>
        bool ExportDebugResultToExcel(string filePath, DebugResult result);

        /// <summary>生成测试报告</summary>
        bool GenerateTestReport(string filePath, DebugResult result, string notes = null);
    }

    /// <summary>
    /// 报告服务实现
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ILogger<ReportService> _logger;

        public ReportService(ILogger<ReportService> logger)
        {
            _logger = logger;
        }

        public bool ExportToCsv(string filePath, List<CA210Data> data)
        {
            try
            {
                _logger.LogInformation("导出CSV文件: {FilePath}", filePath);

                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    // CSV表头
                    writer.WriteLine("时间,Lv,x,y,T,Duv,u',v',X,Y,Z");

                    // 数据行
                    foreach (var item in data)
                    {
                        writer.WriteLine($"{item.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                        $"{item.lv:F2},{item.Sx:F4},{item.Sy:F4}," +
                                        $"{item.T:F0},{item.Duv:F4}," +
                                        $"{item.Ud:F4},{item.Vd:F4}," +
                                        $"{item.X:F2},{item.Y:F2},{item.Z:F2}");
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

        public bool ExportDebugResultToExcel(string filePath, DebugResult result)
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

                    // 基本信息
                    int row = 3;
                    resultSheet.Cell(row, 1).Value = "开始时间:";
                    resultSheet.Cell(row, 2).Value = result.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                    resultSheet.Cell(row, 1).Value = "结束时间:";
                    resultSheet.Cell(row, 2).Value = result.EndTime.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                    resultSheet.Cell(row, 1).Value = "持续时间:";
                    resultSheet.Cell(row, 2).Value = $"{result.Duration.TotalSeconds:F1} 秒";
                    row++;
                    resultSheet.Cell(row, 1).Value = "状态:";
                    resultSheet.Cell(row, 2).Value = result.Success ? "成功" : "失败";
                    resultSheet.Cell(row, 2).Style.Font.Color = result.Success ? XLColor.Green : XLColor.Red;
                    row++;

                    // 目标值
                    row++;
                    resultSheet.Cell(row, 1).Value = "目标值";
                    resultSheet.Cell(row, 1).Style.Font.Bold = true;
                    row++;
                    resultSheet.Cell(row, 1).Value = "目标 x:";
                    resultSheet.Cell(row, 2).Value = result.TargetX.ToString("F4");
                    row++;
                    resultSheet.Cell(row, 1).Value = "目标 y:";
                    resultSheet.Cell(row, 2).Value = result.TargetY.ToString("F4");
                    row++;
                    resultSheet.Cell(row, 1).Value = "容差范围:";
                    resultSheet.Cell(row, 2).Value = $"±{result.Tolerance:F4}";

                    // 初始值
                    row++;
                    resultSheet.Cell(row, 1).Value = "初始测量值";
                    resultSheet.Cell(row, 1).Style.Font.Bold = true;
                    row++;
                    WriteMeasurementData(resultSheet, ref row, result.InitialData);

                    // 最终值
                    row++;
                    resultSheet.Cell(row, 1).Value = "最终测量值";
                    resultSheet.Cell(row, 1).Style.Font.Bold = true;
                    row++;
                    WriteMeasurementData(resultSheet, ref row, result.FinalData);

                    // 偏差
                    row++;
                    resultSheet.Cell(row, 1).Value = "偏差分析";
                    resultSheet.Cell(row, 1).Style.Font.Bold = true;
                    row++;
                    resultSheet.Cell(row, 1).Value = "Δx 偏差:";
                    resultSheet.Cell(row, 2).Value = result.FinalDeltaX.ToString("F4");
                    resultSheet.Cell(row, 3).Value = result.FinalDeltaX <= result.Tolerance ? "合格" : "不合格";
                    resultSheet.Cell(row, 3).Style.Font.Color = result.FinalDeltaX <= result.Tolerance ? XLColor.Green : XLColor.Red;
                    row++;
                    resultSheet.Cell(row, 1).Value = "Δy 偏差:";
                    resultSheet.Cell(row, 2).Value = result.FinalDeltaY.ToString("F4");
                    resultSheet.Cell(row, 3).Value = result.FinalDeltaY <= result.Tolerance ? "合格" : "不合格";
                    resultSheet.Cell(row, 3).Style.Font.Color = result.FinalDeltaY <= result.Tolerance ? XLColor.Green : XLColor.Red;
                    row++;
                    resultSheet.Cell(row, 1).Value = "迭代次数:";
                    resultSheet.Cell(row, 2).Value = result.Iterations;

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

        public bool GenerateTestReport(string filePath, DebugResult result, string notes = null)
        {
            // 使用Excel生成测试报告
            return ExportDebugResultToExcel(filePath, result);
        }

        private void WriteMeasurementData(IXLWorksheet sheet, ref int row, CA210Data data)
        {
            if (data == null) return;

            sheet.Cell(row, 1).Value = "Lv (cd/m²):";
            sheet.Cell(row, 2).Value = data.Lv.ToString("F2");
            row++;
            sheet.Cell(row, 1).Value = "x:";
            sheet.Cell(row, 2).Value = data.Sx.ToString("F4");
            row++;
            sheet.Cell(row, 1).Value = "y:";
            sheet.Cell(row, 2).Value = data.Sy.ToString("F4");
            row++;
            sheet.Cell(row, 1).Value = "色温 T (K):";
            sheet.Cell(row, 2).Value = data.T.ToString("F0");
            row++;
            sheet.Cell(row, 1).Value = "u':";
            sheet.Cell(row, 2).Value = data.Ud.ToString("F4");
            row++;
            sheet.Cell(row, 1).Value = "v':";
            sheet.Cell(row, 2).Value = data.Vd.ToString("F4");
            row++;
            sheet.Cell(row, 1).Value = "X:";
            sheet.Cell(row, 2).Value = data.X.ToString("F2");
            row++;
            sheet.Cell(row, 1).Value = "Y:";
            sheet.Cell(row, 2).Value = data.Y.ToString("F2");
            row++;
            sheet.Cell(row, 1).Value = "Z:";
            sheet.Cell(row, 2).Value = data.Z.ToString("F2");
            row++;
        }
    }
}
