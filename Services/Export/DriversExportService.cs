using System.IO;
using ClosedXML.Excel;
using Reports.Services.Drivers;

namespace Reports.Services.Export;
public interface IDriversExportService
{
    void ClearRows(string excelPath, int lastCol = 12);
    void AppendRow(string excelPath, Dictionary<string, DriverRowValue> row, int sheetIndex = 1);
}
public static class DriversExportService
{
    public static void ClearRows(string excelPath, int lastCol = 12)
    {
        if (!File.Exists(excelPath))
            throw new FileNotFoundException("Excel file not found.", excelPath);

        using var workbook = new XLWorkbook(excelPath);
        var ws = workbook.Worksheet(1);

        var lastRowUsed = ws.LastRowUsed();
        if (lastRowUsed is null)
        {
            workbook.Save();
            return;
        }

        var lastRow = lastRowUsed.RowNumber();
        if (lastRow <= 1)
        {
            workbook.Save();
            return;
        }

        ws.Range(2, 1, lastRow, lastCol).Clear(XLClearOptions.Contents);
        workbook.Save();
    }

    public static void AppendRow(string excelPath, Dictionary<string, DriverRowValue> row, int sheetIndex = 1)
    {
        using var workbook = File.Exists(excelPath) ? new XLWorkbook(excelPath) : new XLWorkbook();
        var ws = workbook.Worksheets.Count >= sheetIndex ? workbook.Worksheet(sheetIndex) : workbook.AddWorksheet("Sheet1");

        var lastRow = ws.LastRowUsed();
        var newRow = lastRow is not null ? lastRow.RowNumber() + 1 : 1;

        foreach (var kvp in row)
            ws.Cell(newRow, Convert.ToInt32(kvp.Value.Col)).Value = kvp.Value.Val;

        workbook.SaveAs(excelPath);
    }
}