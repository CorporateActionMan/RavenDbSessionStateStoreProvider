
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

    public class GetItemTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void GetItemReturnsExpectedSessionStateStoreDataWhenItemHasNotExpired()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            bool locked = false;
            TimeSpan lockAge = new TimeSpan();
            SessionStateActions actions = new SessionStateActions();
            object lockId = null;

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
        public void GetItemReturnsNullWhenItemHasExpired()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            bool locked = false;
            TimeSpan lockAge = new TimeSpan();
            SessionStateActions actions = new SessionStateActions();
            object lockId = null;

            string providedSessionId = "A sessionId";

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddSeconds(-1);

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
        public void GetItemReturnsNullWhenItemIsLocked()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            bool locked = false;
            TimeSpan lockAge = new TimeSpan();
            SessionStateActions actions = new SessionStateActions();
            object lockId = null;

            string providedSessionId = "A sessionId";

            SessionStateDocument sessionObject = TestSessionDocumentFactory.CreateSessionStateDocument(providedSessionId, expectedAppName);
            sessionObject.Expiry = DateTime.UtcNow.AddDays(1);
            sessionObject.Locked = true;

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
    }


    [Serializable]
    public class Car
    {
        private string name;
        private string manufacturer;

        public Car()
        {
        }

        public Car(string name, string manufacturer)
        {
            this.name = name;
            this.manufacturer = manufacturer;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Manufacturer
        {
            get { return manufacturer; }
            set { manufacturer = value; }
        }
    }
}
