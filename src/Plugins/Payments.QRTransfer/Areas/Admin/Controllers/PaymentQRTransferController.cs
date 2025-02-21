using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.QRTransfer.Models;

namespace Payments.QRTransfer.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentQRTransferController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;


    public PaymentQRTransferController(IWorkContextAccessor workContextAccessor,
        IStoreService storeService,
        ISettingService settingService,
        ITranslationService translationService)
    {
        _workContextAccessor = workContextAccessor;
        _storeService = storeService;
        _settingService = settingService;
        _translationService = translationService;
    }


    protected virtual async Task<string> GetActiveStore(IStoreService storeService, IWorkContext workContext)
    {
        var stores = await storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault().Id;

        var storeId =
            workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        var store = await storeService.GetStoreById(storeId);

        return store != null ? store.Id : "";
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContextAccessor.WorkContext);
        var qrTransferPaymentSettings = await _settingService.LoadSetting<QRTransferPaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            DescriptionText = qrTransferPaymentSettings.DescriptionText,
            AdditionalFee = qrTransferPaymentSettings.AdditionalFee,
            AdditionalFeePercentage = qrTransferPaymentSettings.AdditionalFeePercentage,
            ShippableProductRequired = qrTransferPaymentSettings.ShippableProductRequired,
            DisplayOrder = qrTransferPaymentSettings.DisplayOrder,
            SkipPaymentInfo = qrTransferPaymentSettings.SkipPaymentInfo,

            ImapUsername = qrTransferPaymentSettings.ImapUsername,
            ImapPassword = qrTransferPaymentSettings.ImapPassword,
            ImapHost = qrTransferPaymentSettings.ImapHost,
            ImapPort = qrTransferPaymentSettings.ImapPort,
            ImapSecureSocketOptions = qrTransferPaymentSettings.ImapSecureSocketOptions,
            FullName = qrTransferPaymentSettings.FullName,
            IBAN = qrTransferPaymentSettings.IBAN,
            BankCode = qrTransferPaymentSettings.BankCode,
            ReferenceNo = qrTransferPaymentSettings.ReferenceNo,
            IsDynamic = qrTransferPaymentSettings.IsDynamic,
            PaymentDescription = qrTransferPaymentSettings.PaymentDescription,
        };
        model.DescriptionText = qrTransferPaymentSettings.DescriptionText;

        model.ActiveStore = storeScope;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContextAccessor.WorkContext);
        var qrTransferPaymentSettings = await _settingService.LoadSetting<QRTransferPaymentSettings>(storeScope);

        //save settings
        qrTransferPaymentSettings.DescriptionText = model.DescriptionText;
        qrTransferPaymentSettings.AdditionalFee = model.AdditionalFee;
        qrTransferPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        qrTransferPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
        qrTransferPaymentSettings.DisplayOrder = model.DisplayOrder;
        qrTransferPaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

        qrTransferPaymentSettings.ImapUsername = model.ImapUsername;
        qrTransferPaymentSettings.ImapPassword = model.ImapPassword;
        qrTransferPaymentSettings.ImapHost = model.ImapHost;
        qrTransferPaymentSettings.ImapPort = model.ImapPort;
        qrTransferPaymentSettings.ImapSecureSocketOptions = model.ImapSecureSocketOptions;

        qrTransferPaymentSettings.FullName = model.FullName;
        qrTransferPaymentSettings.IBAN = model.IBAN;
        qrTransferPaymentSettings.BankCode = model.BankCode;
        qrTransferPaymentSettings.IsDynamic = model.IsDynamic;
        qrTransferPaymentSettings.ReferenceNo = model.ReferenceNo;
        qrTransferPaymentSettings.PaymentDescription = model.PaymentDescription;

        await _settingService.SaveSetting(qrTransferPaymentSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}