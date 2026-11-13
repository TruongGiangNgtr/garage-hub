using GarageHub.Repository;
using GarageHub.Service;
using GarageHub.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRepositoryLayer(builder.Configuration);
builder.Services.AddServiceLayer();
builder.Services.AddPaymentModule(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandling();

if (!app.Environment.IsDevelopment())
{
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


app.Run();
