﻿using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers.Admin;

[HtmlTargetElement("admin-label", Attributes = ForAttributeName)]
public class LabelRequiredTagHelper : LabelTagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string DisplayHintAttributeName = "asp-display-hint";
    private const string RequiredAttributeName = "asp-required";
    private readonly ITranslationService _translationService;

    private readonly IWorkContextAccessor _workContextAccessor;

    public LabelRequiredTagHelper(IHtmlGenerator generator, IWorkContextAccessor workContextAccessor,
        ITranslationService translationService) : base(generator)
    {
        _workContextAccessor = workContextAccessor;
        _translationService = translationService;
    }

    [HtmlAttributeName(DisplayHintAttributeName)]
    public bool DisplayHint { get; set; } = true;

    [HtmlAttributeName(RequiredAttributeName)]
    public bool RequiredAttribute { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);
        output.TagName = "label";
        output.TagMode = TagMode.StartTagAndEndTag;
        var classValue = output.Attributes.ContainsName("class")
            ? $"{output.Attributes["class"].Value}"
            : "control-label col-md-3 col-sm-3";
        if (RequiredAttribute) classValue += " required";
        output.Attributes.SetAttribute("class", classValue);

        var resourceDisplayName = For.Metadata.GetDisplayName();

        var langId = _workContextAccessor.WorkContext.WorkingLanguage.Id;

        var resource = _translationService.GetResource(
            resourceDisplayName.ToLowerInvariant(), langId, returnEmptyIfNotFound: true);

        if (!string.IsNullOrEmpty(resource)) output.Content.SetContent(resource);

        if (DisplayHint)
        {
            var hintResource = _translationService.GetResource(
                resourceDisplayName + ".Hint", langId, returnEmptyIfNotFound: true);

            if (!string.IsNullOrEmpty(hintResource)) output.Content.AppendHtml($"<p class='hint'>{hintResource}</p>");
        }
    }
}