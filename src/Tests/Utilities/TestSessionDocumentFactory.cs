using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests.Utilities
{
    using Raven.AspNet.SessionState;

    public static class TestSessionDocumentFactory
    {
        public static SessionStateDocument CreateSessionStateDocument(string sessionId, string applicationName)
        {
            return new SessionStateDocument(sessionId, applicationName);
        }
    }
}
