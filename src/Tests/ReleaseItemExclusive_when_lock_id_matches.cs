using System;
using System.Collections.Generic;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    using Moq;
    using Raven.Json.Linq;

    public class ReleaseItemExclusive_when_lock_id_matches : RavenSessionStateTest
    {
        protected const string SessionId = "XVB";
        protected const int LockIdExisting = 4;
        protected DateTime ExpiryExisting = DateTime.UtcNow;

        public ReleaseItemExclusive_when_lock_id_matches()
        {
            RavenJObject ravenJObject = new RavenJObject();
            ravenJObject.Add("Raven-Expiration-Date", null);
            MockSyncAdvancedSessionOperation.Setup(cmd => cmd.GetMetadataFor(It.IsAny<SessionStateDocument>()))
                .Returns(ravenJObject);
            //call ReleaseItemExclusive with matching lockId  
           Subject.ReleaseItemExclusive(null, SessionId, LockIdExisting);
           ExpiryExisting = DateTime.UtcNow;
        }

        protected override SessionStateDocument PreExistingSessionStateDocument
        {
            get
            {
                return
                    new SessionStateDocument(SessionId, ApplicationName)
                    {
                        Locked = true,
                        LockId = LockIdExisting,
                        Expiry = ExpiryExisting
                    };
            }
        }

        [Fact]
        public void lock_is_removed()
        {
           Assert.False(PersistedSessionStateDocument.Locked); 
        }

        [Fact]
        public void expiry_is_extended()
        {
            var newExpiry = PersistedSessionStateDocument.Expiry;
            var expectedExpiry = ExpiryExisting.Add(Timeout);
            Assert.True(expectedExpiry >= newExpiry);
        }
    }
}