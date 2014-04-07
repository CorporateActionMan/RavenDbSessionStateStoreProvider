
namespace Tests.ProviderTests
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Web;
    using System.Web.Hosting;
    using System.Web.SessionState;
    using Moq;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Utilities;

    public class CreateUninitializedItemTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void ExpectedRavenDbCallsAreMade()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            object lockId = 0;

            string providedSessionId = "A sessionId";

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            SessionStateStoreData item = RavenSessionStateStoreProvider.Deserialize(null, sessionObject.SessionItems, 10);

            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>())).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.CreateUninitializedItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, 10);

            // Assert
            MockDocumentSession.Verify(cmd => cmd.Store(It.IsAny<SessionStateDocument>()), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Once());
        }
    }
}
