using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Application.Common.Helpers
{
    public static class NpoiBulkInsertHelper
    {
        // NPOI is fully open source and suitable for commercial use
        public static List<T> ParseExcelToEntities<T>(Stream excelStream, Func<IDictionary<string, object>, T> mapFunc)
        {
            var entities = new List<T>();
            IWorkbook workbook = new XSSFWorkbook(excelStream);
            var sheet = workbook.GetSheetAt(0);
            var headerRow = sheet.GetRow(0);
            var columnNames = new List<string>();
            for (int col = 0; col < headerRow.LastCellNum; col++)
            {
                columnNames.Add(headerRow.GetCell(col).ToString());
            }
            for (int rowIdx = 1; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                var values = new Dictionary<string, object>();
                for (int col = 0; col < headerRow.LastCellNum; col++)
                {
                    values[columnNames[col]] = row.GetCell(col)?.ToString();
                }
                entities.Add(mapFunc(values));
            }
            return entities;
        }
    }
}
