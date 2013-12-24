using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            // The CERT must be attached here or the request will fail.

            var initialRequest = await CreateRequest(u, cert);
            initialRequest.ClientCertificates.Add(cert);
            DumpRequest(initialRequest);
            var resp = await initialRequest.GetResponseAsync();

            DumpResponseInfo(resp);

            Assert.IsTrue(resp.ResponseUri.AbsolutePath.StartsWith("/adfs/ls/"), string.Format("Didn't see the login redirect - this url doesn't require auth? - response uri '{0}'", url));

            // Step two: Create a new web request using this redirect, and add the cert to it.
            // The CERT must be attached here, or the request will fail. But after this cookies are good enough to
            // power the access.

            var loginRequest = await CreateRequest(resp.ResponseUri, cert);
            loginRequest.ClientCertificates.Add(cert);
            DumpRequest(loginRequest);

            resp = await loginRequest.GetResponseAsync();
            DumpResponseInfo(resp);

            Uri homeSiteLoginRedirect = null;
            var repostFields = new Dictionary<string, string>();
            using (var rdr = new StreamReader(resp.GetResponseStream()))
            {
                var text = await rdr.ReadToEndAsync();
                Assert.IsFalse(text.Contains("HTTP Error 401.2 - Unauthorized"), "unauth error came back.");

                // This should contain a redirect that is deep in a http form. We need to parse that out.
                var doc = new HtmlDocument();
                doc.LoadHtml(text);
                var forms = doc.DocumentNode.SelectNodes("//form").ToArray();
                Assert.AreEqual(1, forms.Length, "Should be only one form back");
                var form = forms[0];

                homeSiteLoginRedirect = new Uri(form.Attributes["action"].Value);

                foreach (var inputs in form.SelectNodes("//input[@type]"))
                {
                    if (inputs.Attributes["type"].Value == "hidden")
                    {
                        var name = inputs.Attributes["name"].Value;
                        var value = WebUtility.HtmlDecode(inputs.Attributes["value"].Value);
                        Console.WriteLine("  Form data found: {0} with value {1}", name, "<temp>");
                        repostFields[name] = value;
                    }
                }
            }

            // Step three: request the login redirect
            var loginHomeRedirect = await CreateRequest(homeSiteLoginRedirect, cert, repostFields);
            DumpRequest(loginHomeRedirect);
            resp = await loginHomeRedirect.GetResponseAsync();
            DumpResponseInfo(resp);
            Assert.AreEqual(url, resp.ResponseUri.OriginalString, "Redirect to where we wanted to go!");

            // Step 4: the request to the original resource to see if it worked (or not).
            var finalRequest = await CreateRequest(u, cert);
            DumpRequest(finalRequest);
            resp = await finalRequest.GetResponseAsync();
            DumpResponseInfo(resp);
            using (var rdr = new StreamReader(resp.GetResponseStream()))
            {
                var text = await rdr.ReadToEndAsync();
                Console.WriteLine("==> Size of html is {0} bytes", text.Length);
                var doc = new HtmlDocument();
                doc.LoadHtml(text);
                foreach (var titleNodes in doc.DocumentNode.SelectNodes("//title"))
                {
                    Console.WriteLine("  HTML Title: {0}", titleNodes.InnerHtml);
                }
            }
        }

        /// <summary>
        /// Dump details about the request
        /// </summary>
        /// <param name="initialRequest"></param>
        private void DumpRequest(HttpWebRequest initialRequest)
        {
            Console.WriteLine("Requesting {0}", initialRequest.RequestUri.OriginalString);
            Console.WriteLine("  Content-Length: {0}", initialRequest.ContentLength);
            if (initialRequest.ContentLength > 0)
                Console.WriteLine("  Content-Type: {0}", initialRequest.ContentType);
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
        private async Task<HttpWebRequest> CreateRequest(Uri u, X509Certificate2 cerncert, Dictionary<string, string> postData = null)
        {
            var h = WebRequest.CreateHttp(u);

            // This is required in order for the interactions to work properly. This causes, among other things, cookies to
            // be set back and forth.
            h.UserAgent = "curl-sso-certificate/0.5.1(Mozilla)";

            // Cache cookies going back and forth, with the sso requires.
            h.CookieContainer = gCookies;

            // If there is post data, add it
            if (postData != null)
            {
                h.Method = "POST";
                h.ContentType = "application/x-www-form-urlencoded";
                StringBuilder bld = new StringBuilder();
                bool first = true;
                foreach (var item in postData)
                {
                    if (!first)
                        bld.Append("&");
                    first = false;
                    bld.AppendFormat("{0}={1}", item.Key, Uri.EscapeDataString(item.Value));
                }
                Byte[] PostBuffer = System.Text.Encoding.UTF8.GetBytes(bld.ToString());
                using (var wrtr = await h.GetRequestStreamAsync())
                {
                    await wrtr.WriteAsync(PostBuffer, 0, PostBuffer.Length);
                    wrtr.Close();
                }
            }

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
