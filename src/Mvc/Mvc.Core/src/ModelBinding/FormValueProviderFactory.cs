// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A <see cref="IValueProviderFactory"/> for <see cref="FormValueProvider"/>.
/// </summary>
public class FormValueProviderFactory : IValueProviderFactory
{
    private readonly MvcOptions? _options;

    /// <summary>
    /// Creates a new <see cref="FormValueProviderFactory"/>.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/> options.</param>
    public FormValueProviderFactory(MvcOptions? options)
    {
        _options = options;
    }

    /// <summary>
    /// Creates a new <see cref="FormValueProviderFactory"/>.
    /// </summary>
    public FormValueProviderFactory()
        : this(options: null)
    {
    }

    /// <inheritdoc />
    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var request = context.ActionContext.HttpContext.Request;
        if (request.HasFormContentType)
        {
            // Allocating a Task only when the body is form data.
            return AddValueProviderAsync(context, _options);
        }

        return Task.CompletedTask;
    }

    private static async Task AddValueProviderAsync(ValueProviderFactoryContext context, MvcOptions? options)
    {
        var request = context.ActionContext.HttpContext.Request;
        IFormCollection form;

        try
        {
            form = await request.ReadFormAsync();
        }
        catch (InvalidDataException ex)
        {
            // ReadFormAsync can throw InvalidDataException if the form content is malformed.
            // Wrap it in a ValueProviderException that the CompositeValueProvider special cases.
            throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
        }
        catch (IOException ex)
        {
            // ReadFormAsync can throw IOException if the client disconnects.
            // Wrap it in a ValueProviderException that the CompositeValueProvider special cases.
            throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
        }

        var valueProvider = new FormValueProvider(
            BindingSource.Form,
            form,
            CultureInfo.CurrentCulture,
            options);

        context.ValueProviders.Add(valueProvider);
    }
}
