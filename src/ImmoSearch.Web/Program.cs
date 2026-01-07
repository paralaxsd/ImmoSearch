using ImmoSearch.Web.Endpoints;
using ImmoSearch.Web.Options;
using ImmoSearch.Web.Services;
using System.Globalization;

PrepareApplication();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.Configure<AppInfoOptions>(builder.Configuration.GetSection("AppInfo"));
builder.Services.AddHttpClient<ListingApiClient>(AssignClientBaseAddressFrom);
builder.Services.AddHttpClient<StatusApiClient>(AssignClientBaseAddressFrom);
builder.Services.AddHttpClient("ApiProxy", AssignClientBaseAddressFrom);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.MapWebEndpoints();
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

static void AssignClientBaseAddressFrom(IServiceProvider sp, HttpClient httpClient)
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["ApiBaseUrl"] ?? "https://localhost:5001";
    httpClient.BaseAddress = new(baseUrl);
}
