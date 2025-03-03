﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class WishlistController : BasePublicController
{
    #region Constructors

    public WishlistController(
        IWorkContextAccessor workContextAccessor,
        IShoppingCartService shoppingCartService,
        ITranslationService translationService,
        ICustomerService customerService,
        IPermissionService permissionService,
        IMediator mediator,
        ShoppingCartSettings shoppingCartSettings)
    {
        _workContextAccessor = workContextAccessor;
        _shoppingCartService = shoppingCartService;
        _translationService = translationService;
        _customerService = customerService;
        _permissionService = permissionService;
        _mediator = mediator;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #endregion

    #region Fields

    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ITranslationService _translationService;
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Wishlist

    [HttpGet]
    [ProducesResponseType(typeof(MiniWishlistModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> SidebarWishlist()
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return Content("");

        var cart = _workContextAccessor.WorkContext.CurrentCustomer.ShoppingCartItems.Where(sci =>
            sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);

        if (!string.IsNullOrEmpty(_workContextAccessor.WorkContext.CurrentStore.Id))
            cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContextAccessor.WorkContext.CurrentStore.Id);

        var model = await _mediator.Send(new GetMiniWishlist {
            Cart = cart.ToList(),
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        return Json(model);
    }

    [HttpGet]
    [ProducesResponseType(typeof(WishlistModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> Index(Guid? customerGuid)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return RedirectToRoute("HomePage");

        var customer = customerGuid.HasValue
            ? await _customerService.GetCustomerByGuid(customerGuid.Value)
            : _workContextAccessor.WorkContext.CurrentCustomer;
        if (customer == null)
            return RedirectToRoute("HomePage");

        var cart = customer.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);

        if (!string.IsNullOrEmpty(_workContextAccessor.WorkContext.CurrentStore.Id))
            cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContextAccessor.WorkContext.CurrentStore.Id);

        var model = await _mediator.Send(new GetWishlist {
            Cart = cart.ToList(),
            Customer = customer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            IsEditable = !customerGuid.HasValue,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return View(model);
    }

    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> UpdateQuantity(UpdateQuantityModel model)
    {
        var warnings = new List<string>();
        if (ModelState.IsValid)
        {
            var cart =
                (await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, ShoppingCartType.Wishlist))
                .FirstOrDefault(x => x.Id == model.ShoppingCartId);
            if (cart != null)
            {
                var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItem(
                    _workContextAccessor.WorkContext.CurrentCustomer,
                    cart.Id, cart.WarehouseId, cart.Attributes, cart.EnteredPrice,
                    cart.RentalStartDateUtc, cart.RentalEndDateUtc,
                    model.Quantity);
                warnings.AddRange(currSciWarnings);
            }
        }
        else
        {
            warnings = ModelState.Values.SelectMany(x => x.Errors.Select(x => x.ErrorMessage)).ToList();
        }

        return Json(new
        {
            success = !warnings.Any(),
            warnings = string.Join(", ", warnings),
            totalproducts =
                (await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, ShoppingCartType.Wishlist))
                .Sum(x => x.Quantity)
        });
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> AddItemToCartFromWishlist(AddCartFromWishlistModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return Json(new { success = false, message = "No permission" });

        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return Json(new { success = false, message = "No permission" });

        var pageCustomer = model.CustomerGuid.HasValue
            ? await _customerService.GetCustomerByGuid(model.CustomerGuid.Value)
            : _workContextAccessor.WorkContext.CurrentCustomer;
        if (pageCustomer == null)
            return Json(new { success = false, message = "Customer not found" });

        var itemCart = pageCustomer.ShoppingCartItems
            .FirstOrDefault(
                sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist && sci.Id == model.ShoppingCartId);

        if (itemCart == null)
            return Json(new { success = false, message = "Shopping cart ident not found" });

        var warnings = (await _shoppingCartService.AddToCart(_workContextAccessor.WorkContext.CurrentCustomer,
            itemCart.ProductId, ShoppingCartType.ShoppingCart,
            _workContextAccessor.WorkContext.CurrentStore.Id, itemCart.WarehouseId,
            itemCart.Attributes, itemCart.EnteredPrice,
            itemCart.RentalStartDateUtc, itemCart.RentalEndDateUtc, itemCart.Quantity,
            validator: new ShoppingCartValidatorOptions { GetRequiredProductWarnings = false })).warnings;

        if (warnings.Any())
            return Json(new { success = false, message = string.Join(',', warnings) });

        if (_shoppingCartSettings.MoveItemsFromWishlistToCart)
            await _shoppingCartService.DeleteShoppingCartItem(_workContextAccessor.WorkContext.CurrentCustomer, itemCart);

        return Json(new { success = true, message = "" });
    }

    [DenySystemAccount]
    [HttpGet]
    public virtual async Task<IActionResult> DeleteItemFromWishlist(string shoppingCartId)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return Json(new { success = false, message = "No permission" });

        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return Json(new { success = false, message = "No permission" });

        var itemCart = _workContextAccessor.WorkContext.CurrentCustomer.ShoppingCartItems
            .FirstOrDefault(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist && sci.Id == shoppingCartId);

        if (itemCart == null)
            return Json(new { success = false, message = "Shopping cart ident not found" });

        await _shoppingCartService.DeleteShoppingCartItem(_workContextAccessor.WorkContext.CurrentCustomer, itemCart);

        return Json(new { success = true, message = "" });
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> EmailWishlist(WishlistEmailAFriendModel model,
        [FromServices] IMessageProviderService messageProviderService,
        [FromServices] CaptchaSettings captchaSettings)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) ||
            !_shoppingCartSettings.EmailWishlistEnabled)
            return Content("");

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, ShoppingCartType.Wishlist);
        if (!cart.Any())
            return Content("");

        if (ModelState.IsValid)
        {
            //email
            await messageProviderService.SendWishlistEmailAFriendMessage(_workContextAccessor.WorkContext.CurrentCustomer,
                _workContextAccessor.WorkContext.CurrentStore,
                _workContextAccessor.WorkContext.WorkingLanguage.Id, model.YourEmailAddress,
                model.FriendEmail, FormatText.ConvertText(model.PersonalMessage));

            model.SuccessfullySent = true;
            model.Result = _translationService.GetResource("Wishlist.EmailAFriend.SuccessfullySent");

            return Json(model);
        }

        //If we got this far, something failed, redisplay form
        model.DisplayCaptcha = captchaSettings.Enabled && captchaSettings.ShowOnEmailWishlistToFriendPage;
        model.Result = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage));

        return Json(model);
    }

    #endregion
}