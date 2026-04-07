using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Reports.Services.Crm;

public static class CrmClientFactory
{
    public static HttpClient Create(Uri baseUri, IReadOnlyDictionary<string, string> cookies)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        foreach (var (name, value) in cookies)
        {
            handler.CookieContainer.Add(baseUri, new Cookie(name, value) { Path = "/", Secure = true });
        }

        var client = new HttpClient(handler) { BaseAddress = baseUri };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("OData-Version", "4.0");
        client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
        client.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"");
        return client;
    }
}