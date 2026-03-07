using System.Collections.ObjectModel;

namespace Reports.Services;

public sealed class ChromeTabsStore
{
    public ObservableCollection<string> TabUrls { get; } = new();

    public void ReplaceAll(IEnumerable<string> urls)
    {
        var filtered = urls.Where(u => u.Contains("crm")).ToList();

        // remove items that no longer exist
        for (int i = TabUrls.Count - 1; i >= 0; i--)
        {
            if (!filtered.Contains(TabUrls[i]))
                TabUrls.RemoveAt(i);
        }

        // add new items
        foreach (var url in filtered)
        {
            if (!TabUrls.Contains(url))
                TabUrls.Add(url);
        }
    }
}