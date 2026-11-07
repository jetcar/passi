using NUnit.Framework;
using OpenIDC.Helpers;

namespace OpenIDCTests
{
    public class LogValueTests
    {
        [Test]
        public void EscapesLineBreaksInUntrustedLogFields()
        {
            Assert.That(LogValue.Safe("client\r\nForged entry\nnext"),
                Is.EqualTo("client\\r\\nForged entry\\nnext"));
        }

        [Test]
        public void PreservesOrdinaryValuesAndNull()
        {
            Assert.That(LogValue.Safe("client-id"), Is.EqualTo("client-id"));
            Assert.That(LogValue.Safe(null), Is.Null);
        }
    }
}
