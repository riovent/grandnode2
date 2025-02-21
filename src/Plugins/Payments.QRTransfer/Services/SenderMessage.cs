using MailKit.Search;
using MailKit;
using Microsoft.Extensions.Logging;
using Payments.QRTransfer.Configurations;
using MailKit.Net.Imap;
using MailKit.Security;
using AngleSharp.Dom;
using AngleSharp;
using Payments.QRTransfer.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Grand.Business.Core.Interfaces.Common.Stores;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Routing;
using Payments.QRTransfer.Helpers;

namespace Payments.QRTransfer.Services
{
    public class SenderMessage : ISenderMessage
    {
        private readonly ILogger<SenderMessage> _logger;
        private IMAPConfig _config;
        private readonly IStoreService _storeService;
        private readonly LinkGenerator _linkGenerator;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IConfigLoaderService _configLoaderService;
        public SenderMessage(ILogger<SenderMessage> logger, IConfigLoaderService configLoaderService, IStoreService storeService, LinkGenerator linkGenerator)
        {
            _logger = logger;
            _configLoaderService = configLoaderService;
            _storeService = storeService;
            _linkGenerator = linkGenerator;
        }


        public async Task SendMessage()
        {
            await _semaphore.WaitAsync();

            try
            {
                _config = await _configLoaderService.CreateIMAPConfig();
                var cancel = new CancellationTokenSource();
                var client = new ImapClient();


                await ConnectAsync(client, cancel);

                var uids = await client.Inbox.SearchAsync(SearchQuery.NotSeen, cancel.Token);

                if (uids.Count == 0)
                    return; // No new unread messages


                foreach (var uniqueId in uids)
                {
                    var message = client.Inbox.GetMessage(uniqueId);
                    if (!message.From.Mailboxes.Any(m => m.Address == "halkbank.bilgilendirme@bilgi.halkbank.com.tr"))
                        continue;

                    if (!("HESABA GELEN FAST BİLGİLENDİRME FORMU".ContainsTarget(message.Subject)))
                        continue;

                    var completePayment = await Read(message.HtmlBody);

                    var stores = await _storeService.GetAllStores();
                    var store = stores.FirstOrDefault();
                    var httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(store.SslEnabled ? store.SecureUrl : store.Url);
                    var paymentCompletedUrl = _linkGenerator.GetPathByRouteValues(QRTransferPaymentDefaults.PaymentCompleted);
                    var result = await httpClient.PostAsJsonAsync(paymentCompletedUrl, completePayment);
                    if (result.IsSuccessStatusCode)
                        await client.Inbox.AddFlagsAsync(uniqueId, MessageFlags.Seen, true, cancel.Token);
                }

                await DisconnectAsync(client);

                cancel?.Cancel();
                cancel?.Dispose();
                cancel = null;

                client?.Dispose();
                client = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Error");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ConnectAsync(ImapClient client, CancellationTokenSource cancel)
        {
            if (!client.IsConnected)
                await client.ConnectAsync(_config.Host, _config.Port, (SecureSocketOptions)_config.SecureSocketOptions, cancel.Token);

            if (!client.IsAuthenticated)
            {
                await client.AuthenticateAsync(_config.Username, _config.Password, cancel.Token);

                await client.Inbox.OpenAsync(FolderAccess.ReadWrite, cancel.Token);
            }
        }

        private async Task DisconnectAsync(ImapClient client)
        {
            await client.DisconnectAsync(true);
        }

        private async Task<PaymentCompletedModel> Read(string htmlContent)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(htmlContent));
            var completePayment = new PaymentCompletedModel();


            var name = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(1) > td:nth-of-type(1)");
            completePayment.RecipientName = name;

            var date = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(1) > td:nth-of-type(3)");
            string format = "dd.MM.yyyy HH.mm";
            CultureInfo culture = CultureInfo.InvariantCulture;

            DateTime parsedDate = DateTime.ParseExact(date, format, culture);
            completePayment.TransactionDate = parsedDate;

            var sube = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(2) > td > table > tbody > tr:nth-of-type(1) > td:nth-of-type(3)");
            completePayment.RecipientBranch = sube;

            var currencyCode = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(2) > td > table > tbody > tr:nth-of-type(2) > td:nth-of-type(3)");
            completePayment.CurrencyCode = currencyCode;

            var iban = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(2) > td > table > tbody > tr:nth-of-type(3) > td:nth-of-type(3)").Replace(" ", "");
            completePayment.RecipientIBAN = iban;
            completePayment.RecipientBankName = "HalkBank";


            var bankName = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(3) > td > table > tbody > tr:nth-of-type(1) > td:nth-of-type(3)");
            completePayment.SenderBankName = bankName;

            var senderIBAN = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(3) > td > table > tbody > tr:nth-of-type(2) > td:nth-of-type(3)");
            completePayment.SenderIBAN = senderIBAN;

            var senderName = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(3) > td > table > tbody > tr:nth-of-type(3) > td:nth-of-type(3)");
            completePayment.SenderName = senderName;

            var code = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(3) > td > table > tbody > tr:nth-of-type(4) > td:nth-of-type(3)");
            completePayment.TransactionCode = code;

            var validationCode = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(3) > td > table > tbody > tr:nth-of-type(5) > td:nth-of-type(3)");
            completePayment.Description = validationCode;

            var charecterPrice = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(3) > td > table > tbody > tr:nth-of-type(6) > td:nth-of-type(3)");
            completePayment.AmountWords = charecterPrice;

            var price = GetValue(document, "body > table:nth-of-type(1) > tbody > tr > td:nth-of-type(1) > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr > td > table:nth-of-type(1) > tbody > tr:nth-of-type(2) > td > table > tbody > tr > td:nth-of-type(2) > table > tbody > tr:nth-of-type(4) > td:nth-of-type(2)");
            completePayment.Amount = double.Parse(price.Split(" ")[0], new CultureInfo("tr-TR"));

            return completePayment;
        }


        private string GetValue(IDocument document, string selector)
        {
            var element = document.QuerySelector(selector);
            if (element != null)
            {
                return Clear(element);
            }
            return "";
        }

        private string Clear(IElement element)
        {
            return string.Join(" ", element.ChildNodes.Where(p => p.NodeType == NodeType.Text).Select(p => p.TextContent.Trim()).Where(p => p.Length > 0));
        }
    }
}
