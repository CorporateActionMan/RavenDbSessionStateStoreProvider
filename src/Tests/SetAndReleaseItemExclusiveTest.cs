using System.Web;
using System.Web.SessionState;
using Raven.AspNet.SessionState;

namespace Tests
{
    using Moq;
    using Raven.Json.Linq;

    public abstract class SetAndReleaseItemExclusiveTest : RavenSessionStateTest
    {
        protected SetAndReleaseItemExclusiveTest()
        {
            Subject.ApplicationName = ApplicationName;
            Subject.SetAndReleaseItemExclusive(null, SessionId, new SessionStateStoreData(Items, new HttpStaticObjectsCollection(), (int)Timeout.TotalMinutes ), LockId, NewItem);
        }

        protected SetAndReleaseItemExclusiveTest(string applicationName)
        {
            Subject.ApplicationName = applicationName;
            Subject.SetAndReleaseItemExclusive(null, SessionId, new SessionStateStoreData(Items, new HttpStaticObjectsCollection(), (int)Timeout.TotalMinutes), LockId, NewItem);
        }

        protected abstract string SessionId { get; }
        protected abstract int LockId { get; }
        protected abstract bool NewItem { get; }
        protected abstract SessionStateItemCollection Items { get; }
    }
}