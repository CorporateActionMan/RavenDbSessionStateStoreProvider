﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using Raven.Client.Document;
using Raven.Json.Linq;

namespace Raven.AspNet
{
    /// <summary>
    /// An ASP.NET session-state store-provider implementation (http://msdn.microsoft.com/en-us/library/ms178588.aspx) using 
    /// RavenDb (http://ravendb.net) for persistence.
    /// </summary>
    public class RavenSessionStateStoreProvider : SessionStateStoreProviderBase
    {
        private const int RetriesOnConcurrentConfictsDefault = 3;


        private string _applicationName;
        private IDocumentStore _documentStore;
        private SessionStateSection _sessionStateConfig;
        private int _retriesOnConcurrentConflicts = RetriesOnConcurrentConfictsDefault;


        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(config["connectionStringName"]))
                throw new ConfigurationErrorsException("Must supply a connectionStringName.");

            if (string.IsNullOrEmpty(name))
                name = "RavenSessionStateStore";

            base.Initialize(name, config);

            if (config["retriesOnConcurrentConflicts"] != null)
            {
                int retriesOnConcurrentConflicts;
                if (int.TryParse(config["retriesOnConcurrentConflicts"], out retriesOnConcurrentConflicts))
                    _retriesOnConcurrentConflicts = retriesOnConcurrentConflicts;
            }

