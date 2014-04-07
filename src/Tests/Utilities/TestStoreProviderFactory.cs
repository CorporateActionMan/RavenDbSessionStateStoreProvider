using System;

namespace Tests.Utilities
{
    using System.Web.Configuration;
    using Moq;
    using Raven.AspNet.SessionState;
    using Raven.AspNet.SessionState.Interfaces;

    public static class TestStoreProviderFactory
    {
        public static RavenSessionStateStoreProvider SetupStoreProvider(string expectedAppPath, Mock<IHostingProvider> mockHostingProvider)
        {
            RavenSessionStateStoreProvider subject = new RavenSessionStateStoreProvider
            {
                SessionStateConfig = new SessionStateSection
                {
                    Timeout = TimeSpan.FromMinutes(20)
                }
            };

            mockHostingProvider.SetupGet(cmd => cmd.ApplicationVirtualPath).Returns(expectedAppPath);
            subject.HostingProvider = mockHostingProvider.Object;
            return subject;
        }
    }
}
