using Reports.Services.Drivers;

namespace Reports.Services.Export;

public sealed class DriversExportServiceAdapter : IDriversExportService
{
    public void ClearRows(string excelPath, int lastCol = 12)
    {
        DriversExportService.ClearRows(excelPath, lastCol);
    }

    public void AppendRow(string excelPath, Dictionary<string, DriverRowValue> row, int sheetIndex = 1)
    {
        DriversExportService.AppendRow(excelPath, row, sheetIndex);
    }
}