using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests
{
    [TestFixture]
    public class ReusableCancellationTokenSourceTests
    {
        private ReusableCancellationTokenSource source;

        [SetUp]
        public void SetUp()
        {
            source = new ReusableCancellationTokenSource();
        }

        [Test]
        public void ReturnsSameToken()
        {
            Assert.That(source.Token, Is.EqualTo(source.Token), "source.Token");
        }

        [Test]
        public void ReturnsNewTokenAfterCancel()
        {
            var before = source.Token;

            source.Cancel();

            Assert.That(before.IsCancellationRequested, Is.True, "Should cancel existing token");
            Assert.That(source.Token.IsCancellationRequested, Is.False, "Should create new token that has not been canceled.");
        }
    }
}