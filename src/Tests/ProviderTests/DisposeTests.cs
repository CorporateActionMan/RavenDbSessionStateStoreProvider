
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

    public class DisposeTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void DocumentStoreIsDisposedWhenItExists()
        {
            // Arrange
            const string expectedAppName = "Heath Robinson's Atom Splitter";
            const string appPath = "An arbitrary application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            var keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);

            MockDocumentStore.Setup(cmd => cmd.Dispose()).Verifiable();

            subject.Initialize("An arbitrary session store name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.Dispose();

            // Assert
            MockDocumentStore.Verify(cmd => cmd.Dispose(), Times.Once());
        }

        [Test]
        public void DocumentStoreIsNotDisposedWhenItDoesntExist()
        {
            // Arrange
            var subject = new RavenSessionStateStoreProvider();

            MockDocumentStore.Setup(cmd => cmd.Dispose()).Verifiable();

            // Act
            subject.Dispose();

            // Assert
            MockDocumentStore.Verify(cmd => cmd.Dispose(), Times.Never());
        }
    }
}
