
namespace Tests.ProviderTests
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using NUnit.Framework;
    using Raven.Client;
    using Utilities;

    public class InitializeTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void DocumentStoreNotProvided_NullConnectionStringThrowsConfigurationErrorsException()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            // Act
            TestDelegate act = () => subject.Initialize("", keyPairs, null);

            // Assert
            Assert.Throws<ConfigurationErrorsException>(act, "null connection string should throw configuration errors");

        }

        [Test]
        public void DocumentStoreNotProvided_ValidConnectionStringDoesNotThrowConfigurationErrorsException()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            string connectionStringName = "AConnectionString";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            keyPairs.Set("connectionStringName", connectionStringName);

            // Act
            TestDelegate act = () => subject.Initialize("", keyPairs, null);

            // Assert
            Assert.DoesNotThrow(act, "null connection string should throw configuration errors");
        }

        [Test]
        public void DocumentStoreNotProvided_ValidConnectionStringEnsuresDocumentStoreCreated()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            string connectionStringName = "AConnectionString";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            keyPairs.Set("connectionStringName", connectionStringName);

            IDocumentStore docStore = null;

            // Act
            subject.Initialize("", keyPairs, docStore);

            // Assert
            Assert.IsNotNull(subject.DocumentStore);
        }

        [Test]
        public void ConfigNotProvidedThrowsArgumentNullException()
        {
            // Arrange
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            string connectionStringName = "AConnectionString";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);
            keyPairs.Set("connectionStringName", connectionStringName);

            IDocumentStore docStore = null;

            // Act
            TestDelegate act = () => subject.Initialize("", null, docStore);

            // Assert
            Assert.Throws<ArgumentNullException>(act, "Config cannot be null");
        }

    }
}
