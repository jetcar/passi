using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace IdentityServer;

public class ValidatePromptParameter : IOpenIddictServerHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>
{
    /// <summary>
    /// Gets the default descriptor definition assigned to this handler.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ValidateAuthorizationRequestContext>()
            .UseSingletonHandler<ValidatePromptParameter>()
            .SetOrder(OpenIddictServerHandlers.Authentication.ValidateNonceParameter.Descriptor.Order + 1_000)
            .SetType(OpenIddictServerHandlerType.BuiltIn)
            .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(OpenIddictServerEvents.ValidateAuthorizationRequestContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Reject requests specifying prompt=none with consent/login or select_account.
        //if (context.Request.HasPrompt(Prompts.None) && (context.Request.HasPrompt(Prompts.Consent) ||
        //                                                context.Request.HasPrompt(Prompts.Login) ||
        //                                                context.Request.HasPrompt(Prompts.SelectAccount)))
        //{
        //    context.Logger.LogInformation(SR.GetResourceString(SR.ID6040));

        //    context.Reject(
        //        error: OpenIddictConstants.Errors.InvalidRequest,
        //        description: SR.FormatID2052(OpenIddictConstants.Parameters.Prompt),
        //        uri: SR.FormatID8000(SR.ID2052));

        //    return default;
        //}

        return default;
    }
}
