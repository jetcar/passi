using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using OpenIDC.Helpers;

namespace OpenIDCTests
{
    public class PkceHelperTests
    {
        private static string ComputeS256Challenge(string verifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            var base64 = System.Convert.ToBase64String(hash);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        [Test]
        public void ValidateCodeChallengeReturnsTrueForMatchingS256Challenge()
        {
            var verifier = "this-is-a-valid-code-verifier-1234567890";
            var challenge = ComputeS256Challenge(verifier);

            var result = PkceHelper.ValidateCodeChallenge(verifier, challenge, "S256");

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateCodeChallengeReturnsFalseForMismatchedS256Challenge()
        {
            var verifier = "this-is-a-valid-code-verifier-1234567890";
            var wrongChallenge = ComputeS256Challenge("some-other-verifier");

            var result = PkceHelper.ValidateCodeChallenge(verifier, wrongChallenge, "S256");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCodeChallengeReturnsFalseWhenS256ChallengeLengthDiffers()
        {
            var verifier = "this-is-a-valid-code-verifier-1234567890";
            var challenge = ComputeS256Challenge(verifier);

            var result = PkceHelper.ValidateCodeChallenge(verifier, challenge.Substring(0, challenge.Length - 1), "S256");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCodeChallengeReturnsTrueForMatchingPlainChallenge()
        {
            var result = PkceHelper.ValidateCodeChallenge("plain-verifier", "plain-verifier", "plain");

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateCodeChallengeReturnsFalseForMismatchedPlainChallenge()
        {
            var result = PkceHelper.ValidateCodeChallenge("plain-verifier", "different-value", "plain");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCodeChallengeReturnsFalseForUnknownMethod()
        {
            var result = PkceHelper.ValidateCodeChallenge("verifier", "verifier", "unknown-method");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCodeChallengeReturnsFalseWhenVerifierIsEmpty()
        {
            var result = PkceHelper.ValidateCodeChallenge("", "challenge", "plain");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCodeChallengeReturnsFalseWhenChallengeIsEmpty()
        {
            var result = PkceHelper.ValidateCodeChallenge("verifier", "", "plain");

            Assert.That(result, Is.False);
        }
    }
}
