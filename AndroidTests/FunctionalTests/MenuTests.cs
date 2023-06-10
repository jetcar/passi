using System;
using System.Linq;
using System.Net;
using System.Threading;
using AndroidTests.TestClasses;
using AndroidTests.Tools;
using AppConfig;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Registration;
using passi_android.Tools;
using passi_android.utils.Services;
using RestSharp;
using Xamarin.Forms;
using Color = WebApiDto.Auth.Color;

namespace AndroidTests.FunctionalTests
{
    public class MenuTests : TestBase
    {

        [Test,Timeout(10000)]
        public void UpdateCertificateWithPin()
        {
            var mainView = MainTestClass.OpenMainPage();
          
           
        }
       
    }
}