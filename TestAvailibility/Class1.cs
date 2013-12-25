

using System.Net;
namespace TestAvailibility
{
    public class NetworkAccess
    {
        public void Test()
        {
            var h = WebRequest.CreateHttp("http://www.nytimes.com");
            h.CookieContainer = null;
            //h.ClientCertificates.Add(null); Not availible in windows store!

            //var filter = new HttpBaseProtocolFilter();
            var handler = new HttpClient();
        }
    }
}
