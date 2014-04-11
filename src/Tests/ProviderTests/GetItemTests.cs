
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

    public class GetItemTests : RavenSessionStoreTestsBase
    {
        [Test]
        public virtual void EnableSessionStateIsSetToReadOnlyDoesNotThrowConfigurationException()
        {
            // Arrange
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            RavenSessionStoreTestsBase.SetEnableSessionState(PagesEnableSessionState.ReadOnly);

            bool locked;
            TimeSpan lockAge;
            SessionStateActions actions;
            object lockId;
            HttpContext context = new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter()));

            // Act
            Action act = () => subject.GetItem(context, "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            act.ShouldNotThrow<ConfigurationException>("This method should only be called if EnableSessionState is set to ReadOnly");
        }

        [TestCase(PagesEnableSessionState.False)]
        [TestCase(PagesEnableSessionState.True)]
        [Test]
        public virtual void EnableSessionStateIsNotSetToReadOnlyThrowsConfigurationException(PagesEnableSessionState enableSessionStateMode)
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
            Action act = () => subject.GetItem(context, "A sessionId", out locked, out lockAge, out lockId, out actions);

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

            RavenSessionStoreTestsBase.SetEnableSessionState(PagesEnableSessionState.ReadOnly);

            // Act
            var result =
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            Assert.IsNull(result);
            locked.Should().BeFalse();
        }

        [Test]
        public void ReturnsExpectedSessionStateStoreDataWhenItemHasNotExpired()
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

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems  = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            var result = 
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId , out actions );

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
            Assert.IsInstanceOf<Car>(result.Items[0]);
            Assert.AreEqual("A6", ((Car)result.Items[0]).Name);
            Assert.AreEqual("Audi", ((Car)result.Items[0]).Manufacturer);
        }

        [Test]
        public void ReturnsNullWhenItemHasExpired()
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
            sessionObject.Expiry = expectedNow.AddSeconds(-1);

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);



            // Act
            var result =
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ReturnsExpectedSpecificationWhenItemIsLocked()
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
            sessionObject.Expiry = expectedNow.AddMinutes(20);
            sessionObject.LockDate = expectedNow.AddDays(-1);
            sessionObject.Locked = true;
            sessionObject.LockId = 23456756;
            
            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            var result =
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            result.Should().BeNull();
            locked.Should().BeTrue();
            lockAge.ShouldBeEquivalentTo(new TimeSpan(1,0,0,0));
            lockId.ShouldBeEquivalentTo(sessionObject.LockId);
        }

        [Test]
        public void Cookieless_ItemFoundWithInitializeItemActionFlagValueOutputsValueAndSetsDataStoreValueToZero()
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
            sessionObject.Expiry = expectedNow.AddMinutes(20);
            sessionObject.LockDate = expectedNow.AddDays(-1);
            sessionObject.Locked = false;
            sessionObject.LockId = 23456756;
            sessionObject.Flags = SessionStateActions.InitializeItem;

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);
            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>())).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            RavenSessionStoreTestsBase.SetEnableSessionState(PagesEnableSessionState.ReadOnly);

            ActivateCookielessModeAndRegenerateExpiredSessionId();

            // Act
            var result =
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            actions.ShouldBeEquivalentTo(SessionStateActions.InitializeItem);
            MockDocumentSession.Verify(cmd => cmd.Store(It.Is<SessionStateDocument>(sd => sd.Flags == 0)), Times.Once());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Once());
        }

        [Test]
        public void WithCookies_ItemFoundWithInitializeItemActionFlagValueShouldNotMakeCalls()
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
            sessionObject.Expiry = expectedNow.AddMinutes(20);
            sessionObject.LockDate = expectedNow.AddDays(-1);
            sessionObject.Locked = false;
            sessionObject.LockId = 23456756;
            sessionObject.Flags = SessionStateActions.InitializeItem;

            var sessionItems = new SessionStateItemCollection();

            sessionItems["ACar"] = new Car("A6", "Audi");

            sessionObject.SessionItems = subject.Serialize(sessionItems);

            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(providedSessionId, expectedAppName))).Returns(sessionObject);
            MockDocumentSession.Setup(cmd => cmd.Store(It.IsAny<SessionStateDocument>())).Verifiable();
            MockDocumentSession.Setup(cmd => cmd.SaveChanges()).Verifiable();

            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            DeactivateCookielessModeAndRegenerateExpiredSessionId();

            // Act
            var result =
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            actions.ShouldBeEquivalentTo(SessionStateActions.None);
            MockDocumentSession.Verify(cmd => cmd.Store(It.Is<SessionStateDocument>(sd => sd.Flags == 0)), Times.Never());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Never());
        }

        [Test]
        public void ItemFoundWithNoActionFlagValueOutputsValueAndSetsDataStoreValueToZero()
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
            sessionObject.Expiry = expectedNow.AddMinutes(20);
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
                    subject.GetItem(new HttpContext(new SimpleWorkerRequest("", "", "", "", new StringWriter())), "A sessionId", out locked, out lockAge, out lockId, out actions);

            // Assert
            actions.ShouldBeEquivalentTo(sessionObject.Flags);
            MockDocumentSession.Verify(cmd => cmd.Store(It.Is<SessionStateDocument>(sd => sd.Flags == 0)), Times.Never());
            MockDocumentSession.Verify(cmd => cmd.SaveChanges(), Times.Never());
        }

        private static void ActivateCookielessModeAndRegenerateExpiredSessionId()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            SessionStateSection sessionStateSection = config.GetSection("system.web/sessionState") as SessionStateSection;
            if (sessionStateSection != null)
            {
                sessionStateSection.Cookieless = HttpCookieMode.UseUri;
                sessionStateSection.RegenerateExpiredSessionId = true;
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("system.web/sessionState");
        }

        private static void DeactivateCookielessModeAndRegenerateExpiredSessionId()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            SessionStateSection sessionStateSection = config.GetSection("system.web/sessionState") as SessionStateSection;
            if (sessionStateSection != null)
            {
                sessionStateSection.Cookieless = HttpCookieMode.UseCookies;
                sessionStateSection.RegenerateExpiredSessionId = false;
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("system.web/sessionState");
        }
    }


    [Serializable]
    public class Car
    {
        private string _name;
        private string _manufacturer;

        public Car()
        {
        }

        public Car(string name, string manufacturer)
        {
            _name = name;
            _manufacturer = manufacturer;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Manufacturer
        {
            get { return _manufacturer; }
            set { _manufacturer = value; }
        }
    }
}
