using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;

namespace IdentityServer;

public class PublicFacingUrlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _publicFacingUri;

    public PublicFacingUrlMiddleware(RequestDelegate next, string publicFacingUri)
    {
        _publicFacingUri = publicFacingUri;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;

        context.SetIdentityServerOrigin(_publicFacingUri);
        context.SetIdentityServerBasePath(request.PathBase.Value.TrimEnd('/'));

        await _next(context);
    }
}