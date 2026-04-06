using Reports.Services.Drivers;

namespace Reports.Services.Export;

public interface IDriversExportService
{
    void ClearRows(string excelPath, int lastCol = 12);
    void AppendRow(string excelPath, Dictionary<string, DriverRowValue> row, int sheetIndex = 1);
}