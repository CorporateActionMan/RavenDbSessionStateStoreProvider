
namespace Tests.ProviderTests
{
    using System;
    using System.Collections.Specialized;
    using System.Web.Configuration;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Utilities;

    public class ApplicationNameTests : RavenSessionStoreTestsBase
    {

        [Test]
        public void WhenAttributeIsNullNameSetToHostingEnvironmentApplicationPath()
        {
            // Arrange
            string expectedAppPath = "Any ole iron, any ole iron";
            var subject = TestStoreProviderFactory.SetupStoreProvider(expectedAppPath, MockHostingProvider);

            // Act
            subject.Initialize("", new NameValueCollection(), MockDocumentStore.Object);

            // Assert
            Assert.AreEqual(expectedAppPath, subject.ApplicationName);
        }

        [Test]
        public void WhenAttributeIsEmptyNameSetToHostingEnvironmentApplicationPath()
        {
            // Arrange
            string expectedAppPath = "ABC its easy as 123";
            var subject = TestStoreProviderFactory.SetupStoreProvider(expectedAppPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", string.Empty);

            // Act
            subject.Initialize("", keyPairs, MockDocumentStore.Object);

            // Assert
            Assert.AreEqual(expectedAppPath, subject.ApplicationName);
        }

        [Test]
        public void WhenAttributeIsWhitespaceNameSetToHostingEnvironmentApplicationPath()
        {
            // Arrange
            string expectedAppPath = "Candy Girl you are my world";
            var subject = TestStoreProviderFactory.SetupStoreProvider(expectedAppPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", "   ");

            // Act
            subject.Initialize("", keyPairs, MockDocumentStore.Object);

            // Assert
            Assert.AreEqual(expectedAppPath, subject.ApplicationName);
        }

        [Test]
        public void WhenAttributeIsProvidedNameSetToProvidedAttribute()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            // Act
            subject.Initialize("", keyPairs, MockDocumentStore.Object);

            // Assert
            Assert.AreEqual(expectedAppName, subject.ApplicationName);
        }
    }
}
