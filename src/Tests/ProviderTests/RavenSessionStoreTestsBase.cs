
namespace Tests.ProviderTests
{
    using System.Configuration;
    using System.Web.Configuration;
    using Moq;
    using NUnit.Framework;
    using Raven.AspNet.SessionState;
    using Raven.AspNet.SessionState.Interfaces;
    using Raven.Client;
    using Raven.Json.Linq;
    using Utilities;

    [TestFixture]
    public abstract class RavenSessionStoreTestsBase
    {
        protected Mock<IDocumentStore> MockDocumentStore;
        protected Mock<IHostingProvider> MockHostingProvider;
        protected Mock<ISessionStateUtility> MockSessionStateUtility;
        protected Mock<IDocumentSession> MockDocumentSession;
        protected Mock<ISyncAdvancedSessionOperation> MockAdvancedSessionOperation;

        [SetUp]
        public void Initialize()
        {
            MockDocumentStore = new Mock<IDocumentStore>();
            MockHostingProvider = new Mock<IHostingProvider>();
            MockSessionStateUtility = new Mock<ISessionStateUtility>();
            MockDocumentSession = new Mock<IDocumentSession>();
            MockAdvancedSessionOperation = new Mock<ISyncAdvancedSessionOperation>();

            MockDocumentStore.Setup(cmd => cmd.OpenSession()).Returns(MockDocumentSession.Object);
            MockDocumentSession.SetupGet(cmd => cmd.Advanced).Returns(MockAdvancedSessionOperation.Object);

            RavenJObject ravenJObject = new RavenJObject();
            ravenJObject.Add("Raven-Expiration-Date", null);
            MockAdvancedSessionOperation.Setup(cmd => cmd.GetMetadataFor(It.IsAny<SessionStateDocument>()))
                .Returns(ravenJObject);
        }

        protected static void SetEnableSessionState(PagesEnableSessionState enableSessionStateMode)
        {
            ConfigurationService.SetEnableSessionState(enableSessionStateMode);
        }
    }
}
