
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
            string expectedAppName = "You are everything ... to me";
            string appPath = "Application path";
            var subject = TestStoreProviderFactory.SetupStoreProvider(appPath, MockHostingProvider);
            NameValueCollection keyPairs = new NameValueCollection();
            keyPairs.Set("applicationName", expectedAppName);


            subject.Initialize("A name", keyPairs, MockDocumentStore.Object);

            // Act
            subject.Dispose();

            // Assert
            MockDocumentStore.Verify(cmd => cmd.Dispose(), Times.Once());
        }
    }
}
