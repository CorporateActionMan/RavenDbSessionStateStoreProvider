using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    public class RemoveItemTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void ExpectedRavenCallsMadeIfDocumentExistsAndLockIdMatches()
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

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);
            MockDocumentSession.Setup(cmd => cmd.Delete(sessionObject)).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.RemoveItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, lockId, item);

            // Assert
            MockDocumentSession.Verify(
                cmd =>
                    cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId,
                        expectedAppName)), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.Delete(sessionObject), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Once());
        }

        [Test]
        public void ExpectedRavenCallsMadeIfDocumentDoesNotExistAndLockIdMatches()
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

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns((SessionStateDocument)null);
            MockDocumentSession.Setup(cmd => cmd.Delete(sessionObject)).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.RemoveItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, lockId, item);

            // Assert
            MockDocumentSession.Verify(
                cmd =>
                    cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId,
                        expectedAppName)), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.Delete(sessionObject), Times.Never());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Never());
        }

        [Test]
        public void ExpectedRavenCallsMadeIfDocumentExistsAndLockIdDoesNotMatch()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            object lockId = 1;

            string providedSessionId = "A sessionId";

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            SessionStateStoreData item = RavenSessionStateStoreProvider.Deserialize(null, sessionObject.SessionItems, 10);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);
            MockDocumentSession.Setup(cmd => cmd.Delete(sessionObject)).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.RemoveItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, lockId, item);

            // Assert
            MockDocumentSession.Verify(
                cmd =>
                    cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId,
                        expectedAppName)), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.Delete(sessionObject), Times.Never());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Never());
        }

        [Test]
        public void ExpectedRavenCallsMadeIfDocumentDoesNotExistAndLockIdDoesNotMatch()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            object lockId = 1;

            string providedSessionId = "A sessionId";

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            SessionStateStoreData item = RavenSessionStateStoreProvider.Deserialize(null, sessionObject.SessionItems, 10);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns((SessionStateDocument)null);
            MockDocumentSession.Setup(cmd => cmd.Delete(sessionObject)).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.RemoveItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), providedSessionId, lockId, item);

            // Assert
            MockDocumentSession.Verify(
                cmd =>
                    cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId,
                        expectedAppName)), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.Delete(sessionObject), Times.Never());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Never());
        }
    }
}
