
namespace Raven.AspNet.SessionState.Providers
{
    using System.Web.Hosting;
    using Interfaces;

    public class CustomHostingProvider : IHostingProvider
    {
        public string ApplicationVirtualPath
        {
            get { return HostingEnvironment.ApplicationVirtualPath; }
        }
    }
}
