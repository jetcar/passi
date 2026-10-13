using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NUnit.Framework;
using WebApiDto.SignUp;

namespace PassiWebApiTests.Dto
{
    public class SignupDtoValidationTests
    {
        // [ApiController] rejects a request with 400 before the action runs whenever DataAnnotations
        // validation fails on the body. SignupDto.Email carried only [EmailAddress], which (like most
        // DataAnnotations attributes other than [Required]) treats null as valid, so a request with no
        // Email passed model validation and reached SignUpController.SignUp/Code, where
        // signupDto.Email.Trim() threw a NullReferenceException that ErrorHandlerMiddleware turns into
        // a 500 instead of a clean 400 - unlike every sibling DTO (SignupConfirmationDto.Email,
        // DeleteUserDto.Email, SignupCheckDto.Username), which are all [Required].
        [Test]
        public void MissingEmailFailsValidation()
        {
            var dto = new SignupDto { DeviceId = "device-1", UserGuid = Guid.NewGuid() };
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

            Assert.That(isValid, Is.False);
            Assert.That(results.Any(r => r.MemberNames.Contains(nameof(SignupDto.Email))), Is.True);
        }
    }
}
