namespace Payments.QRTransfer.Configurations
{
    public class IMAPConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public int SecureSocketOptions { get; set; }
    }
}
