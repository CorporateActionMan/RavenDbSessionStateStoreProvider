using System;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class ReleaseItemExclusive_when_sessionId_does_not_match : RavenSessionStateTest
    {
        protected const string SessionId = "XVB";
        protected const int LockIdExisting = 4;
        protected DateTime ExpiryExisting = DateTime.UtcNow.AddMilliseconds(6);

        protected SessionStateDocument PersistedSessionStateDocument { get; private set; }

        public ReleaseItemExclusive_when_sessionId_does_not_match()
        {
            PersistedSessionStateDocument = PreExistingSessionStateDocument.ShallowCopy();
            //call ReleaseItemExclusive with a lockId that does not match
            Subject.ApplicationName = ApplicationName;
            Subject.ReleaseItemExclusive(null, "A Different SessionId", LockIdExisting);

            using (var session = MockDocumentStore.Object.OpenSession())
            {
                PersistedSessionStateDocument = session.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(SessionId, ApplicationName));
            }
        }

        protected override SessionStateDocument PreExistingSessionStateDocument
        {
            get
            {
                return new SessionStateDocument(SessionId, ApplicationName)
                {
                    Locked = true,
                    LockId = LockIdExisting,
                    Expiry = ExpiryExisting
                };
            }
        }


        [Fact]
        public void item_remains_locked()
        {
            Assert.True(PersistedSessionStateDocument.Locked);
        }

        [Fact]
        public void lock_id_is_unchanged()
        {
            Assert.Equal(LockIdExisting, PersistedSessionStateDocument.LockId);
        }

        [Fact]
        public void expiry_is_unchanged()
        {
            Assert.Equal(ExpiryExisting, PersistedSessionStateDocument.Expiry);
        }
    }
}