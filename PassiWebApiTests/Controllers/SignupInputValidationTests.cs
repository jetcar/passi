using System;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using passi_webapi.Controllers;
using Services;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Controllers;

public class SignupInputValidationTests
{
    // Null dependencies ensure invalid input is rejected before any repository access.
    private readonly SignUpController _controller = new(null, null, null, null);

    [Test]
    public void NullBodiesAreRejectedBeforeDereference()
    {
        Assert.That(_controller.SignUp(null), Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(_controller.Confirm(null), Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(_controller.Check(null), Is.InstanceOf<BadRequestObjectResult>());
        Assert.ThrowsAsync<BadRequestException>(async () => await _controller.Code(null));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void SignupRejectsMissingEmailAndDevice(string invalid)
    {
        Assert.That(_controller.SignUp(new SignupDto { Email = invalid, DeviceId = "device", UserGuid = Guid.NewGuid() }),
            Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(_controller.SignUp(new SignupDto { Email = "user@example.test", DeviceId = invalid, UserGuid = Guid.NewGuid() }),
            Is.InstanceOf<BadRequestObjectResult>());
        Assert.ThrowsAsync<BadRequestException>(async () => await _controller.Code(new SignupDto { Email = invalid }));
    }

    [Test]
    public void SignupRejectsMissingAccountIdentifier()
    {
        foreach (var id in new Guid?[] { null, Guid.Empty })
            Assert.That(_controller.SignUp(new SignupDto { Email = "user@example.test", DeviceId = "device", UserGuid = id }),
                Is.InstanceOf<BadRequestObjectResult>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void ConfirmRejectsEveryMissingRequiredField(string invalid)
    {
        foreach (var field in new[] { "Email", "Code", "PublicCert", "Guid", "DeviceId" })
        {
            var request = new SignupConfirmationDto
            {
                Email = "user@example.test", Code = "123456", PublicCert = "public-certificate",
                Guid = Guid.NewGuid().ToString(), DeviceId = "device"
            };
            typeof(SignupConfirmationDto).GetProperty(field).SetValue(request, invalid);
            Assert.That(_controller.Confirm(request), Is.InstanceOf<BadRequestObjectResult>(), field);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void CheckRejectsMissingUsernameAndCode(string invalid)
    {
        Assert.That(_controller.Check(new SignupCheckDto { Username = invalid, Code = "123456" }),
            Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(_controller.Check(new SignupCheckDto { Username = "user@example.test", Code = invalid }),
            Is.InstanceOf<BadRequestObjectResult>());
    }
}