            _applicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
            _sessionStateConfig = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
            _documentStore = new DocumentStore
            {
                ConnectionStringName = config["connectionStringName"],
                Conventions = { FindIdentityProperty = q => q.Name == "SessionId" }
            };
            _documentStore.Initialize();
        }


       
        /// <summary>
        /// Retrieves session values and information from the session data store and locks the session-item data 
        /// at the data store for the duration of the request. 
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        /// <param name="locked">An output parameter indicating whether the item is currently exclusively locked.</param>
        /// <param name="lockAge">The age of the exclusive lock (if present)</param>
        /// <param name="lockId">The identifier of the exclusive lock (if present)</param>
        /// <param name="actions">Used with sessions whose Cookieless property is true, 
        /// when the regenerateExpiredSessionId attribute is set to true. 
        /// An actionFlags value set to InitializeItem (1) indicates that the entry in the session data store is a 
        /// new session that requires initialization.</param>
        /// <returns>The session data</returns>
        public override SessionStateStoreData GetItemExclusive(HttpContext context,
                                                               string id,
                                                               out bool locked,
                                                               out TimeSpan lockAge,
                                                               out object lockId,
                                                               out SessionStateActions actions)
        {
            return GetSessionStoreItem(true, context, id, _retriesOnConcurrentConflicts, out locked,
                                       out lockAge, out lockId, out actions);
        }

        /// <summary>
        /// This method performs the same work as the GetItemExclusive method, except that it does not attempt to lock the session item in the data store.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        /// <param name="locked">An output parameter indicating whether the item is currently exclusively locked.</param>
        /// <param name="lockAge">The age of the exclusive lock (if present)</param>
        /// <param name="lockId">The identifier of the exclusive lock (if present)</param>
        /// <param name="actions">Used with sessions whose Cookieless property is true, 
        /// when the regenerateExpiredSessionId attribute is set to true. 
        /// An actionFlags value set to InitializeItem (1) indicates that the entry in the session data store is a 
        /// new session that requires initialization.</param>
        /// <returns>The session data</returns>
        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked,
                                                      out TimeSpan lockAge, out object lockId,
                                                      out SessionStateActions actions)
        {
            return GetSessionStoreItem(false, context, id, 0, out locked, out lockAge, out lockId, out actions);
        }


        /// <summary>
        /// If the newItem parameter is true, the SetAndReleaseItemExclusive method inserts a new item into the data store with the supplied values. 
        /// Otherwise, the existing item in the data store is updated with the supplied values, and any lock on the data is released. 
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        /// <param name="item">The current session values to be stored</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        /// <param name="newItem">If true, a new item is inserted into the store.  Otherwise, the existing item in 
        /// the data store is updated with the supplied values, and any lock on the data is released. </param>
        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item,
                                                        object lockId, bool newItem)
        {
            var serializedItems = Serialize((SessionStateItemCollection)item.Items);

            using (var documentSession = _documentStore.OpenSession())
            {
                SessionState sessionState;
                if (newItem)
                {
                    sessionState = documentSession.Query<SessionState>().SingleOrDefault(
                        x => x.SessionId == id && x.ApplicationName == _applicationName && x.Expires < DateTime.UtcNow);

                    if (sessionState == null)
                    {
                        sessionState = new SessionState ( id, _applicationName );
                        documentSession.Store(sessionState);
                    }

                }
                else
                {
                    sessionState = documentSession.Query<SessionState>().Single(
                        x => x.SessionId == id && x.ApplicationName == _applicationName && x.LockId == (int)lockId);
                }

                var expiry = DateTime.UtcNow.AddMinutes(_sessionStateConfig.Timeout.Minutes);
                sessionState.Expires = expiry;
                documentSession.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expiry);
                sessionState.SessionItems = serializedItems;
                sessionState.Locked = false;

                documentSession.SaveChanges();
            }
        }

        /// <summary>
        /// Releases the lock on an item in the session data store.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            using (var documentSession = _documentStore.OpenSession())
            {
                var sessionState = documentSession.Query<SessionState>().SingleOrDefault(
                    x => x.SessionId == id && x.ApplicationName == _applicationName && x.LockId == (int)lockId);

                if (sessionState != null)
                {
                    sessionState.Locked = false;

                    var expiry = DateTime.UtcNow.AddMinutes(_sessionStateConfig.Timeout.Minutes);
                    sessionState.Expires = expiry;
                    documentSession.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expiry);

                    documentSession.SaveChanges();
                }
            }
        }


        /// <summary>
        /// deletes the session information from the data store where the data store item matches the supplied SessionID value, 
        /// the current application, and the supplied lock identifier.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        /// <param name="lockId">The exclusive-lock identifier.</param>
        /// <param name="item"></param>
        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            using (var documentSession = _documentStore.OpenSession())
            {
                var sessionState =
                    documentSession.Query<SessionState>().SingleOrDefault(
                        x => x.SessionId == id && x.ApplicationName == _applicationName && x.LockId == (int)lockId);

                if (sessionState != null)
                {
                    documentSession.Delete(sessionState);
                    documentSession.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Resets the expiry timeout for a session item.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        public override void ResetItemTimeout(HttpContext context, string id)
        {
            using (var documentSession = _documentStore.OpenSession())
            {
                var sessionState = documentSession.Query<SessionState>().SingleOrDefault(
                    x => x.SessionId == id && x.ApplicationName == _applicationName);

                if (sessionState != null)
                {
                    var expiry = DateTime.UtcNow.AddMinutes(_sessionStateConfig.Timeout.Minutes);
                    sessionState.Expires = expiry;
                    documentSession.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expiry);
                    documentSession.SaveChanges();
                }
            }
        }


        /// <summary>
        /// Adds an uninitialized item to the session data store.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="id">The session identifier.</param>
        /// <param name="timeout">The expiry timeout in minutes.</param>
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            using (var documentSession = _documentStore.OpenSession())
            {
                var expiry = DateTime.UtcNow.AddMinutes(timeout);

                var sessionState = new SessionState(id, _applicationName)
                {
                    Expires = expiry
                };

                documentSession.Store(sessionState);
                documentSession.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expiry);
                documentSession.SaveChanges();
            }
        }


        /// <summary>
        ///  returns a new SessionStateStoreData object with an empty ISessionStateItemCollection object, 
        ///  an HttpStaticObjectsCollection collection, and the specified Timeout value.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="timeout">The expiry timeout in minutes.</param>
        /// <returns>A newly created SessionStateStoreData object.</returns>
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(),
                                             SessionStateUtility.GetSessionStaticObjects(context),
                                             timeout);
        }

        /// <summary>
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

        /// <summary>
        /// Performs any initialization required by your session-state store provider.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        public override void InitializeRequest(HttpContext context)
        {
        }

        /// <summary>
        /// Performs any cleanup required by your session-state store provider.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        public override void EndRequest(HttpContext context)
        {
        }

        public override void Dispose()
        {
            if (_documentStore != null)
                _documentStore.Dispose();
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
                                                          string id,
                                                          int retriesRemaining,
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

            using (var documentSession = _documentStore.OpenSession())
            {
                //don't tolerate stale data
                documentSession.Advanced.AllowNonAuthoritiveInformation = false;
                //if we get a concurrency conflict, then we want to know about it
                documentSession.Advanced.UseOptimisticConcurrency = true;

                var sessionState =
                    documentSession.Query<SessionState>().SingleOrDefault(
                        x => x.SessionId == id && x.ApplicationName == _applicationName);

                if (sessionState == null)
                    return null;

                //if the record is locked, we can't have it.
                if (sessionState.Locked)
                {
                    locked = true;
                    lockAge = DateTime.UtcNow.Subtract(sessionState.LockDate);
                    lockId = sessionState.LockId;
                    return null;
                }

                //generally we shouldn't get expired items, as the expiration bundle should clean them up,
                //but just in case the bundle isn't installed, or we made the window, we'll delete expired items here.
                if (sessionState.Expires < DateTime.UtcNow)
                {
                    try
                    {
                        documentSession.Delete(sessionState);
                        documentSession.SaveChanges();
                    }
                    catch (ConcurrencyException)
                    {
                        //we swallow, as we don't care. Presumably the other modifier deleted it as well.
                    }

                    return null;
                }

                if (lockRecord)
                {
                    sessionState.Locked = true;
                    sessionState.LockId += 1;
                    sessionState.LockDate = DateTime.UtcNow;

                    try
                    {
                        documentSession.SaveChanges();
                    }
                    catch (ConcurrencyException)
                    {
                        if (retriesRemaining > 0)
                            return GetSessionStoreItem(true, context, id, retriesRemaining - 1, out locked,
                                                       out lockAge, out lockId, out actionFlags);

                        throw;
                    }
                }

                lockId = sessionState.LockId;
                return
                    sessionState.Flags == SessionStateActions.InitializeItem
                        ? new SessionStateStoreData(new SessionStateItemCollection(),
                                                    SessionStateUtility.GetSessionStaticObjects(context),
                                                    _sessionStateConfig.Timeout.Minutes)
                        : Deserialize(context, sessionState.SessionItems, _sessionStateConfig.Timeout.Minutes);
            }
        }

        private string Serialize(SessionStateItemCollection items)
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

        private static SessionStateStoreData Deserialize(HttpContext context,
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

                return new SessionStateStoreData(sessionItems,
                                                 SessionStateUtility.GetSessionStaticObjects(context),
                                                 timeout);
            }
        }
    }
}