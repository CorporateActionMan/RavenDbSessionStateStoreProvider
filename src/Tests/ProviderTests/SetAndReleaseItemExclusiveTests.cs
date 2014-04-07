using System;

namespace Tests.ProviderTests
{
    using System.Collections.Specialized;
    using System.IO;
    using System.Web;
    using System.Web.Hosting;
    using System.Web.SessionState;
    using Moq;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Utilities;

    public class SetAndReleaseItemExclusiveTests : RavenSessionStoreTestsBase
    {

        [Test]
        public void NullSessionStoreDataItemThrowsArgumentNullException()
        {
            // Arrange & Act
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            TestDelegate act =
                () =>
                    subject.SetAndReleaseItemExclusive(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", null, new object(), true);

            // Assert
            Assert.Throws<ArgumentNullException>(act, "SessionStateStoreData item cannot be null");

        }

        [Test]
        public void NewItemCallsSessionStore()
        {
            // Arrange & Act
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            subject.Initialize("", keyPairs, MockDocumentStore.Object);

            SessionStateStoreData sessionData = new SessionStateStoreData(
                new SessionStateItemCollection(),
                new HttpStaticObjectsCollection(),
                20
                );

            subject.SetAndReleaseItemExclusive(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", sessionData, new object(), true);

            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>())).Verifiable();

            // Assert
            MockDocumentSession.Verify(cmd => cmd.Store(It.IsAny<SessionStateDocument>()), Times.Once());
        }
    }
}
