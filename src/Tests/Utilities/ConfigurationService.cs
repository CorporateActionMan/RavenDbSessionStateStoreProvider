namespace Tests.Utilities
{
    using System.Configuration;
    using System.Web.Configuration;

    public class ConfigurationService
    {
        public static void SetEnableSessionState(PagesEnableSessionState enableSessionStateMode)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            PagesSection pagesSection = config.GetSection("system.web/pages") as PagesSection;
            if (pagesSection != null) pagesSection.EnableSessionState = enableSessionStateMode;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("system.web/pages");
        }
    }
}