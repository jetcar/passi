using MauiTest.TestClasses;
using MauiTest.Tools;
using NUnit.Framework;

namespace MauiTest.FunctionalTests
{
    public class MenuTests : TestBase
    {
        [Test, Timeout(10000)]
        public void UpdateCertificateWithPin()
        {
            var mainView = MainTestClass.OpenMainPage();
        }
    }
}