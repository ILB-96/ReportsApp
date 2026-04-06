using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Abstractions.Controls;

namespace Reports.Services.Navigation;

public sealed class DependencyInjectionPageProvider : INavigationViewPageProvider
{
    private readonly IServiceProvider _serviceProvider;

    public DependencyInjectionPageProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? GetPage(Type pageType)
    {
        return _serviceProvider.GetService(pageType);
    }
}