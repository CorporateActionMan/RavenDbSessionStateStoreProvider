
namespace Raven.AspNet.SessionState.Utilities
{
    using System.Web;
    using System.Web.SessionState;
    using Interfaces;

    public class CustomSessionStateUtility : ISessionStateUtility
    {

        public HttpStaticObjectsCollection GetSessionStaticObjects(HttpContext context)
        {
            return context != null
                ? SessionStateUtility.GetSessionStaticObjects(context)
                : new HttpStaticObjectsCollection();
        }
    }
}
