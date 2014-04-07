
namespace Tests.ProviderTests
{
    using System;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Raven.Client;
    using Raven.Client.Document;

    public class ConstructorTests : RavenSessionStoreTestsBase
    {
        [Test]
        public void NullDocumentStoreThrowsArgumentNullException()
        {
            // Arrange
            IDocumentStore nullStore = null;
            TestDelegate act = () => new RavenSessionStateStoreProvider(nullStore);

            // Assert
            Assert.Throws<ArgumentNullException>(act, "document store cannot be null");
        }

        [Test]
        public void ValidDocumentStoreDoesNotThrowArgumentNullException()
        {
            // Arrange
            IDocumentStore nonNullStore = new DocumentStore();
            TestDelegate act = () => new RavenSessionStateStoreProvider(nonNullStore);

            // Assert
            Assert.DoesNotThrow(act, "valid document store does not throw an exception");
        }
    }
}
