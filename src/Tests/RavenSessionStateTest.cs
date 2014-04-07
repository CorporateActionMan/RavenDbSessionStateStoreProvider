

namespace Tests
{
    using System;
    using System.Collections.Specialized;
    using System.Web.Configuration;
    using Moq;
    using Raven.AspNet.SessionState;
    using Raven.Client;
    using Raven.Json.Linq;

    public abstract class RavenSessionStateTest
    {
        protected Mock<IDocumentSession> MockDocumentSession;
        protected Mock<IDocumentStore> MockDocumentStore;
        protected Mock<ISyncAdvancedSessionOperation> MockSyncAdvancedSessionOperation;
        protected virtual SessionStateDocument PreExistingSessionStateDocument { get { return null; } }
        protected SessionStateDocument PersistedSessionStateDocument { get; set; }

        protected RavenSessionStateTest()
        {
            MockDocumentSession = new Mock<IDocumentSession>();
            MockDocumentStore = new Mock<IDocumentStore>();
            MockSyncAdvancedSessionOperation = new Mock<ISyncAdvancedSessionOperation>();

            MockDocumentSession.SetupGet(cmd => cmd.Advanced).Returns(MockSyncAdvancedSessionOperation.Object);
            MockDocumentStore.Setup(cmd => cmd.OpenSession()).Returns(MockDocumentSession.Object);
            
            Subject = new RavenSessionStateStoreProvider{ApplicationName = ApplicationName, SessionStateConfig = new SessionStateSection
                {
                    Timeout = Timeout
                }};
            Subject.Initialize("", new NameValueCollection(), MockDocumentStore.Object);
            if (PreExistingSessionStateDocument != null)
            {
                PersistedSessionStateDocument = PreExistingSessionStateDocument.ShallowCopy();
            }
            MockDocumentSession.Setup(cmd => cmd.Load<SessionStateDocument>(It.IsAny<string>()))
                    .Returns(PersistedSessionStateDocument);
            RavenJObject ravenJObject = new RavenJObject();
            ravenJObject.Add("Raven-Expiration-Date", null);
            MockSyncAdvancedSessionOperation.Setup(cmd => cmd.GetMetadataFor(It.IsAny<SessionStateDocument>()))
                .Returns(ravenJObject);
            
        }

        protected RavenSessionStateStoreProvider Subject { get; private set; }
        protected string ApplicationName {get { return "DummyApplicationName"; }}

        protected TimeSpan Timeout {get { return TimeSpan.FromMinutes(20); }}
    }
}