using System;

namespace Tests.ProviderTests
{
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
            var keyPairs = new NameValueCollection();
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

        [Test]
        public virtual void EnableSessionStateIsSetToTrueDoesNotThrowConfigurationException()
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            SetEnableSessionState(PagesEnableSessionState.True);

            SessionStateStoreData sessionData = new SessionStateStoreData(
                                                                    new SessionStateItemCollection(),
                                                                    new HttpStaticObjectsCollection(),
                                                                    20
                                                                    );
            object lockId = null;
            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));

            // Act
            Action act = () => subject.SetAndReleaseItemExclusive(context, "A sessionId", sessionData, lockId, true);

            // Assert
            act.ShouldNotThrow<ConfigurationException>("This method should only be called if EnableSessionState is set to True");
        }

        [TestCase(PagesEnableSessionState.False)]
        [TestCase(PagesEnableSessionState.ReadOnly)]
        [Test]
        public virtual void EnableSessionStateIsNotSetToTrueThrowsConfigurationException(PagesEnableSessionState enableSessionStateMode)
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            SetEnableSessionState(enableSessionStateMode);

            SessionStateStoreData sessionData = new SessionStateStoreData(
                                                                    new SessionStateItemCollection(),
                                                                    new HttpStaticObjectsCollection(),
                                                                    20
                                                                    );
            object lockId = null;
            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));


            // Act
            Action act = () => subject.SetAndReleaseItemExclusive(context, "A sessionId", sessionData, lockId, true);

            // Assert
            act.ShouldThrow<ConfigurationException>("This method should only be called if EnableSessionState is not set to True");
        }
    }
}
