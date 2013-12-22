using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DesktopUnitTests
{
    [TestClass]
    public class CDSNetworkTests
    {
        /// <summary>
        /// Very simple web fetch test
        /// </summary>
        [TestMethod]
        public async Task PublicWebFetchWR()
        {
            var docid = "1637926";
            await GetMetaDataTestWR(docid);
        }

        [TestMethod]
        public async Task PublicWebFetchHC()
        {
            var docid = "1637926";
            await GetMetaDataTestHC(docid);
        }

        [TestMethod]
        public async Task PrivateWebFetchWR()
        {
            var docid = "1512932";
            await GetMetaDataTestWR(docid);
        }

        [TestMethod]
        public async Task PrivateWebFetchHC()
        {
            var docid = "1512932";
            await GetMetaDataTestHC(docid);
        }

        private static async Task GetMetaDataTestWR(string docid)
        {
            var reqUri = new Uri(string.Format("https://cds.cern.ch/record/{0}/export/xm?ln=en", docid));
            var wreq = WebRequest.CreateHttp(reqUri);

            var wres = await wreq.GetResponseAsync();
            using (var rdr = new StreamReader(wres.GetResponseStream()))
            {
                var result = await rdr.ReadToEndAsync();
                Debug.WriteLine(result);
                Assert.IsTrue(result.Contains(docid));
            }
        }

        private static async Task GetMetaDataTestHC(string docid)
        {
            var reqUri = string.Format("https://cds.cern.ch/record/{0}/export/xm?ln=en", docid);

            var config = new HttpClientHandler() { ClientCertificateOptions = ClientCertificateOption.Automatic };
            var handler = new HttpClient(config, true);

            var resp = await handler.GetAsync(reqUri);
            var result = await resp.Content.ReadAsStringAsync();
            Debug.WriteLine(result);
            Assert.IsTrue(result.Contains(docid));
        }

    }
}
