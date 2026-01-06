using System.Globalization;
using ImmoSearch.Web.Services;

PrepareApplication();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient<ListingApiClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static void PrepareApplication()
{
    var culture = CultureInfo.GetCultureInfo("de-AT");
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;

    Console.WriteLine($"[{DateTime.Now}] Launching ImmoSearch.Web v{ThisAssembly.AssemblyInformationalVersion}");
}
