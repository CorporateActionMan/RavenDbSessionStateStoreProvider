using System;
using System.Web.SessionState;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{

    public class SetAndReleaseItemExclusive_when_application_does_not_match : SetAndReleaseItemExclusiveTest
    {
        protected override string SessionId
        {
            get { return "BLAHBLAHBLAH"; }
        }

        protected override int LockId
        {
            get { return 4; }
        }

        protected override bool NewItem
        {
            get { return false; }
        }

        protected override SessionStateItemCollection Items
        {
            get
            {
                var items = new SessionStateItemCollection();
                items["Name"] = "Papa Smurf";
                return items;
            }
        }

        public SetAndReleaseItemExclusive_when_application_does_not_match()
            :base("FooBar!")
        {
        }

        protected override SessionStateDocument PreExistingSessionStateDocument
        {
            get
            {
                return new SessionStateDocument(SessionId, ApplicationName)
                {
                    Locked = true,
                    LockId = 4,
                    SessionItems = Subject.Serialize(Items),
                    Expiry = DateTime.UtcNow.AddMinutes(10)
                };
            }
        }

        [Fact]
        public void item_remains_locked()
        {
            Assert.True(PersistedSessionStateDocument.Locked);
        }

        [Fact]
        public void lock_id_is_not_modified()
        {
            Assert.Equal(PreExistingSessionStateDocument.LockId, PersistedSessionStateDocument.LockId);
        }

        [Fact]
        public void data_is_not_modified()
        {
            Assert.Equal(PreExistingSessionStateDocument.SessionItems, PersistedSessionStateDocument.SessionItems);
        }

    }
}