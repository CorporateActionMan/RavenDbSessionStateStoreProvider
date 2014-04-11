using System;
using System.Web.SessionState;

namespace Tests
{
    using System.Web.Configuration;
    using Utilities;

    public abstract class GetItemExclusiveTest : RavenSessionStateTest
    {
        protected GetItemExclusiveTest()
        {
            ConfigurationService.SetEnableSessionState(PagesEnableSessionState.True);
            bool locked;
            TimeSpan lockAge;
            object lockId;
            SessionStateActions actions;
            Result = Subject.GetItemExclusive(null, SessionId, out locked, out lockAge, out lockId, out actions);
            Locked = locked;
            LockId = lockId;
            LockAge = lockAge;
        }

        protected abstract string SessionId { get; }


        
        protected SessionStateStoreData Result { get; set; }
        protected bool Locked { get; set; }
        protected object LockId { get; set; }
        protected TimeSpan LockAge { get; set; }
    }
}