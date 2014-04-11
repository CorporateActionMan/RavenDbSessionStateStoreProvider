
namespace Tests.ProviderTests
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.Hosting;
    using System.Web.SessionState;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Raven.AspNet.SessionState.Providers;
    using Utilities;

    public class GetItemExclusiveTests : RavenSessionStoreTestsBase
    {
        [Test]
        public virtual void EnableSessionStateIsSetToTrueDoesNotThrowConfigurationException()
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            RavenSessionStoreTestsBase.SetEnableSessionState(PagesEnableSessionState.True);

            bool locked;
            TimeSpan lockAge;
            SessionStateActions actions;
            object lockId;
            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));

            // Act
            Action act = () => subject.GetItemExclusive(context, "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            act.ShouldNotThrow<ConfigurationException>("This method should only be called if EnableSessionState is set to ReadOnly");
        }

        [TestCase(PagesEnableSessionState.False)]
        [TestCase(PagesEnableSessionState.ReadOnly)]
        [Test]
        public virtual void EnableSessionStateIsNotSetToTrueThrowsConfigurationException(PagesEnableSessionState enableSessionStateMode)
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            RavenSessionStoreTestsBase.SetEnableSessionState(enableSessionStateMode);

            bool locked;
            TimeSpan lockAge;
            SessionStateActions actions;
            object lockId;
            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));


            // Act
            Action act = () => subject.GetItemExclusive(context, "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            act.ShouldThrow<ConfigurationException>("This method should only be called if EnableSessionState is set to ReadOnly");
        }

        [Test]
        public void NoItemFoundInDataStoreSetsOutLockedToFalseAndReturnsNull()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            bool locked;
            TimeSpan lockAge;
            SessionStateActions actions;
            object lockId;

            string providedSessionId = "A sessionId";

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns((SessionStateDocument)null);

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            RavenSessionStoreTestsBase.SetEnableSessionState(PagesEnableSessionState.True);

            // Act
            var result =
                    subject.GetItemExclusive(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            Assert.IsNull(result);
            locked.Should().BeFalse();
        }

        [Test]
        public void NonLockedItemFoundVerifyExpirationDateSet()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            bool locked;
            TimeSpan lockAge;
            SessionStateActions actions;
            object lockId;

            string providedSessionId = "A sessionId";

            DateTime expectedNow = DateTime.UtcNow.Date;

            Mock<TimeProviderBase> mockTimeProvider = new Mock<TimeProviderBase>();
            mockTimeProvider.SetupGet(cmd => cmd.UtcNow).Returns(expectedNow);
            TimeProviderBase.Current = mockTimeProvider.Object;

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = expectedNow.AddMinutes(5);
            sessionObject.LockDate = expectedNow.AddDays(-1);
            sessionObject.Locked = false;
            sessionObject.LockId = 23456756;
            sessionObject.Flags = SessionStateActions.None;

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);
            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>())).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            var result =
                    subject.GetItemExclusive(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Exactly(2));
            sessionObject.Expiry.ShouldBeEquivalentTo(expectedNow.AddMinutes(20));
        }
    }
}
