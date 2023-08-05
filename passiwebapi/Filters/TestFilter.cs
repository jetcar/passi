using System;
using ConfigurationManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace passi_webapi.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestFilterAttribute : TypeFilterAttribute
    {
        public TestFilterAttribute() : base(typeof(CustomAuthorizationFilter))
        {
        }
    }

    public class CustomAuthorizationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var appsettings = context.HttpContext.RequestServices.GetService(typeof(AppSetting)) as AppSetting;
            if (appsettings["IsTest"] != "true")
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }

}