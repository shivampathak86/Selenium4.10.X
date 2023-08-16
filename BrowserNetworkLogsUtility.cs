using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.V112.Network;
using System;
using System.Threading.Tasks;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V112.DevToolsSessionDomains;

namespace ECataLogComman.Library.Utilities
{
    public static class BrowserNetworkLogsUtility
    {
        #region Private Fields
        private static IWebDriver _webDriver;
        private static IDevTools _devTools;
        private static DevToolsSessionDomains _domains;
        private static readonly object _lock = new object();
        #endregion

        #region Public Event

        public static event EventHandler<RequestWillBeSentEventArgs> NetworkRequestWillBeSent;

        #endregion

        #region Public Methods
        public static void Initialize(IWebDriver webDriver)
        {
            lock (_lock)
            {
                _webDriver = webDriver;
                _devTools = _webDriver as IDevTools;

                if (_devTools != null)
                {
                    _domains = _devTools.GetDevToolsSession().GetVersionSpecificDomains<DevToolsSessionDomains>();
                }
            }
        }

        public static void SubcribeToNetworkRequestEvents()
        {
            lock (_lock)
            {
                if (_domains != null)
                {
                    _domains.Network.RequestWillBeSent += Network_RequestWillBeSent;
                    Task task = _domains.Network.Enable(new EnableCommandSettings());
                    task.Wait();
                }
            }
        }
        #endregion

        #region Private Methods
        private static void Network_RequestWillBeSent(object sender, RequestWillBeSentEventArgs e)
        {
            NetworkRequestWillBeSent?.Invoke(null, e); 
        }

        #endregion
    }

}
