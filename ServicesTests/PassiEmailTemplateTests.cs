using NUnit.Framework;
using Services;

namespace ServicesTests;

public class PassiEmailTemplateTests
{
    [Test]
    public void ConfirmationCodeIsIncludedInSubjectPlainTextAndHtml()
    {
        var email = PassiEmailTemplate.CreateConfirmationCode("123456");

        Assert.Multiple(() =>
        {
            Assert.That(email.Subject, Is.EqualTo("Passi confirmation code: 123456"));
            Assert.That(email.PlainText, Does.Contain("confirmation code is: 123456"));
            Assert.That(email.Html, Does.Contain(">123456</p>"));
            Assert.That(email.Html, Does.Contain("<meta name=\"viewport\""));
        });
    }

    [Test]
    public void ConfirmationCodeIsHtmlEncoded()
    {
        var email = PassiEmailTemplate.CreateConfirmationCode("<123456>");

        Assert.Multiple(() =>
        {
            Assert.That(email.Html, Does.Contain("&lt;123456&gt;"));
            Assert.That(email.Html, Does.Not.Contain("><123456></p>"));
        });
    }

    [Test]
    public void ConfirmationCodeCannotInjectSubjectHeaders()
    {
        var email = PassiEmailTemplate.CreateConfirmationCode("123456\r\nBcc: attacker@example.com");

        Assert.That(email.Subject, Does.Not.Contain("\r").And.Not.Contain("\n"));
    }
}
