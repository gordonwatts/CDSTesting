using CERNSSO;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NugetTestDesktop
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing out access");

            //var cred = LookupUserPass();
            //WebAccess.LoadUsernamePassword(cred.Item1, cred.Item2);
            var cert = FindCert();
            WebAccess.LoadCertificate(cert);

            var response = WebAccess.GetWebResponse(new Uri("https://cds.cern.ch/record/1512932?ln=en"));
            Console.WriteLine("Title: {0}", ExtractHTMLTitleInfo(response.Result).Result);
        }

        /// <summary>
        /// Extract the title from the response.
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        public static async Task<string> ExtractHTMLTitleInfo(HttpResponseMessage resp)
        {
            var text = await resp.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(text);
            var titleNode = doc.DocumentNode.Descendants("title").FirstOrDefault();
            if (titleNode == null)
                throw new InvalidDataException("No title node found for the web page!");
            return titleNode.InnerHtml;
        }

        /// <summary>
        /// Get the cert now
        /// </summary>
        /// <returns></returns>
        private static X509Certificate2 FindCert()
        {
            var st = new X509Store(StoreName.My);
            st.Open(OpenFlags.ReadOnly);
            try
            {
                var allcerts = st.Certificates.Cast<X509Certificate2>().Where(s => s.SubjectName.Name.Contains("DC=cern") && s.SubjectName.Name.Contains("OU=Users")).ToArray();
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
