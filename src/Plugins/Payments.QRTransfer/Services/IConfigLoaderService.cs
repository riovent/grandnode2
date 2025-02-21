using Payments.QRTransfer.Configurations;

namespace Payments.QRTransfer.Services
{
    public interface IConfigLoaderService
    {
        Task<IMAPConfig> CreateIMAPConfig();
    }
}
