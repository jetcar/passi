using AndroidTests.TestClasses;
using AndroidTests.Tools;
using NUnit.Framework;
using System.Threading;

namespace AndroidTests.FunctionalTests
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