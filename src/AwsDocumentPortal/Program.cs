using Amazon;
using Amazon.S3;
using AwsDocumentPortal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var regionName = builder.Configuration["AWS:Region"] ?? "ap-south-1";
builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(RegionEndpoint.GetBySystemName(regionName)));

builder.Services.AddSingleton<ILocalDocumentStorage, LocalDocumentStorage>();
builder.Services.AddSingleton<IS3DocumentStorage, S3DocumentStorage>();
builder.Services.AddSingleton<IDocumentStorage, StorageProviderDocumentStorage>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();