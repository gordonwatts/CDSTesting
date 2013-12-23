using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace DesktopUnitTests
{
    [TestClass]
    public class CERNLogin
    {
        /// <summary>
        /// This is meant to be a desktop version of the code to log-in to the CERN CDS site.
        /// </summary>
        /// <remarks>
        /// Can we just get headers in some places in case the resource is large (like several megs)?
        /// </remarks>
        [TestMethod]
        public async Task LoginToCDS()
        {
            // Config

            string url = "https://cds.cern.ch/record/1512932?ln=en";
            var cert = FindCert();

            // Just to make sure, even though this is a test.

            var u = new Uri(url); // Fails if the url is badly formed.
            Assert.IsNotNull(cert);

            // Step one, access the protected page. We get back a login-in URL

            var initialRequest = CreateRequest(u, cert);
            DumpRequest(initialRequest);
            var resp = await initialRequest.GetResponseAsync();

            DumpResponseInfo(resp);

            Assert.IsTrue(resp.ResponseUri.AbsolutePath.StartsWith("/adfs/ls/"), string.Format("Didn't see the login redirect - this url doesn't require auth? - response uri '{0}'", url));

            // Step two: Create a new web request using this redirect, and add the cert to it.
            // NOTE: it looks like we could have added a client cert here. Is that was is needed?
            // NOTE: The method is POST b.c. that is what it is in the perl code. Test that?

            var loginRequest = CreateRequest(resp.ResponseUri, cert);
            loginRequest.Method = "POST";
            loginRequest.ContentLength = 0;
            DumpRequest(loginRequest);

            resp = await loginRequest.GetResponseAsync();
            DumpResponseInfo(resp);

            using (var rdr = new StreamReader(resp.GetResponseStream()))
            {
                var text = await rdr.ReadToEndAsync();
                Assert.IsFalse(text.Contains("HTTP Error 401.2 - Unauthorized"), "unauth error came back.");
            }

            // Step three: the request to the original resource to see if it worked (or not).
            var finalRequest = CreateRequest(u, cert);
            DumpRequest(finalRequest);

            resp = await finalRequest.GetResponseAsync();
            DumpResponseInfo(resp);
        }

        /// <summary>
        /// Dump details about the request
        /// </summary>
        /// <param name="initialRequest"></param>
        private void DumpRequest(HttpWebRequest initialRequest)
        {
            Console.WriteLine("Requesting {0}", initialRequest.RequestUri.OriginalString);
            if (initialRequest.SupportsCookieContainer && initialRequest.CookieContainer != null)
            {
                var allcookies = initialRequest.CookieContainer.GetCookies(initialRequest.RequestUri);
                if (allcookies != null)
                    foreach (var c in allcookies)
                    {
                        Console.WriteLine("  cook: {0}", c.ToString());
                    }
            }
        }

        private static CookieContainer gCookies = new CookieContainer();

        /// <summary>
        /// Generate prep stuff for our web request
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        private HttpWebRequest CreateRequest(Uri u, X509Certificate2 cerncert)
        {
            var h = WebRequest.CreateHttp(u);

            // This is required in order for the interactions to work properly. This causes, among other things, cookies to
            // be set back and forth.
            h.UserAgent = "curl-sso-certificate/0.5.1(Mozilla)";

            // Cache cookies going back and forth, with the sso requires.
            h.CookieContainer = gCookies;

            // Add the client cert for login.
            // NOTE: not obvious when and how often this has to be attached.
            // Perhaps only when the actual log-in page is referenced?
            h.ClientCertificates.Add(cerncert);

            return h;
        }

        private static void DumpResponseInfo(WebResponse resp)
        {
            Console.WriteLine("Who responded: {0}", resp.ResponseUri.ToString());
            Console.WriteLine("  Content Length: {0}", resp.ContentLength);
            foreach (string header in resp.Headers.Keys)
            {
                Console.WriteLine(" Hdr: {0} => {1}", header, resp.Headers[header]);
            }
            var respH = resp as HttpWebResponse;
            foreach (var c in respH.Cookies)
            {
                Console.WriteLine(" Cookie: {0}", c.ToString());
            }
        }

        /// <summary>
        /// Find the CERN Cert already installed in a cert store somewhere.
        /// </summary>
        /// <returns></returns>
        private static X509Certificate2 FindCert()
        {
            var st = new X509Store(StoreName.My);
            st.Open(OpenFlags.ReadOnly);
            try
            {
                var allcerts = st.Certificates.Cast<X509Certificate2>().Where(s => s.SubjectName.Name.Contains("DC=cern") && s.SubjectName.Name.Contains("OU=Users")).ToArray();
                Assert.AreEqual(1, allcerts.Length, "Should have exactly one good CERT in the store!");
                return allcerts[0];
            }
            finally
            {
                st.Close();
            }
        }
    }
}
