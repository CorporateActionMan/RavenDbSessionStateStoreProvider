
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

    public class ResetItemTimeoutTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void EnableSessionStateSetToReadOnlyThrowsConfigurationException()
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            SetEnableSessionState(PagesEnableSessionState.ReadOnly);

            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));


            // Act
            Action act = () => subject.ResetItemTimeout(context, "A session id");

            // Assert
            act.ShouldThrow<ConfigurationException>("This method should only be called if EnableSessionState is not set to ReadOnly");
        }

        [TestCase(PagesEnableSessionState.False)]
        [TestCase(PagesEnableSessionState.True)]
        [Test]
        public void EnableSessionStateNotSetToReadOnlyDoesNotThrowConfigurationException(PagesEnableSessionState sessionStateMode)
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            SetEnableSessionState(sessionStateMode);

            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));


            // Act
            Action act = () => subject.ResetItemTimeout(context, "A session id");

            // Assert
            act.ShouldNotThrow<ConfigurationException>("This method should only be called if EnableSessionState is not set to ReadOnly");
        }

        [TestCase(PagesEnableSessionState.False)]
        [TestCase(PagesEnableSessionState.True)]
        [Test]
        public void ExpiryTimeoutResetAsExpected(PagesEnableSessionState sessionStateMode)
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            SetEnableSessionState(sessionStateMode);

            string providedSessionId = "A sessionId";

            DateTime expectedNow = DateTime.UtcNow.Date;

            Mock<TimeProviderBase> mockTimeProvider = new Mock<TimeProviderBase>();
            mockTimeProvider.SetupGet(cmd => cmd.UtcNow).Returns(expectedNow);
            TimeProviderBase.Current = mockTimeProvider.Object;

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = expectedNow.AddMinutes(5);
            sessionObject.LockDate = expectedNow.AddDays(-1);
            sessionObject.Locked = true;
            sessionObject.LockId = 23456756;

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.ResetItemTimeout(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId");

            // Assert
            MockDocumentSession.Verify(cmd => cmd.Load<SessionStateDocument>(It.IsAny<string>()), Times.Once());
            sessionObject.Expiry.ShouldBeEquivalentTo(expectedNow.AddMinutes(20), "The expiry is not being amended as expected");
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Once());
        }
    }
}
