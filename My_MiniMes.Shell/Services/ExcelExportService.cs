using My_MiniMes.Shell.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace My_MiniMes.Shell.Services
{
    /// <summary>
    /// 使用 NPOI 将订单数据导出为 Excel (.xlsx) 的服务。
    /// NPOI 是企业 C# 开发中常用的免费 Excel 导入导出库，支持 xls 和 xlsx。
    /// </summary>
    public static class ExcelExportService
    {
        /// <summary>
        /// 将订单列表导出到指定路径的 xlsx 文件。
        /// </summary>
        /// <param name="orders">订单实体集合。</param>
        /// <param name="deviceNames">设备 ID 到设备名称的映射。</param>
        /// <param name="filePath">导出文件完整路径。</param>
        public static Task ExportOrdersAsync(
            IEnumerable<OrderModel> orders,
            IReadOnlyDictionary<int, string> deviceNames,
            string filePath)
        {
            return Task.Run(() =>
            {
                // .NET Core 下注册代码页编码，确保中文、符号等内容写入 Excel 不乱码
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var orderList = orders.ToList();

                using var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("订单数据");

                var headerStyle = workbook.CreateCellStyle();
                headerStyle.Alignment = HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;
                headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;

                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerFont.FontHeightInPoints = 11;
                headerStyle.SetFont(headerFont);

                var dataStyle = workbook.CreateCellStyle();
                dataStyle.VerticalAlignment = VerticalAlignment.Center;

                var headers = new[] { "订单号", "产品名称", "客户名称", "目标数量", "已生产数量", "完成率", "状态", "调度设备", "交付期限", "创建时间" };
                var headerRow = sheet.CreateRow(0);

                for (var i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                var rowIndex = 1;
                long targetTotal = 0;
                long producedTotal = 0;

                foreach (var order in orderList)
                {
                    deviceNames.TryGetValue(order.AssignedDeviceId ?? 0, out var deviceName);

                    targetTotal += order.TargetQuantity;
                    producedTotal += order.ProducedQuantity;

                    var progress = order.TargetQuantity <= 0
                        ? 0
                        : Math.Round(order.ProducedQuantity * 100.0 / order.TargetQuantity, 1);

                    var row = sheet.CreateRow(rowIndex);
                    row.CreateCell(0).SetCellValue(order.OrderNo);
                    row.CreateCell(1).SetCellValue(order.ProductName);
                    row.CreateCell(2).SetCellValue(order.CustomerName);
                    row.CreateCell(3).SetCellValue(order.TargetQuantity);
                    row.CreateCell(4).SetCellValue(order.ProducedQuantity);
                    row.CreateCell(5).SetCellValue($"{progress.ToString("F1")}%");
                    row.CreateCell(6).SetCellValue(OrderStatusCatalog.GetDisplayName(order.Status));
                    row.CreateCell(7).SetCellValue(string.IsNullOrWhiteSpace(deviceName) ? "尚未调度" : deviceName);
                    row.CreateCell(8).SetCellValue(order.Deadline.ToString("yyyy-MM-dd"));
                    row.CreateCell(9).SetCellValue(order.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));

                    for (var i = 0; i < headers.Length; i++)
                    {
                        row.GetCell(i).CellStyle = dataStyle;
                    }

                    rowIndex++;
                }

                var totalRow = sheet.CreateRow(rowIndex);
                totalRow.CreateCell(0).SetCellValue("合计");
                totalRow.CreateCell(1).SetCellValue(string.Empty);
                totalRow.CreateCell(2).SetCellValue(string.Empty);
                totalRow.CreateCell(3).SetCellValue(targetTotal);
                totalRow.CreateCell(4).SetCellValue(producedTotal);
                totalRow.CreateCell(5).SetCellValue(string.Empty);
                totalRow.CreateCell(6).SetCellValue(string.Empty);
                totalRow.CreateCell(7).SetCellValue(string.Empty);
                totalRow.CreateCell(8).SetCellValue(string.Empty);
                totalRow.CreateCell(9).SetCellValue(string.Empty);

                for (var i = 0; i < headers.Length; i++)
                {
                    totalRow.GetCell(i).CellStyle = headerStyle;
                }

                sheet.CreateFreezePane(0, 1);

                for (var i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                workbook.Write(stream);
            });
        }
    }
}
