﻿using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Attributes;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Common;
using Grand.Web.Models.Customer;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class AccountController : BasePublicController
{
    #region Ctor

    public AccountController(
        IGrandAuthenticationService authenticationService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor,
        ICustomerService customerService,
        IGroupService groupService,
        ICustomerManagerService customerManagerService,
        ICountryService countryService,
        IMediator mediator,
        IMessageProviderService messageProviderService,
        CaptchaSettings captchaSettings,
        CustomerSettings customerSettings)
    {
        _authenticationService = authenticationService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
        _customerService = customerService;
        _groupService = groupService;
        _customerManagerService = customerManagerService;
        _customerSettings = customerSettings;
        _countryService = countryService;
        _messageProviderService = messageProviderService;
        _captchaSettings = captchaSettings;
        _mediator = mediator;
    }

    #endregion

    #region My account / Auctions

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Auctions()
    {
        if (_customerSettings.HideAuctionsTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetAuctions
            { Customer = _workContextAccessor.WorkContext.CurrentCustomer, Language = _workContextAccessor.WorkContext.WorkingLanguage });

        return View(model);
    }

    #endregion

    #region My account / Notes

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Notes()
    {
        if (_customerSettings.HideNotesTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetNotes { Customer = _workContextAccessor.WorkContext.CurrentCustomer });

        return View(model);
    }

    #endregion

    #region My account / Documents

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Documents(DocumentPagingModel command)
    {
        if (_customerSettings.HideDocumentsTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetDocuments {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Command = command
        });

        return View(model);
    }

    #endregion

    #region My account / Reviews

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Reviews()
    {
        if (_customerSettings.HideReviewsTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetReviews
            { Customer = _workContextAccessor.WorkContext.CurrentCustomer, Language = _workContextAccessor.WorkContext.WorkingLanguage });

        return View(model);
    }

    #endregion

    #region My account / Courses

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Courses()
    {
        if (_customerSettings.HideCoursesTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetCourses
            { Customer = _workContextAccessor.WorkContext.CurrentCustomer, Store = _workContextAccessor.WorkContext.CurrentStore });

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IGrandAuthenticationService _authenticationService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ICountryService _countryService;
    private readonly IMediator _mediator;
    private readonly IMessageProviderService _messageProviderService;
    private readonly CustomerSettings _customerSettings;
    private readonly CaptchaSettings _captchaSettings;

    #endregion

    #region Login / logout

    //available even when navigation is not allowed
    [PublicStore(true)]
    [ClosedStore(true)]
    [IgnoreApi]
    public virtual IActionResult Login(bool? checkoutAsGuest)
    {
        var model = new LoginModel {
            UsernamesEnabled = _customerSettings.UsernamesEnabled,
            CheckoutAsGuest = checkoutAsGuest.GetValueOrDefault(),
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage
        };
        return View(model);
    }

    [HttpPost]
    //available even when navigation is not allowed
    [PublicStore(true)]
    [ClosedStore(true)]
    [AutoValidateAntiforgeryToken]
    [IgnoreApi]
    public virtual async Task<IActionResult> Login(LoginModel model, string returnUrl)
    {
        if (ModelState.IsValid)
        {
            var loginResult =
                await _customerManagerService.LoginCustomer(
                    _customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password);
            switch (loginResult)
            {
                case CustomerLoginResults.Successful:
                {
                    var customer = _customerSettings.UsernamesEnabled
                        ? await _customerService.GetCustomerByUsername(model.Username)
                        : await _customerService.GetCustomerByEmail(model.Email);
                    //sign in
                    return await SignInAction(customer, model.RememberMe, returnUrl);
                }
                case CustomerLoginResults.RequiresTwoFactor:
                {
                    var userName = _customerSettings.UsernamesEnabled ? model.Username : model.Email;
                    HttpContext.Session.SetString("RequiresTwoFactor", userName);
                    return RedirectToRoute("TwoFactorAuthorization");
                }
            }
        }

        //If we got this far, something failed, redisplay form
        model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage;

        return View(model);
    }

    [IgnoreApi]
    public async Task<IActionResult> TwoFactorAuthorization()
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("Login");

        var username = HttpContext.Session.GetString("RequiresTwoFactor");
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute("HomePage");

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(username)
            : await _customerService.GetCustomerByEmail(username);
        if (customer == null)
            return RedirectToRoute("HomePage");

        if (!customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute("HomePage");

        if (_customerSettings.TwoFactorAuthenticationType != TwoFactorAuthenticationType.AppVerification)
            await _mediator.Send(new GetTwoFactorAuthentication {
                Customer = customer,
                Language = _workContextAccessor.WorkContext.WorkingLanguage,
                Store = _workContextAccessor.WorkContext.CurrentStore
            });

        return View();
    }

    [IgnoreApi]
    [HttpPost]
    public async Task<IActionResult> TwoFactorAuthorization(string token,
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("Login");

        var username = HttpContext.Session.GetString("RequiresTwoFactor");
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute("HomePage");

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(username)
            : await _customerService.GetCustomerByEmail(username);
        if (customer == null)
            return RedirectToRoute("Login");

        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
        }
        else
        {
            var secretKey = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorSecretKey);
            if (await twoFactorAuthenticationService.AuthenticateTwoFactor(secretKey, token, customer,
                    _customerSettings.TwoFactorAuthenticationType))
            {
                //remove session
                HttpContext.Session.Remove("RequiresTwoFactor");

                //sign in
                return await SignInAction(customer);
            }

            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
        }

        await _mediator.Publish(new CustomerLoginFailedEvent(customer));

        return View();
    }

    [IgnoreApi]
    private async Task<IActionResult> SignInAction(Customer customer, bool createPersistentCookie = false,
        string returnUrl = null)
    {
        //raise event       
        await _mediator.Publish(new CustomerLoggedInEvent(customer));

        //sign in new customer
        await _authenticationService.SignIn(customer, createPersistentCookie);

        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return RedirectToRoute("HomePage");

        return Redirect(returnUrl);
    }

    //available even when a store is closed
    [ClosedStore(true)]
    //available even when navigation is not allowed
    [PublicStore(true)]
    [IgnoreApi]
    public virtual async Task<IActionResult> Logout(
        [FromServices] StoreInformationSettings storeInformationSettings)
    {
        if (_workContextAccessor.WorkContext.OriginalCustomerIfImpersonated != null)
        {
            //logout impersonated customer
            await _customerService.UpdateUserField<int?>(_workContextAccessor.WorkContext.OriginalCustomerIfImpersonated,
                SystemCustomerFieldNames.ImpersonatedCustomerId, null);

            //redirect back to customer details page (admin area)
            return RedirectToAction("Edit", "Customer",
                new { id = _workContextAccessor.WorkContext.CurrentCustomer.Id, area = "Admin" });
        }

        //raise event       
        await _mediator.Publish(new CustomerLoggedOutEvent(_workContextAccessor.WorkContext.CurrentCustomer));

        //standard logout 
        await _authenticationService.SignOut();

        //Cookie
        if (storeInformationSettings.DisplayCookieInformation) TempData["Grand.IgnoreCookieInformation"] = true;

        return RedirectToRoute("HomePage");
    }

    #endregion

    #region Password recovery

    //available even when navigation is not allowed
    [PublicStore(true)]
    [HttpGet]
    public virtual IActionResult PasswordRecovery()
    {
        var model = new PasswordRecoveryModel {
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnPasswordRecoveryPage
        };
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PublicStore(true)]
    [ProducesResponseType(typeof(PasswordRecoveryModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> PasswordRecovery(PasswordRecoveryModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var customer = await _customerService.GetCustomerByEmail(model.Email);
        await _mediator.Send(new PasswordRecoverySendCommand {
            Customer = customer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Model = model
        });

        model.Result = _translationService.GetResource("Account.PasswordRecovery.EmailHasBeenSent");
        model.Send = true;
        return View(model);
    }

    [HttpGet]
    [PublicStore(true)]
    [ProducesResponseType(typeof(PasswordRecoveryConfirmModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> PasswordRecoveryConfirm(string token, string email)
    {
        var customer = await _customerService.GetCustomerByEmail(email);
        if (customer == null)
            return RedirectToRoute("HomePage");

        var model = await _mediator.Send(new GetPasswordRecoveryConfirm { Customer = customer, Token = token });

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    //available even when navigation is not allowed
    [PublicStore(true)]
    public virtual async Task<IActionResult> PasswordRecoveryConfirm(PasswordRecoveryConfirmModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var customer = await _customerService.GetCustomerByEmail(model.Email);

        await _customerManagerService.ChangePassword(new ChangePasswordRequest(model.Email,
            _customerSettings.DefaultPasswordFormat, model.NewPassword));

        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordRecoveryToken, "");

        model.DisablePasswordChanging = true;
        model.Result = _translationService.GetResource("Account.PasswordRecovery.PasswordHasBeenChanged");

        return View(model);
    }

    #endregion

    #region Register

    //available even when navigation is not allowed
    [PublicStore(true)]
    [HttpGet]
    [ProducesResponseType(typeof(RegisterModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> Register()
    {
        //check whether registration is allowed
        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });

        //check if customer is registered.
        if (await _groupService.IsRegistered(_workContextAccessor.WorkContext.CurrentCustomer)) return RedirectToRoute("HomePage");

        var model = await _mediator.Send(new GetRegister {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            ExcludeProperties = false,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    //available even when navigation is not allowed
    [PublicStore(true)]
    public virtual async Task<IActionResult> Register(RegisterModel model, string returnUrl)
    {
        //check whether registration is allowed
        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });

        //check if customer is registered. 
        if (await _groupService.IsRegistered(_workContextAccessor.WorkContext.CurrentCustomer)) return RedirectToRoute("HomePage");

        if (ModelState.IsValid)
        {
            if (_customerSettings.UsernamesEnabled && model.Username != null) model.Username = model.Username.Trim();

            var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
            var registrationRequest = new RegistrationRequest(_workContextAccessor.WorkContext.CurrentCustomer, model.Email,
                _customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password,
                _customerSettings.DefaultPasswordFormat, _workContextAccessor.WorkContext.CurrentStore.Id, isApproved);
            await _customerManagerService.RegisterCustomer(registrationRequest);

            var customerAttributes = await _mediator.Send(new GetParseCustomAttributes
                { SelectedAttributes = model.SelectedAttributes });

            await _mediator.Send(new CustomerRegisteredCommand {
                Customer = _workContextAccessor.WorkContext.CurrentCustomer,
                CustomerAttributes = customerAttributes,
                Model = model,
                Store = _workContextAccessor.WorkContext.CurrentStore
            });

            //login customer now
            if (isApproved)
                await _authenticationService.SignIn(_workContextAccessor.WorkContext.CurrentCustomer, true);

            //raise event       
            await _mediator.Publish(new CustomerRegisteredEvent(_workContextAccessor.WorkContext.CurrentCustomer));

            switch (_customerSettings.UserRegistrationType)
            {
                case UserRegistrationType.EmailValidation:
                {
                    //email validation message
                    await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.AccountActivationToken, Guid.NewGuid().ToString());
                    await _messageProviderService.SendCustomerEmailValidationMessage(
                        _workContextAccessor.WorkContext.CurrentCustomer, _workContextAccessor.WorkContext.CurrentStore,
                        _workContextAccessor.WorkContext.WorkingLanguage.Id);

                    //result
                    return RedirectToRoute("RegisterResult",
                        new { resultId = (int)UserRegistrationType.EmailValidation });
                }
                case UserRegistrationType.AdminApproval:
                {
                    return RedirectToRoute("RegisterResult",
                        new { resultId = (int)UserRegistrationType.AdminApproval });
                }
                case UserRegistrationType.Standard:
                {
                    //send customer welcome message
                    await _messageProviderService.SendCustomerWelcomeMessage(_workContextAccessor.WorkContext.CurrentCustomer,
                        _workContextAccessor.WorkContext.CurrentStore, _workContextAccessor.WorkContext.WorkingLanguage.Id);

                    var redirectUrl = Url.RouteUrl("RegisterResult",
                        new { resultId = (int)UserRegistrationType.Standard }, HttpContext.Request.Scheme);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        redirectUrl = CommonExtensions.ModifyQueryString(redirectUrl, "returnurl", returnUrl);

                    return Redirect(redirectUrl);
                }
                default:
                {
                    return RedirectToRoute("HomePage");
                }
            }
        }

        //If we got this far, something failed, redisplay form
        model = await _mediator.Send(new GetRegister {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            ExcludeProperties = true,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Model = model,
            OverrideCustomCustomerAttributes = await _mediator.Send(new GetParseCustomAttributes
                { SelectedAttributes = model.SelectedAttributes })
        });

        return View(model);
    }

    //available even when navigation is not allowed
    [PublicStore(true)]
    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult RegisterResult(int resultId)
    {
        var resultText = "";
        switch ((UserRegistrationType)resultId)
        {
            case UserRegistrationType.Disabled:
                resultText = _translationService.GetResource("Account.Register.Result.Disabled");
                break;
            case UserRegistrationType.Standard:
                resultText = _translationService.GetResource("Account.Register.Result.Standard");
                break;
            case UserRegistrationType.AdminApproval:
                resultText = _translationService.GetResource("Account.Register.Result.AdminApproval");
                break;
            case UserRegistrationType.EmailValidation:
                resultText = _translationService.GetResource("Account.Register.Result.EmailValidation");
                break;
        }

        var model = new RegisterResultModel {
            Result = resultText
        };
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    //available even when navigation is not allowed
    [PublicStore(true)]
    public virtual async Task<IActionResult> CheckUsernameAvailability(string username)
    {
        var usernameAvailable = false;
        var statusText = _translationService.GetResource("Account.CheckUsernameAvailability.NotAvailable");

        if (!_customerSettings.UsernamesEnabled || string.IsNullOrWhiteSpace(username))
            return Json(new { Available = false, Text = statusText });

        if (_workContextAccessor.WorkContext.CurrentCustomer is { Username: not null } &&
            _workContextAccessor.WorkContext.CurrentCustomer.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
        {
            statusText = _translationService.GetResource("Account.CheckUsernameAvailability.CurrentUsername");
        }
        else
        {
            var customer = await _customerService.GetCustomerByUsername(username);
            if (customer != null) return Json(new { Available = false, Text = statusText });
            statusText = _translationService.GetResource("Account.CheckUsernameAvailability.Available");
            usernameAvailable = true;
        }

        return Json(new { Available = usernameAvailable, Text = statusText });
    }

    //available even when navigation is not allowed
    [HttpGet]
    [PublicStore(true)]
    public virtual async Task<IActionResult> AccountActivation(string token, string email)
    {
        var customer = await _customerService.GetCustomerByEmail(email);
        if (customer == null)
            return RedirectToRoute("HomePage");

        var activationToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AccountActivationToken);
        if (string.IsNullOrEmpty(activationToken))
            return RedirectToRoute("HomePage");

        if (!activationToken.Equals(token, StringComparison.OrdinalIgnoreCase))
            return RedirectToRoute("HomePage");

        //activate user account
        customer.Active = true;
        customer.StoreId = _workContextAccessor.WorkContext.CurrentStore.Id;
        await _customerService.UpdateActive(customer);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.AccountActivationToken, "");

        //send welcome message
        await _messageProviderService.SendCustomerWelcomeMessage(customer, _workContextAccessor.WorkContext.CurrentStore,
            _workContextAccessor.WorkContext.WorkingLanguage.Id);

        var model = new AccountActivationModel {
            Result = _translationService.GetResource("Account.AccountActivation.Activated")
        };
        return View(model);
    }

    #endregion

    #region My account / Info

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(CustomerInfoModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> Info()
    {
        var model = await _mediator.Send(new GetInfo {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            ExcludeProperties = false,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(CustomerInfoModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> Info(CustomerInfoModel model)
    {
        if (ModelState.IsValid)
        {
            await _mediator.Send(new UpdateCustomerInfoCommand {
                Customer = _workContextAccessor.WorkContext.CurrentCustomer,
                CustomerAttributes = await _mediator.Send(new GetParseCustomAttributes {
                    SelectedAttributes = model.SelectedAttributes,
                    CustomerCustomAttribute = _workContextAccessor.WorkContext.CurrentCustomer.Attributes.ToList()
                }),
                Model = model,
                OriginalCustomerIfImpersonated = _workContextAccessor.WorkContext.OriginalCustomerIfImpersonated,
                Store = _workContextAccessor.WorkContext.CurrentStore
            });
            return RedirectToRoute("CustomerInfo");
        }

        //If we got this far, something failed, redisplay form
        model = await _mediator.Send(new GetInfo {
            Model = model,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            ExcludeProperties = true,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            OverrideCustomCustomerAttributes = await _mediator.Send(new GetParseCustomAttributes {
                SelectedAttributes = model.SelectedAttributes,
                CustomerCustomAttribute = _workContextAccessor.WorkContext.CurrentCustomer.Attributes.ToList()
            })
        });

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> RemoveExternalAssociation(string id,
        [FromServices] IExternalAuthenticationService openAuthenticationService)
    {
        //ensure it's our record
        var ear = (await openAuthenticationService.GetExternalIdentifiers(_workContextAccessor.WorkContext.CurrentCustomer))
            .FirstOrDefault(x => x.Id == id);

        if (ear == null)
            return Json(new {
                redirect = Url.Action("Info")
            });

        await openAuthenticationService.DeleteExternalAuthentication(ear);

        return Json(new {
            redirect = Url.Action("Info")
        });
    }

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Export()
    {
        if (!_customerSettings.AllowUsersToExportData)
            return Challenge();

        var model = await _mediator.Send(new GetCustomerData(_workContextAccessor.WorkContext.CurrentCustomer));

        return File(model, "text/xls", "PersonalInfo.xlsx");
    }

    #endregion

    #region My account / Addresses

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(CustomerAddressListModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> Addresses()
    {
        var model = await _mediator.Send(new GetAddressList {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> AddressDelete(string addressId)
    {
        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        //find address (ensure that it belongs to the current customer)
        var address = customer.Addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            return Json(new {
                redirect = Url.RouteUrl("CustomerAddresses")
            });
        customer.RemoveAddress(address);
        await _customerService.DeleteAddress(address, customer.Id);

        return Json(new {
            redirect = Url.RouteUrl("CustomerAddresses")
        });
    }

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(CustomerAddressEditModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> AddressAdd()
    {
        var countries =
            await _countryService.GetAllCountries(_workContextAccessor.WorkContext.WorkingLanguage.Id, _workContextAccessor.WorkContext.CurrentStore.Id);
        var model = new CustomerAddressEditModel {
            Address = await _mediator.Send(new GetAddressModel {
                Language = _workContextAccessor.WorkContext.WorkingLanguage,
                Store = _workContextAccessor.WorkContext.CurrentStore,
                Model = null,
                Address = null,
                ExcludeProperties = false,
                PrePopulateWithCustomerFields = true,
                Customer = _workContextAccessor.WorkContext.CurrentCustomer,
                LoadCountries = () => countries
            })
        };

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(AddressModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> AddressAdd(CustomerAddressEditModel model,
        [FromServices] AddressSettings addressSettings)
    {
        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        if (ModelState.IsValid)
        {
            var address = model.Address.ToEntity(_workContextAccessor.WorkContext.CurrentCustomer, addressSettings);
            address.Attributes = await _mediator.Send(new GetParseCustomAddressAttributes
                { SelectedAttributes = model.Address.SelectedAttributes });
            customer.Addresses.Add(address);

            await _customerService.InsertAddress(address, customer.Id);

            return RedirectToRoute("CustomerAddresses");
        }

        var countries =
            await _countryService.GetAllCountries(_workContextAccessor.WorkContext.WorkingLanguage.Id, _workContextAccessor.WorkContext.CurrentStore.Id);
        //If we got this far, something failed, redisplay form
        model.Address = await _mediator.Send(new GetAddressModel {
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Model = model.Address,
            Address = null,
            ExcludeProperties = true,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            LoadCountries = () => countries,
            OverrideAttributes = await _mediator.Send(new GetParseCustomAddressAttributes
                { SelectedAttributes = model.Address.SelectedAttributes })
        });

        return View(model);
    }

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(CustomerAddressEditModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> AddressEdit(string addressId)
    {
        var customer = _workContextAccessor.WorkContext.CurrentCustomer;
        //find address (ensure that it belongs to the current customer)
        var address = customer.Addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            //address is not found
            return RedirectToRoute("CustomerAddresses");

        var countries =
            await _countryService.GetAllCountries(_workContextAccessor.WorkContext.WorkingLanguage.Id, _workContextAccessor.WorkContext.CurrentStore.Id);
        var model = new CustomerAddressEditModel();
        model.Address = await _mediator.Send(new GetAddressModel {
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Model = model.Address,
            Address = address,
            ExcludeProperties = false,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            LoadCountries = () => countries
        });

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    [ProducesResponseType(typeof(CustomerAddressEditModel), StatusCodes.Status200OK)]

    public virtual async Task<IActionResult> AddressEdit(CustomerAddressEditModel model,
        [FromServices] AddressSettings addressSettings)
    {
        var customer = _workContextAccessor.WorkContext.CurrentCustomer;
        //find address (ensure that it belongs to the current customer)
        var address = customer.Addresses.FirstOrDefault(a => a.Id == model.Address.Id);
        if (address == null)
            //address is not found
            return RedirectToRoute("CustomerAddresses");

        if (ModelState.IsValid)
        {
            address = model.Address.ToEntity(address, _workContextAccessor.WorkContext.CurrentCustomer, addressSettings);
            address.Attributes = await _mediator.Send(new GetParseCustomAddressAttributes
                { SelectedAttributes = model.Address.SelectedAttributes });
            await _customerService.UpdateAddress(address, customer.Id);

            if (customer.BillingAddress?.Id == address.Id)
                await _customerService.UpdateBillingAddress(address, customer.Id);
            if (customer.ShippingAddress?.Id == address.Id)
                await _customerService.UpdateShippingAddress(address, customer.Id);

            return RedirectToRoute("CustomerAddresses");
        }

        var countries =
            await _countryService.GetAllCountries(_workContextAccessor.WorkContext.WorkingLanguage.Id, _workContextAccessor.WorkContext.CurrentStore.Id);
        //If we got this far, something failed, redisplay form
        model.Address = await _mediator.Send(new GetAddressModel {
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Model = model.Address,
            Address = address,
            ExcludeProperties = true,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            LoadCountries = () => countries,
            OverrideAttributes = await _mediator.Send(new GetParseCustomAddressAttributes
                { SelectedAttributes = model.Address.SelectedAttributes })
        });

        return View(model);
    }

    #endregion

    #region My account / Downloadable products

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> DownloadableProducts()
    {
        if (_customerSettings.HideDownloadableProductsTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetDownloadableProducts {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Language = _workContextAccessor.WorkContext.WorkingLanguage
        });
        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> UserAgreement(Guid orderItemId)
    {
        var model = await _mediator.Send(new GetUserAgreement { OrderItemId = orderItemId });
        return model == null ? RedirectToRoute("HomePage") : View(model);
    }

    #endregion

    #region My account / Change password

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> ChangePassword()
    {
        var model = new ChangePasswordModel {
            PasswordIsExpired = await _mediator.Send(new GetPasswordIsExpiredQuery
                { Customer = _workContextAccessor.WorkContext.CurrentCustomer })
        };

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var changePasswordRequest = new ChangePasswordRequest(_workContextAccessor.WorkContext.CurrentCustomer.Email,
            _customerSettings.DefaultPasswordFormat, model.NewPassword, model.OldPassword);

        await _customerManagerService.ChangePassword(changePasswordRequest);

        //sign in
        await _authenticationService.SignIn(_workContextAccessor.WorkContext.CurrentCustomer, true);

        model.Result = _translationService.GetResource("Account.ChangePassword.Success");
        return View(model);
    }

    #endregion

    #region My account / Delete account

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual IActionResult DeleteAccount()
    {
        if (!_customerSettings.AllowUsersToDeleteAccount)
            return RedirectToRoute("CustomerInfo");

        var model = new DeleteAccountModel();

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> DeleteAccount(DeleteAccountModel model)
    {
        if (!_customerSettings.AllowUsersToDeleteAccount)
            return RedirectToRoute("CustomerInfo");

        if (!ModelState.IsValid) return View(model);

        //delete account 
        await _mediator.Send(new DeleteAccountCommand {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            IpAddress = HttpContext.Connection?.RemoteIpAddress?.ToString()
        });

        //standard logout 
        await _authenticationService.SignOut();

        //Show success full message 
        Success(_translationService.GetResource("Account.Delete.Success"));

        return RedirectToRoute("HomePage");
    }

    #endregion

    #region My account / TwoFactorAuth

    [IgnoreApi]
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public async Task<IActionResult> EnableTwoFactorAuthenticator()
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("CustomerInfo");

        if (_workContextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetTwoFactorAuthentication {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(model);
    }

    [IgnoreApi]
    [HttpPost]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public async Task<IActionResult> EnableTwoFactorAuthenticator(
        CustomerInfoModel.TwoFactorAuthenticationModel model,
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("CustomerInfo");

        if (_workContextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute("CustomerInfo");

        if (string.IsNullOrEmpty(model.Code))
        {
            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
        }
        else
        {
            if (await twoFactorAuthenticationService.AuthenticateTwoFactor(model.SecretKey, model.Code,
                    _workContextAccessor.WorkContext.CurrentCustomer, _customerSettings.TwoFactorAuthenticationType))
            {
                await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.TwoFactorEnabled, true);
                await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.TwoFactorSecretKey, model.SecretKey);

                Success(_translationService.GetResource("Account.TwoFactorAuth.Enabled"));

                return RedirectToRoute("CustomerInfo");
            }

            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
        }

        return View(model);
    }

    [IgnoreApi]
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public async Task<IActionResult> DisableTwoFactorAuthenticator()
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("CustomerInfo");

        if (!_workContextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute("CustomerInfo");

        _ = await _mediator.Send(new GetTwoFactorAuthentication {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        var model = new CustomerInfoModel.TwoFactorAuthorizationModel {
            TwoFactorAuthenticationType = _customerSettings.TwoFactorAuthenticationType
        };
        return View(model);
    }

    [IgnoreApi]
    [HttpPost]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public async Task<IActionResult> DisableTwoFactorAuthenticator(
        CustomerInfoModel.TwoFactorAuthorizationModel model,
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("CustomerInfo");

        if (!_workContextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute("CustomerInfo");

        if (string.IsNullOrEmpty(model.Code))
        {
            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
        }
        else
        {
            var secretKey =
                _workContextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                    .TwoFactorSecretKey);
            if (await twoFactorAuthenticationService.AuthenticateTwoFactor(secretKey, model.Code,
                    _workContextAccessor.WorkContext.CurrentCustomer, _customerSettings.TwoFactorAuthenticationType))
            {
                await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.TwoFactorEnabled, false);
                await _customerService.UpdateUserField<string>(_workContextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.TwoFactorSecretKey, null);

                Success(_translationService.GetResource("Account.TwoFactorAuth.Disabled"));

                return RedirectToRoute("CustomerInfo");
            }

            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
        }

        return View(model);
    }

    #endregion

    #region My account / Sub accounts

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> SubAccounts()
    {
        if (!await _groupService.IsOwner(_workContextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        if (_customerSettings.HideSubAccountsTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetSubAccounts { Customer = _workContextAccessor.WorkContext.CurrentCustomer });

        return View(model);
    }

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> SubAccountAdd()
    {
        if (!await _groupService.IsOwner(_workContextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        var model = new SubAccountCreateModel {
            Active = true
        };
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> SubAccountAdd(SubAccountCreateModel model)
    {
        if (!await _groupService.IsOwner(_workContextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        if (!ModelState.IsValid) return View(model);

        await _mediator.Send(new SubAccountAddCommand {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Model = model,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        return RedirectToRoute("CustomerSubAccounts");
    }

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> SubAccountEdit(string id)
    {
        if (!await _groupService.IsOwner(_workContextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        var model = await _mediator.Send(new GetSubAccount
            { CustomerId = id, CurrentCustomer = _workContextAccessor.WorkContext.CurrentCustomer });

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> SubAccountEdit(SubAccountEditModel model)
    {
        if (!await _groupService.IsOwner(_workContextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        if (!ModelState.IsValid) return View(model);

        _ = await _mediator.Send(new SubAccountEditCommand {
            CurrentCustomer = _workContextAccessor.WorkContext.CurrentCustomer,
            EditModel = model,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        return RedirectToRoute("CustomerSubAccounts");
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> SubAccountDelete(string id)
    {
        if (!await _groupService.IsOwner(_workContextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        var result = await _mediator.Send(new SubAccountDeleteCommand {
            CurrentCustomer = _workContextAccessor.WorkContext.CurrentCustomer,
            CustomerId = id
        });

        if (result.success)
            return Json(new {
                redirect = Url.RouteUrl("CustomerSubAccounts"),
                success = true
            });
        //errors
        return Json(new {
            redirect = Url.RouteUrl("CustomerSubAccounts"),
            success = false,
            error = string.Join("; ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage))
        });
    }

    #endregion
}