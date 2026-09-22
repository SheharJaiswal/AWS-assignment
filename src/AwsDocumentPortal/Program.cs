using Amazon.S3;
using AwsDocumentPortal.Services;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddSingleton<ILocalDocumentStorage,LocalDocumentStorage>();
builder.Services.AddSingleton<IS3DocumentStorage,S3DocumentStorage>();
builder.Services.AddSingleton<IDocumentStorage,StorageProviderDocumentStorage>();
var app=builder.Build();
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Home/Error");app.UseHsts();}
app.UseHttpsRedirection();app.UseStaticFiles();app.UseRouting();app.UseAuthorization();
app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
app.Run();