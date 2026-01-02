namespace ImmoSearch.Scraper.Worker.Scraping.Options;

public class ImmobilienScout24Options
{
    public string BaseUrl { get; set; } = "https://www.immobilienscout24.at";
    public string ZipCode { get; set; } = "1080";
    public int PrimaryAreaFrom { get; set; } = 60;
    public int PrimaryAreaTo { get; set; } = 65;
    public int PrimaryPriceFrom { get; set; } = 430000;
    public int PrimaryPriceTo { get; set; } = 500000;
    public string EstateType { get; set; } = "APARTMENT";
    public string TransferType { get; set; } = "BUY";
    public string UseType { get; set; } = "RESIDENTIAL";
    public int PageSize { get; set; } = 20;
}
