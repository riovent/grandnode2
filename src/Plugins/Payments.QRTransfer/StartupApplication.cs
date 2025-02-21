using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.QRTransfer.Services;
using Payments.QRTransfer.Tasks;

namespace Payments.QRTransfer;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentProvider, QRTransferPaymentProvider>();
        services.AddScoped<IWidgetProvider, QRTransferWidgetProvider>();

        services.AddSingleton<IConfigLoaderService, ConfigLoaderService>();
        services.AddSingleton<ISenderMessage, SenderMessage>();
        services.AddSingleton<IQRTransferPaymentService, QRTransferPaymentService>();

        services.AddKeyedScoped<IScheduleTask, IdleMailScheduleTask>(QRTransferPaymentDefaults.ScheduleTaskName);

    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {

    }

    public bool BeforeConfigure => false;
}