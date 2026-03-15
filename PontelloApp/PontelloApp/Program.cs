using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Services;
using PontelloApp.Ultilities;
using QuestPDF.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("PontelloAppContext")
    ?? throw new InvalidOperationException("Connection string 'PontelloAppContext' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDbContext<PontelloAppContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<OrderService>();


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<RecurringOrderService>();

builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddScoped<RecurringOrderProcessorJob>();

builder.Services.AddHangfireServer();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<RecurringOrderProcessorJob>(
    "check-recurring-orders",
    job => job.RunAsync(),
    "* * * * *");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    PontelloAppInitializer.Initialize(serviceProvider: services, DeleteDatabase: true,
        UseMigrations: true, SeedSampleData: true);

}

QuestPDF.Settings.License = LicenseType.Community;

app.Run();
