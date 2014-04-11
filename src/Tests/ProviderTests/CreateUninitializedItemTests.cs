
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
    using Raven.AspNet.SessionState.Providers;
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

        [Test]
        public void ExpiryIsExpectedExpiry()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            int providedTimeout = 20;
            DateTime now = DateTime.UtcNow.Date;
            DateTime expectedExpiry = now.AddMinutes(providedTimeout);
            string providedSessionId = "A sessionId";
            Mock<TimeProviderBase> mockTimeProvider = new Mock<TimeProviderBase>();
            mockTimeProvider.SetupGet(cmd => cmd.UtcNow).Returns(now);
            TimeProviderBase.Current = mockTimeProvider.Object;

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            SessionStateStoreData item = RavenSessionStateStoreProvider.Deserialize(null, sessionObject.SessionItems, providedTimeout);

            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>()));
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.CreateUninitializedItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, providedTimeout);

            // Assert
            MockDocumentSession.Verify(cmd => cmd.Store(It.Is<SessionStateDocument>(sd =>
                sd.Expiry.Equals(expectedExpiry)
            )), Times.Once());
        }

        [Test]
        public void ActionFlagsValueIsExpected()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            int providedTimeout = 20;
            string providedSessionId = "A sessionId";

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            SessionStateStoreData item = RavenSessionStateStoreProvider.Deserialize(null, sessionObject.SessionItems, providedTimeout);

            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>()));
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.CreateUninitializedItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, providedTimeout);

            // Assert
            MockDocumentSession.Verify(cmd => cmd.Store(It.Is<SessionStateDocument>(sd =>
                sd.Flags.Equals(SessionStateActions.InitializeItem)
            )));
        }
    }
}
    