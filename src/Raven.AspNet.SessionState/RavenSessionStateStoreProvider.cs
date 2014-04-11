

namespace Raven.AspNet.SessionState
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.SessionState;
    using Interfaces;
    using NLog;
    using Providers;
    using Abstractions.Exceptions;
    using Infrastructure;
    using Client;
    using Client.Document;
    using Json.Linq;
    using Utilities;

    /// <summary>
    /// An ASP.NET session-state store-provider implementation (http://msdn.microsoft.com/en-us/library/ms178588.aspx) using 
    /// RavenDb (http://ravendb.net) for persistence.
    /// </summary>
    public class RavenSessionStateStoreProvider : SessionStateStoreProviderBase, IDisposable
    {
        private NameValueCollection _configuration;
        private IDocumentStore _documentStore;
        private IHostingProvider _hostingProvider;
        private ISessionStateUtility _sessionStateUtility;
        private SessionStateSection _sessionStateConfig;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Public parameterless constructor
        /// </summary>
        public RavenSessionStateStoreProvider()
        {
            _hostingProvider = new CustomHostingProvider();
            SessionStateUtility = new CustomSessionStateUtility();
        }

        /// <summary>
        /// Constructor accepting a document store instance, used for testing.
        /// </summary>
        public RavenSessionStateStoreProvider(IDocumentStore documentStore)
            : this()
        {
            CheckValidDocumentStoreParameter(documentStore);
            _documentStore = documentStore;
        }

        private void CheckValidDocumentStoreParameter(IDocumentStore docStore)
        {
            if (docStore == null)
            {
                throw new ArgumentNullException("docStore", "docStore cannot be null");
            }
        }

        public IDocumentStore DocumentStore
        {
            get { return _documentStore; }
        }

        /// <summary>
        /// The name of the application.
        /// Session-data items will be stored against this name.
        /// If not set, defaults to System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath
        /// </summary>
        public string ApplicationName { get; set; }

        internal SessionStateSection SessionStateConfig
        {
            get { return _sessionStateConfig ?? (_sessionStateConfig = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState")); }
            set { _sessionStateConfig = value; }
        }

        public IHostingProvider HostingProvider
        {
            get { return _hostingProvider; }
            set { _hostingProvider = value; }
        }

        public ISessionStateUtility SessionStateUtility
        {
            get { return _sessionStateUtility; }
            set { _sessionStateUtility = value; }
        }

        private static void CheckConfigIsValid(NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");
        }

        private static string GetSessionStoreName(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "RavenSessionStateStore";
            return name;
        }

        private void SetApplicationName()
        {
            if (string.IsNullOrWhiteSpace(_configuration["applicationName"]))
            {
                ApplicationName = HostingProvider.ApplicationVirtualPath;
            }
            else
            {
                ApplicationName = _configuration["applicationName"];
            }
        }

        private void CheckConnectionStringIsValid()
        {
            if (string.IsNullOrEmpty(_configuration["connectionStringName"]))
                throw new ConfigurationErrorsException("Must supply a connectionStringName.");
        }

        private void CreateAndInitializeDocumentStore()
        {
            _documentStore = new DocumentStore
            {
                ConnectionStringName = _configuration["connectionStringName"],
                Conventions = { JsonContractResolver = new PrivatePropertySetterResolver() },
            };

            DocumentStore.Initialize();
        }

        private static void LogInitializationError(Exception ex)
        {
            Logger.ErrorException("Error while initializing.", ex);
        }

        /// <summary>
        ///  see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.createnewstoredata
        /// </summary>
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(),
                                             _sessionStateUtility.GetSessionStaticObjects(context),
                                             timeout);
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.createuninitializeditem
        /// </summary>
        public override void CreateUninitializedItem(HttpContext context, string sessionId, int timeout)
        {
            try
            {
                Logger.Debug("Beginning CreateUninitializedItem. SessionId={0}; Application={1}; timeout={1}.", sessionId, ApplicationName, timeout);

                using (var documentSession = DocumentStore.OpenSession())
                {
                    var expiry = TimeProviderBase.Current.UtcNow.AddMinutes(timeout);

                    var sessionStateDocument = new SessionStateDocument(sessionId, ApplicationName)
                    {
                        Expiry = expiry,
                        Flags = SessionStateActions.InitializeItem
                    };

                    documentSession.Store(sessionStateDocument);
                    documentSession.Advanced.GetMetadataFor(sessionStateDocument)["Raven-Expiration-Date"] =
                        new RavenJValue(expiry);

                    documentSession.SaveChanges();
                }

                Logger.Debug("Completed CreateUninitializedItem. Sessionid={0}; Application={1}; timeout={1}.", sessionId, ApplicationName, timeout);

            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error during CreateUninitializedItem.", ex);
                throw;
            }
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.dispose
        /// </summary>
        public override void Dispose()
        {
            try
            {
                if (DocumentStore != null)
                    DocumentStore.Dispose();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("An exception was thrown while disposing the DocumentStore: ", ex);
                //swallow the exception...nothing good can come from throwing it here!
            }
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.endrequest
        /// Performs any cleanup required by your session-state store provider.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        public override void EndRequest(HttpContext context)
        {
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.getitem
        /// </summary>
        public override SessionStateStoreData GetItem(HttpContext context, string sessionId, out bool locked,
                                                      out TimeSpan lockAge, out object lockId,
                                                      out SessionStateActions actions)
        {
            VerifyEnableSessionStateIsSetToReadOnly();

            try
            {
                Logger.Debug("Beginning GetItem. SessionId={0}, Application={1}.", sessionId, ApplicationName);

                var item = GetSessionStoreItem(false, context, sessionId, out locked, out lockAge, out lockId, out actions);

                ProcessActionFlags(sessionId, out actions);

                Logger.Debug("Completed GetItem. SessionId={0}, Application={1}, locked={2}, lockAge={3}, lockId={4}, actions={5}.",
                    sessionId, ApplicationName, locked, lockAge, lockId, actions);

                return item;
            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during GetItem. SessionId={0}, Application={1}.",
                    sessionId, ApplicationName), ex);
                throw;
            }
        }

        private void ProcessActionFlags(string sessionId, out SessionStateActions actions)
        {
            if (!IsModeCookieless())
            {
                actions = SessionStateActions.None;
                return;
            }
            
            using (var documentSession = DocumentStore.OpenSession())
            {

                var sessionState =
                    documentSession.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId,
                        ApplicationName));

                if (sessionState == null)
                {
                    actions = SessionStateActions.None;
                    return;
                }

                actions = sessionState.Flags;

                if (sessionState.Flags == SessionStateActions.InitializeItem)
                {
                    sessionState.Flags = 0;

                    documentSession.Store(sessionState);
                    documentSession.SaveChanges();
                }
            }
        }

        private bool IsModeCookieless()
        {
            SessionStateSection sessionStateSection = WebConfigurationManager.GetSection("system.web/sessionState") as SessionStateSection;
            if (sessionStateSection == null)
            {
                throw new ConfigurationException("The sessionState section does not exist");
            }

            return sessionStateSection.Cookieless == HttpCookieMode.UseUri &&
                   sessionStateSection.RegenerateExpiredSessionId;
        }

        private void VerifyEnableSessionStateIsSetToReadOnly()
        {
            PagesSection pagesSection = WebConfigurationManager.GetSection("system.web/pages") as PagesSection;
            if (pagesSection == null)
            {
                throw new ConfigurationException("The pages section does not exist");
            }

            if (pagesSection.EnableSessionState == PagesEnableSessionState.True ||
                pagesSection.EnableSessionState == PagesEnableSessionState.False)
            {
                throw new ConfigurationException("EnableState must be set to ReadOnly");
            }
        }

        private void VerifyEnableSessionStateIsSetToTrue()
        {
            PagesSection pagesSection = WebConfigurationManager.GetSection("system.web/pages") as PagesSection;
            if (pagesSection == null)
            {
                throw new ConfigurationException("The pages section does not exist");
            }

            if (pagesSection.EnableSessionState == PagesEnableSessionState.ReadOnly ||
                pagesSection.EnableSessionState == PagesEnableSessionState.False)
            {
                throw new ConfigurationException("EnableState must be set to True");
            }
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.getitemexclusive(v=vs.110).aspx
        /// </summary>
        public override SessionStateStoreData GetItemExclusive(HttpContext context,
                                                               string sessionId,
                                                               out bool locked,
                                                               out TimeSpan lockAge,
                                                               out object lockId,
                                                               out SessionStateActions actions)
        {
            VerifyEnableSessionStateIsSetToTrue();
            try
            {
                Logger.Debug("Beginning GetItemExclusive. SessionId={0}; Application={1}.", sessionId, ApplicationName);

                var item = GetSessionStoreItem(true, context, sessionId, out locked,
                                           out lockAge, out lockId, out actions);

                using (IDocumentSession session = _documentStore.OpenSession())
                {
                    var sessionDataItem = session.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));
                    if (sessionDataItem != null)
                    {
                        UpdateExpiryOnStoredDataItem(sessionDataItem, session);
                        session.SaveChanges();
                    }
                }

                Logger.Debug("Completed GetItemExclusive. SessionId={0}, Application={1}, locked={2}, lockAge={3}, lockId={4}, actions={5}.",
                    sessionId, ApplicationName, locked, lockAge, lockId, actions);

                return item;
            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during GetItemExclusive. SessionId={0}, Application={1}.", sessionId, ApplicationName), ex);
                throw;
            }

        }

        public override void Initialize(string name, NameValueCollection config)
        {
            Initialize(name, config, null);
        }

        internal void Initialize(string name, NameValueCollection config, IDocumentStore documentStore)
        {

            try
            {
                CheckConfigIsValid(config);
                _configuration = config;

                Logger.Debug("Beginning Initialize. Name= {0}. Config={1}.",
                    name, _configuration.AllKeys.Aggregate("", (aggregate, next) => aggregate + next + ":" + _configuration[next]));

                name = GetSessionStoreName(name);

                base.Initialize(name, _configuration);
                SetApplicationName();

                if (documentStore != null)
                    _documentStore = documentStore;

                if (DocumentStore == null)
                {
                    CheckConnectionStringIsValid();
                    CreateAndInitializeDocumentStore();
                }

                Logger.Debug("Completed Initalize.");

            }
            catch (Exception ex)
            {
                LogInitializationError(ex);
                throw;
            }
        }

        /// <summary>
        /// Performs any initialization required by your session-state store provider.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        public override void InitializeRequest(HttpContext context)
        {
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.releaseitemexclusive(v=vs.110).aspx
        /// </summary>
        public override void ReleaseItemExclusive(HttpContext context, string sessionId, object lockId)
        {
            try
            {
                Logger.Debug("Beginning ReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}.", sessionId, ApplicationName, lockId);

                using (var documentSession = DocumentStore.OpenSession())
                {
                    //don't tolerate stale data
                    documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                    var sessionState =
                            documentSession
                                .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));

                    //if the session-state is not present (it may have expired and been removed) or
                    //the locked id does not match, then we do nothing
                    if (sessionState == null || 
                        sessionState.LockId != (int)lockId || 
                        sessionState.ApplicationName != ApplicationName ||
                        sessionState.SessionId != sessionId)
                    {
                        Logger.Debug(
                            "Session state was not present or lock id did not match. Session id: {0}; Application: {1}; Lock id: {2}.",
                            sessionId, ApplicationName, lockId);
                        return;
                    }

                    sessionState.Locked = false;

                    //update the expiry
                    UpdateExpiryOnStoredDataItem(sessionState, documentSession);

                    documentSession.SaveChanges();
                }

                Logger.Debug("Completed ReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}.", sessionId, ApplicationName, lockId);

            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during ReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}.", sessionId, ApplicationName, lockId), ex);
                throw;
            }
        }

        private void UpdateExpiryOnStoredDataItem(SessionStateDocument sessionState, IDocumentSession documentSession)
        {
            var expiry = TimeProviderBase.Current.UtcNow.AddMinutes(SessionStateConfig.Timeout.TotalMinutes);
            sessionState.Expiry = expiry;
            documentSession.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expiry);
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.removeitem
        /// </summary>
        public override void RemoveItem(HttpContext context, string sessionId, object lockId, SessionStateStoreData item)
        {
            try
            {
                Logger.Debug("Beginning RemoveItem. SessionId={0}; Application={1}; lockId={2}.", sessionId,
                    ApplicationName, lockId);

                using (var documentSession = DocumentStore.OpenSession())
                {
                    //don't tolerate stale data
                    documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                    var sessionStateDocument = documentSession
                                .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));


                    if (sessionStateDocument != null && 
                        sessionStateDocument.LockId == (int)lockId &&
                        sessionStateDocument.ApplicationName == ApplicationName &&
                        sessionStateDocument.SessionId == sessionId)
                    {
                        documentSession.Delete(sessionStateDocument);
                        documentSession.SaveChanges();
                    }
                }

                Logger.Debug("Completed RemoveItem. SessionId={0}; Application={1}; lockId={2}.", sessionId,
                    ApplicationName, lockId);
            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during RemoveItem. SessionId={0}; Application={1}; lockId={2}", sessionId, ApplicationName, lockId),
                    ex);
                throw;
            }
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.resetitemtimeout
        /// </summary>
        public override void ResetItemTimeout(HttpContext context, string sessionId)
        {
            VerifyEnableSessionStateIsNotSetToReadOnly();
            try
            {
                Logger.Debug("Beginning ResetItemTimeout. SessionId={0}; Application={1}.", sessionId, ApplicationName);


                using (var documentSession = DocumentStore.OpenSession())
                {
                    //we never want to over-write data with this method
                    documentSession.Advanced.UseOptimisticConcurrency = true;

                    var sessionStateDocument = documentSession
                        .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));


                    if (sessionStateDocument != null)
                    {
                        var expiry = TimeProviderBase.Current.UtcNow.AddMinutes(SessionStateConfig.Timeout.TotalMinutes);
                        sessionStateDocument.Expiry = expiry;
                        documentSession.Advanced.GetMetadataFor(sessionStateDocument)["Raven-Expiration-Date"] =
                            new RavenJValue(expiry);

                        documentSession.SaveChanges();
                    }
                }

                Logger.Debug("Completed ResetItemTimeout. SessionId={0}; Application={1}.", sessionId, ApplicationName);
            }
            catch (ConcurrencyException ex)
            {
                //swallow, we don't care 
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error during ResetItemTimeout. SessionId=" + sessionId, ex);
                throw;
            }
        }

        private void VerifyEnableSessionStateIsNotSetToReadOnly()
        {
            PagesSection pagesSection = WebConfigurationManager.GetSection("system.web/pages") as PagesSection;
            if (pagesSection == null)
            {
                throw new ConfigurationException("The pages section does not exist");
            }

            if (pagesSection.EnableSessionState == PagesEnableSessionState.ReadOnly)
            {
                throw new ConfigurationException("EnableState must not be set to ReadOnly");
            }
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.setandreleaseitemexclusive
        /// </summary>
        public override void SetAndReleaseItemExclusive(HttpContext context, string sessionId, SessionStateStoreData item,
                                                        object lockId, bool newItem)
        {
            VerifyEnableSessionStateIsSetToTrue();
            try
            {
                Logger.Debug(
                    " Beginning SetAndReleaseItemExclusive. SessionId={0}, Application: {1}, LockId={2}, newItem={3}.",
                    sessionId, ApplicationName, lockId, newItem);

                if (item == null)
                    throw new ArgumentNullException("item");

                var serializedItems = Serialize((SessionStateItemCollection)item.Items);

                using (var documentSession = DocumentStore.OpenSession())
                {
                    //don't tolerate stale data
                    documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                    SessionStateDocument sessionStateDocument;

                    if (newItem) //if we are creating a new document
                    {
                        sessionStateDocument = new SessionStateDocument(sessionId, ApplicationName);

                        documentSession.Store(sessionStateDocument);

                    }
                    else //we are not creating a new document, so load it
                    {
                        sessionStateDocument =
                            documentSession.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));

                        //if the lock identifier does not match, then we don't modifiy the data
                        if (sessionStateDocument.LockId != (int)lockId || 
                            sessionStateDocument.ApplicationName != ApplicationName ||
                            sessionStateDocument.SessionId != sessionId)
                        {
                            Logger.Debug(
                                "Lock Id does not match, so data will not be modified. Session Id: {0}; Application: {1}; Lock Id {2}.",
                                sessionId, ApplicationName, lockId);
                            return;
                        }
                    }

                    sessionStateDocument.SessionItems = serializedItems;
                    sessionStateDocument.Locked = false;

                    //set the expiry
                    var expiry = DateTime.UtcNow.AddMinutes(SessionStateConfig.Timeout.TotalMinutes);
                    sessionStateDocument.Expiry = expiry;
                    documentSession.Advanced.GetMetadataFor(sessionStateDocument)["Raven-Expiration-Date"] = new RavenJValue(expiry);

                    documentSession.SaveChanges();
                }

                Logger.Debug("Completed SetAndReleaseItemExclusive. SessionId={0}; Application:{1}; LockId={2}; newItem={3}.", sessionId, ApplicationName, lockId, newItem);

            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during SetAndReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}, newItem={3}.", sessionId, ApplicationName, lockId, newItem), ex);
                throw;
            }
        }

        /// <summary>
        /// see http://msdn.microsoft.com/en-us/library/system.web.sessionstate.sessionstatestoreproviderbase.setitemexpirecallback
        /// Takes as input a delegate that references the Session_OnEnd event defined in the Global.asax file. 
        /// If the session-state store provider supports the Session_OnEnd event, a local reference to the 
        /// SessionStateItemExpireCallback parameter is set and the method returns true; otherwise, the method returns false.
        /// </summary>
        /// <param name="expireCallback">A callback.</param>
        /// <returns>False.</returns>
        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        //
        // GetSessionStoreItem is called by both the GetItem and 
        // GetItemExclusive methods. GetSessionStoreItem retrieves the 
        // session data from the data source. If the lockRecord parameter
        // is true (in the case of GetItemExclusive), then GetSessionStoreItem
        // locks the record and sets a new LockId and LockDate.
        //
        private SessionStateStoreData GetSessionStoreItem(bool lockRecord,
                                                          HttpContext context,
                                                          string sessionId,
                                                          out bool locked,
                                                          out TimeSpan lockAge,
                                                          out object lockId,
                                                          out SessionStateActions actionFlags)
        {
            // Initial values for return value and out parameters.
            lockAge = TimeSpan.Zero;
            lockId = null;
            locked = false;
            actionFlags = 0;

            using (var documentSession = DocumentStore.OpenSession())
            {
                //don't tolerate stale data
                documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                Logger.Debug("Retrieving item from RavenDB. SessionId: {0}; Application: {1}.", sessionId, ApplicationName);

                var sessionState = documentSession.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));

                if (sessionState == null)
                {
                    Logger.Debug("Item not found in RavenDB with SessionId: {0}; Application: {1}.", sessionId, ApplicationName);
                    return null;
                }

                //if the record is locked, we can't have it.
                if (sessionState.Locked)
                {
                    Logger.Debug("Item retrieved is locked. SessionId: {0}; Application: {1}.", sessionId, ApplicationName);

                    locked = true;
                    lockAge = TimeProviderBase.Current.UtcNow.Subtract(sessionState.LockDate);
                    lockId = sessionState.LockId;
                    return null;
                }

                //generally we shouldn't get expired items, as the expiration bundle should clean them up,
                //but just in case the bundle isn't installed, or we made the window, we'll delete expired items here.
                if (TimeProviderBase.Current.UtcNow > sessionState.Expiry)
                {
                    Logger.Debug("Item retrieved has expired. SessionId: {0}; Application: {1}; Expiry (UTC): {2}", sessionId, ApplicationName, sessionState.Expiry);

                    try
                    {
                        documentSession.Delete(sessionState);
                        documentSession.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        //we never want this clean-up op to throw
                        Logger.DebugException("Exception thrown while attempting to remove expired item.", ex);
                    }

                    return null;
                }

                if (lockRecord)
                {
                    sessionState.Locked = true;
                    sessionState.LockId += 1;
                    sessionState.LockDate = DateTime.UtcNow;

                    documentSession.SaveChanges();
                }

                lockId = sessionState.LockId;
                return
                    sessionState.Flags == SessionStateActions.InitializeItem
                        ? new SessionStateStoreData(new SessionStateItemCollection(),
                                                    _sessionStateUtility.GetSessionStaticObjects(context),
                                                    (int)SessionStateConfig.Timeout.TotalMinutes)
                        : Deserialize(context, sessionState.SessionItems, (int)SessionStateConfig.Timeout.TotalMinutes);
            }
        }

        internal string Serialize(SessionStateItemCollection items)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    if (items != null)
                        items.Serialize(writer);

                    writer.Flush();
                    writer.Close();

                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        internal static SessionStateStoreData Deserialize(HttpContext context,
                                                         string serializedItems, int timeout)
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(serializedItems)))
            {
                var sessionItems = new SessionStateItemCollection();

                if (stream.Length > 0)
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        sessionItems = SessionStateItemCollection.Deserialize(reader);
                    }
                }

                ISessionStateUtility sessionStateUtility = new CustomSessionStateUtility();

                return new SessionStateStoreData(sessionItems,
                                                 sessionStateUtility.GetSessionStaticObjects(context),
                                                 timeout);
            }
        }
    }
}