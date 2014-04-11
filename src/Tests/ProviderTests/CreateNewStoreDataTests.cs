
namespace Tests.ProviderTests
{
    using System.IO;
    using System.Web;
    using System.Web.Hosting;
    using System.Web.SessionState;
    using Moq;
    using NUnit.Framework;
    using Utilities;

    public class CreateNewStoreDataTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void ExpectedCallToGetSessionStaticObjectsMade()
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            int expectedTimeout = 6767899;
            MockSessionStateUtility.Setup(cmd => cmd.GetSessionStaticObjects(It.IsAny<HttpContext>()))
                .Returns(new HttpStaticObjectsCollection());
            subject.SessionStateUtility = MockSessionStateUtility.Object;

            // Act
            // ReSharper disable once UnusedVariable
            SessionStateStoreData result = subject.CreateNewStoreData(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())),
                expectedTimeout);

            // Assert
            MockSessionStateUtility.Verify(cmd => cmd.GetSessionStaticObjects(It.IsAny<HttpContext>()), Times.Once());

        }

        [Test]
        public void ReturnedSessionStoreDataObjectMeetsSpecification()
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            int expectedTimeout = 6767899;

            // Act
            SessionStateStoreData result = subject.CreateNewStoreData(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())),
                expectedTimeout);

            // Assert
            Assert.IsNotNull(result.Items);
            Assert.AreEqual(0, result.Items.Count);
            Assert.IsNotNull(result.StaticObjects);
            Assert.AreEqual(0, result.StaticObjects.Count);
            Assert.AreEqual(expectedTimeout, result.Timeout);
        }
    }
}
