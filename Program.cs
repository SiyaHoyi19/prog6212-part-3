using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = "RaceDay.Mvc.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    var url = builder.Configuration["RaceDayApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("RaceDayApi:BaseUrl is missing.");
    client.BaseAddress = new Uri(url);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });
builder.Services.AddScoped<RaceDayApiClient>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
