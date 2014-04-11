
namespace Raven.AspNet.SessionState.Interfaces
{
    using System.Web;

    public interface ISessionStateUtility
    {
        HttpStaticObjectsCollection GetSessionStaticObjects(HttpContext context);
    }
}
