using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Reports.Services.Export;

namespace Reports.Services.Drivers;

public sealed class DriverMaintenanceService
{
    private readonly IDriverPaths _driverPaths;
    private readonly IDriversExportService _driversExportService;

    public DriverMaintenanceService(
        IDriverPaths driverPaths,
        IDriversExportService driversExportService)
    {
        _driverPaths = driverPaths;
        _driversExportService = driversExportService;
    }

    public async Task ClearRowsAsync(string serviceType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
            throw new InvalidOperationException("Service type is required.");

        var excelPath = Path.Combine(
            _driverPaths.DriversFolderPath,
            _driverPaths.DriversFile(serviceType));

        await Task.Run(
            () => _driversExportService.ClearRows(excelPath, _driverPaths.DriversLastColToClear),
            ct);
    }

    public string GetDriversFileName(string serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
            throw new InvalidOperationException("Service type is required.");

        return _driverPaths.DriversFile(serviceType);
    }
}