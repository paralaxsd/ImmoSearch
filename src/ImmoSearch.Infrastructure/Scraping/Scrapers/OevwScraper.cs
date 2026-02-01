using HtmlAgilityPack;
using ImmoSearch.Domain.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ImmoSearch.Domain.Extensions;

namespace ImmoSearch.Infrastructure.Scraping.Scrapers;

sealed class OevwScraper(HttpClient http) : IScraper
{
    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public string Source => "oevw";

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    public async Task<IReadOnlyList<Listing>> ScrapeAsync(CancellationToken token)
    {
        var (csrf, cookies) = await GetCsrfAndCookiesAsync(token);
        if (string.IsNullOrWhiteSpace(csrf)) return [];
        var (firstHtml, maxPage) = await PostAndGetMaxPageAsync(csrf, cookies, token);
        var all = new List<Listing>(ParseListings(firstHtml));
        for (var page = 2; page <= maxPage; page++)
        {
            if (!Debugger.IsAttached) await Task.Delay(1000, token);
            var html = await GetPageHtmlAsync(page, cookies, token);
            all.AddRange(ParseListings(html));
        }
        return all;
    }

    static async Task<(string csrf, string cookies)> GetCsrfAndCookiesAsync(CancellationToken token)
    {
        var getReq = new HttpRequestMessage(HttpMethod.Get, "https://www.oevw.at/suche");
        getReq.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; ImmoSearchBot/1.0)");
        getReq.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml");

        using var getResp = await new HttpClient().SendAsync(getReq, token);
        var html = await getResp.Content.ReadAsStringAsync(token);
        
        var csrf = Regex.Match(html, "var csrfToken = \"([^\"]+)\"").Groups[1].Value;
        var cookies = getResp.Headers.TryGetValues("Set-Cookie", out var setCookies)
            ? setCookies.Select(x => x.Split(';')[0]).JoinedBy("; ")
            : string.Empty;

        return (csrf, cookies);
    }

    async Task<(string html, int maxPage)> PostAndGetMaxPageAsync
        (string csrf, string cookies, CancellationToken token)
    {
        var postReq = CreatePostRequestFrom(csrf, cookies);
        using var postResp = await http.SendAsync(postReq, token);

        var html = await postResp.Content.ReadAsStringAsync(token);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var maxPage = GetMaxPage(doc);

        return (html, maxPage);
    }

    static HttpRequestMessage CreatePostRequestFrom(string csrf, string cookies)
    {
        var payload = new Dictionary<string, object?>
        {
            ["unittypes"] = "apartment",
            ["legalform"] = "buy",
            ["zips"] = "1220",
            ["rooms"] = new List<object>(),
            ["area_from"] = "",
            ["area_to"] = "",
            ["price_to"] = "",
            ["available_immediately"] = "",
            ["only_new"] = ""
        };
        var postReq = new HttpRequestMessage(HttpMethod.Post, "https://www.oevw.at/suche/filter")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        postReq.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; ImmoSearchBot/1.0)");
        postReq.Headers.Add("Accept", "application/json, text/plain, */*");
        postReq.Headers.Add("Referer", "https://www.oevw.at/suche");
        postReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
        postReq.Headers.Add("Origin", "https://www.oevw.at");
        postReq.Headers.Add("X-CSRF-Token", csrf);

        if (cookies.HasContent)
            postReq.Headers.Add("Cookie", cookies);
        return postReq;
    }

    async Task<string> GetPageHtmlAsync(int page, string cookies, CancellationToken token)
    {
        var url = $"https://www.oevw.at/suche?page={page}";
        var getReq = new HttpRequestMessage(HttpMethod.Get, url);
        getReq.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; ImmoSearchBot/1.0)");
        getReq.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml");
        if (cookies.HasContent)
            getReq.Headers.Add("Cookie", cookies);
        using var getResp = await http.SendAsync(getReq, token);
        return await getResp.Content.ReadAsStringAsync(token);
    }

    static int GetMaxPage(HtmlDocument doc)
    {
        var last = doc.DocumentNode
            .SelectSingleNode("//ul[contains(@class,'pagination')]//li[contains(@class,'last')]/a")
            ?.GetAttributeValue("href", string.Empty);
        if (last is { Length: >0 } && int.TryParse(last.Split("=").Last(), out var max))
            return max;
        var pages = doc.DocumentNode
            .SelectNodes("//ul[contains(@class,'pagination')]//a[@class='page-link']")
            ?.Select(x => int.TryParse(x.InnerText, out var n) ? n : 1)
            .ToArray();
        return pages?.Max() ?? 1;
    }

    static IReadOnlyList<Listing> ParseListings(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var items = doc.DocumentNode
            .SelectNodes("//li[contains(@class,'thumblist__item')]")
            .OrEmpty();

        return items.Select(CreateListingFrom).ToArray();
    }

    static Listing CreateListingFrom(HtmlNode item)
    {
        var url = item.SelectSingleNode(".//div[contains(@class,'thumb__link')]/a")?.GetAttributeValue("href", string.Empty);
        var project = item.SelectSingleNode(".//div[contains(@class,'thumb__project small')]")?.InnerText.Trim();
        var heading = item.SelectSingleNode(".//div[contains(@class,'thumb__heading big')]")?.InnerText.Trim();
        var title = ((string?[])[project, heading]).ExceptDefault().JoinedBy(" ");
        var img = item.SelectSingleNode(".//img[contains(@class,'thumb__image')]")?.GetAttributeValue("src", string.Empty);
        var subheading = item.SelectSingleNode(".//div[contains(@class,'thumb__subheading')]//ul[contains(@class,'thumb__subheading__list')]");
        var price = subheading?.SelectSingleNode(".//li[contains(text(),'€')]")?.InnerText.Trim().Replace("€", "").Replace(".", "").Replace(",", ".") ?? string.Empty;
        var size = subheading?.SelectSingleNode(".//li[contains(text(),'m²')]")?.InnerText.Trim().Replace(" m²", "").Replace(",", ".") ?? string.Empty;
        var textList = item.SelectSingleNode(".//div[contains(@class,'thumb__text')]//ul[contains(@class,'thumb__text__list')]");
        var rooms = textList?.SelectSingleNode(".//li[contains(text(),'Zimmer')]")?.InnerText.Trim().Replace(" Zimmer", "") ?? string.Empty;

        var listing = new Listing
        {
            Source = "oevw",
            ExternalId = url ?? string.Empty,
            Title = title ?? string.Empty,
            Price = decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
            Size = decimal.TryParse(size, NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : null,
            Rooms = decimal.TryParse(rooms, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : null,
            Url = !string.IsNullOrWhiteSpace(url) ? "https://www.oevw.at" + url : string.Empty,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(img) ? "https://www.oevw.at" + img : null
        };
        return listing;
    }
}
