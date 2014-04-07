
namespace Tests.ProviderTests
{
    using Moq;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Raven.AspNet.SessionState.Interfaces;
    using Raven.Client;
    using Raven.Json.Linq;

    [TestFixture]
    public abstract class RavenSessionStoreTestsBase
    {
        protected Mock<IDocumentStore> MockDocumentStore;
        protected Mock<IHostingProvider> MockHostingProvider;
        protected Mock<IDocumentSession> MockDocumentSession;
        protected Mock<ISyncAdvancedSessionOperation> MockAdvancedSessionOperation;

        [SetUp]
        public void Initialize()
        {
            MockDocumentStore = new Mock<IDocumentStore>();
            MockHostingProvider = new Mock<IHostingProvider>();
            MockDocumentSession = new Mock<IDocumentSession>();
            MockAdvancedSessionOperation = new Mock<ISyncAdvancedSessionOperation>();

            MockDocumentStore.Setup(cmd => cmd.OpenSession()).Returns(MockDocumentSession.Object);
            MockDocumentSession.SetupGet(cmd => cmd.Advanced).Returns(MockAdvancedSessionOperation.Object);

            RavenJObject ravenJObject = new RavenJObject();
            ravenJObject.Add("Raven-Expiration-Date", null);
            MockAdvancedSessionOperation.Setup(cmd => cmd.GetMetadataFor(It.IsAny<SessionStateDocument>()))
                .Returns(ravenJObject);
        }
    }
}
