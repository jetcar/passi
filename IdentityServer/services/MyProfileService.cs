using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models.Contexts;

namespace IdentityServer.services
{
    [GoogleTracer.Profile]
    public class MyProfileService : IProfileService
    {
        public MyProfileService(ILogger<MyProfileService> logger)
        {
            this.Logger = logger;
        }

        public ILogger<MyProfileService> Logger { get; set; }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.LogProfileRequest(Logger);
            context.AddRequestedClaims(context.Subject.Claims);

            context.IssuedClaims.Add(new Claim("email", context.Subject.FindFirstValue("sub")));
            context.IssuedClaims.Add(new Claim("Thumbprint", context.Subject.FindFirstValue("Thumbprint")));
            context.IssuedClaims.Add(new Claim("sessionId", context.Subject.FindFirstValue("sessionId")));

            context.LogIssuedClaims(Logger);
            await Task.FromResult("");
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.FromResult(1);
        }
    }
}