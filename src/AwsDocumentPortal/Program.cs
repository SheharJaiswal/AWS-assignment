using Amazon;
using Amazon.S3;
using AwsDocumentPortal.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var regionName = builder.Configuration["AWS:Region"] ?? "us-east-1";

builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(
        RegionEndpoint.GetBySystemName(regionName)));

builder.Services.AddSingleton<S3DataProtectionKeyRepository>();

builder.Services
    .AddDataProtection()
    .SetApplicationName("AwsDocumentPortal")
    .AddKeyManagementOptions(options =>
    {
        options.XmlRepository =
            new S3DataProtectionKeyRepository(
                new AmazonS3Client(
                    RegionEndpoint.GetBySystemName(regionName)),
                builder.Configuration);
    });

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