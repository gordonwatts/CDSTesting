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
        /// Make sure we have nothing left over from last time!
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            gCookies = new CookieContainer();
        }

        [TestMethod]
        public async Task LoginToCDSWithUserPass()
        {
            // Config
            string url = "https://cds.cern.ch/record/1512932?ln=en";
            var logininfo = LookupUserPass();

            // x-chcks

            var u = new Uri(url);

            // Access the protected page. This should redirect to the password page. We'll
            // the form data from three.
            var initialRequest = await CreateRequestUP(u);
            DumpRequest(initialRequest);
            var resp = await initialRequest.GetResponseAsync();
            DumpResponseInfo(resp);
            var signinFormData = await GrabFormData(resp.GetResponseStream());

            // Set the user name and password, and repost.

            int oldNumberKeys = signinFormData.RepostFields.Count;
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$txtFormsLogin"] = logininfo.Item1;
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$txtFormsPassword"] = logininfo.Item2;
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$btnFormsLogin"] = "Sign in";
            Assert.AreEqual(oldNumberKeys, signinFormData.RepostFields.Count, "The names of the login and password fields have changed!");

            // Next task is to alter the repost fields a little bit. If we don't do this, we fail authentication. Yes... We do! Yikes! :-)
            signinFormData.RepostFields.Remove("ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$btnSelectFederation");
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$drpFederation"] = "";

            // If we are doing relative URI's, fix it up.
            var loginUri = signinFormData.Action;
            if (!loginUri.IsAbsoluteUri)
            {
                loginUri = new Uri(resp.ResponseUri, loginUri);
            }

            var loginHomeRedirect = await CreateRequestUP(loginUri, signinFormData.RepostFields);
            DumpRequest(loginHomeRedirect);
            resp = await loginHomeRedirect.GetResponseAsync();
            DumpResponseInfo(resp);

            // Last thing to do is get the actual data, assuming the login went well!
            var parsedData = await ParseReply(resp);
            Uri homeSiteLoginRedirect = parsedData.Item1;
            var repostFields = parsedData.Item2;

            // Step three: request the login redirect
            // NOTE: I've gotten "bad gateway" sometimes with this request - but it works
            // the next time we try. CERN's SSO isn't known for being reliable, so we may
            // have to account for that!
            var finalDownload = await CreateRequestUP(homeSiteLoginRedirect, repostFields);
            DumpRequest(finalDownload);
            resp = await finalDownload.GetResponseAsync();
            DumpResponseInfo(resp);
            Assert.AreEqual(url, resp.ResponseUri.OriginalString, "Redirect to where we wanted to go!");
            await ExtractHTMLTitleInfo(resp);

#if false
            using (var rdr = new StreamReader(resp.GetResponseStream()))
            {
                var text = await rdr.ReadToEndAsync();
                Console.WriteLine(text);
            }
#endif
        }

        /// <summary>
        /// There is one form in the data. Grab all the data from it.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task<FormInfo> GrabFormData(Stream stream)
        {
            using (var rdr = new StreamReader(stream))
            {
                return ExtractFormInfo(await rdr.ReadToEndAsync());
            }
        }

        /// <summary>
        /// This is meant to be a desktop version of the code to log-in to the CERN CDS site.
        /// </summary>
        /// <remarks>
        /// Can we just get headers in some places in case the resource is large (like several megs)?
        /// </remarks>
        [TestMethod]
        public async Task LoginToCDSWithCERT()
        {
            // Config

            string url = "https://cds.cern.ch/record/1512932?ln=en";
            var cert = FindCert();

            // Just to make sure, even though this is a test.

            var u = new Uri(url); // Fails if the url is badly formed.
            Assert.IsNotNull(cert);

            // Step one, access the protected page. We get back a login-in URL
            // The CERT must be attached here or the request will fail.

            var initialRequest = await CreateRequest(u);
            initialRequest.ClientCertificates.Add(cert);
            DumpRequest(initialRequest);
            var resp = await initialRequest.GetResponseAsync();
            DumpResponseInfo(resp);

            Assert.IsTrue(resp.ResponseUri.AbsolutePath.StartsWith("/adfs/ls/"), string.Format("Didn't see the login redirect - this url doesn't require auth? - response uri '{0}'", url));

#if false
            // Step two is very much part of the perl script, but it does not seem to be required at all!
            // Step two: Create a new web request using this redirect, and add the cert to it.
            // The CERT must be attached here, or the request will fail. But after this cookies are good enough to
            // power the access.

            var loginRequest = await CreateRequest(resp.ResponseUri, cert);
            loginRequest.ClientCertificates.Add(cert);
            DumpRequest(loginRequest);

            resp = await loginRequest.GetResponseAsync();
            DumpResponseInfo(resp);
#endif

            var parsedData = await ParseReply(resp);
            Uri homeSiteLoginRedirect = parsedData.Item1;
            var repostFields = parsedData.Item2;

            // Step three: request the login redirect
            // NOTE: I've gotten "bad gateway" sometimes with this request - but it works
            // the next time we try. CERN's SSO isn't known for being reliable, so we may
            // have to account for that!
            var loginHomeRedirect = await CreateRequest(homeSiteLoginRedirect, repostFields);
            DumpRequest(loginHomeRedirect);
            resp = await loginHomeRedirect.GetResponseAsync();
            DumpResponseInfo(resp);
            Assert.AreEqual(url, resp.ResponseUri.OriginalString, "Redirect to where we wanted to go!");
            await ExtractHTMLTitleInfo(resp);

#if false
            // Step 4: the request to the original resource to see if it worked (or not).
            // This isn't needed unless the person wants to test - the above should have done a redirect
            // automatically to the actual resource we want, and so should contain everything.
            var finalRequest = await CreateRequest(u, cert);
            DumpRequest(finalRequest);
            resp = await finalRequest.GetResponseAsync();
            DumpResponseInfo(resp);
            await ExtractHTMLTitleInfo(resp);
#endif
        }

        /// <summary>
        /// Extract title info from the html
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        private static async Task ExtractHTMLTitleInfo(WebResponse resp)
        {
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
        /// Utility routine to parse the HTML form that comes back.
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        private async Task<Tuple<Uri, IDictionary<string, string>>> ParseReply(WebResponse resp)
        {
            var repostFields = new Dictionary<string, string>();
            using (var rdr = new StreamReader(resp.GetResponseStream()))
            {
                var text = await rdr.ReadToEndAsync();
                Assert.IsFalse(text.Contains("HTTP Error 401.2 - Unauthorized"), "unauth error came back.");

                var info = ExtractFormInfo(text);
                return Tuple.Create(info.Action, info.RepostFields);
            }
        }

        /// <summary>
        /// Helper class for parsing and extracting info from a form.
        /// </summary>
        internal class FormInfo
        {
            public Uri Action { get; set; }
            public IDictionary<string, string> RepostFields;
        }

        /// <summary>
        /// Extract form data
        /// </summary>
        /// <param name="homeSiteLoginRedirect"></param>
        /// <param name="repostFields"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static FormInfo ExtractFormInfo(string text)
        {
            var result = new FormInfo();

            // This should contain a redirect that is deep in a http form. We need to parse that out.
            var doc = new HtmlDocument();
            doc.LoadHtml(text);
            var forms = doc.DocumentNode.SelectNodes("//form").ToArray();
            Assert.AreEqual(1, forms.Length, "Should be only one form back");
            var form = forms[0];

            // The action is something that came a long in the stream, so it will have been escaped. So,
            // decode that. it could also be relative, so we have to deal with that too.
            var actionUriText = WebUtility.HtmlDecode(form.Attributes["action"].Value);
            result.Action = new Uri(actionUriText, actionUriText.StartsWith("http") ? UriKind.Absolute : UriKind.Relative);

            // Now more through all the form names.
            result.RepostFields = new Dictionary<string, string>();
            foreach (var inputs in doc.DocumentNode.SelectNodes("//input"))
            {
                var ftype = inputs.Attributes["type"].Value;
                if (ftype == "hidden")
                {
                    var name = WebUtility.HtmlDecode(inputs.Attributes["name"].Value);
                    var value = WebUtility.HtmlDecode(inputs.Attributes["value"].Value);
                    Console.WriteLine("  Form data found: {0} with value {1}", name, "<temp>");
                    result.RepostFields[name] = value;
                }
                else if (ftype == "password" || ftype == "text" || ftype == "submit")
                {
                    if (inputs.Attributes.Contains("name"))
                    {
                        var name = inputs.Attributes["name"].Value;
                        result.RepostFields[name] = "";
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Dump details about the request
        /// </summary>
        /// <param name="initialRequest"></param>
        private void DumpRequest(HttpWebRequest initialRequest)
        {
            Console.WriteLine("Requesting {0}", initialRequest.RequestUri.OriginalString);
            Console.WriteLine("  User-Agent: {0}", initialRequest.UserAgent);
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

        /// <summary>
        /// Track cookies for all our requests - necessary b.c. that is how the
        /// web knows what we want.
        /// </summary>
        private static CookieContainer gCookies = new CookieContainer();

        /// <summary>
        /// Generate prep stuff for our web request
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        private async Task<HttpWebRequest> CreateRequest(Uri u, IDictionary<string, string> postData = null)
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
                await AddFormData(postData, h);
            }

            return h;
        }

        /// <summary>
        /// Generate prep stuff for our web request
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        private async Task<HttpWebRequest> CreateRequestUP(Uri u, IDictionary<string, string> postData = null)
        {
            var h = WebRequest.CreateHttp(u);

            // We don't use a user-agent string when going after a username/password form.
            //h.UserAgent = "curl-sso-certificate/0.5.1(Mozilla)";
            h.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.63 Safari/537.36";

            // Cache cookies going back and forth, with the sso requires.
            h.CookieContainer = gCookies;

            // If there is post data, add it
            if (postData != null)
            {
                await AddFormData(postData, h);
            }

            return h;
        }

        /// <summary>
        /// Properly encode form data for the html request, and switch it to be a post.
        /// </summary>
        /// <param name="postData"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private static async Task AddFormData(IDictionary<string, string> postData, HttpWebRequest h)
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
                var k = Uri.EscapeDataString(item.Key);
                var v = Uri.EscapeDataString(item.Value);

                bld.AppendFormat("{0}={1}", k, v);
                Console.WriteLine("  Form Data {0} => {1}", k, v.Length > 1024 ? v.Substring(0, 1024) : v);
            }
            Byte[] PostBuffer = System.Text.Encoding.UTF8.GetBytes(bld.ToString());
            using (var wrtr = await h.GetRequestStreamAsync())
            {
                await wrtr.WriteAsync(PostBuffer, 0, PostBuffer.Length);
                wrtr.Close();
            }
        }

        /// <summary>
        /// Dump useful debugging info.
        /// </summary>
        /// <param name="resp"></param>
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

        /// <summary>
        /// Fetch the credentials from the generic store for "cern.ch" - these must be here
        /// or this test will totally fail!
        /// </summary>
        /// <returns></returns>
        private static Tuple<string, string> LookupUserPass()
        {
            Credential cred;
            if (!NativeMethods.CredRead("cern.ch", CRED_TYPE.GENERIC, 0, out cred))
            {
                Console.WriteLine("Error getting credentials");
                Console.WriteLine("Use the credential control panel, create a generic credential for windows domains for cern.ch with username and password");
                throw new InvalidOperationException();
            }

            string password;
            using (var m = new MemoryStream(cred.CredentialBlob, false))
            using (var sr = new StreamReader(m, System.Text.Encoding.Unicode))
            {
                password = sr.ReadToEnd();
            }

            return Tuple.Create(cred.UserName, password);
        }
    }
}
