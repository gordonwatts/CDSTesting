using System.Net;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace LibWindowsStore
{
    public class Class1
    {
        public void test()
        {
            var h = WebRequest.CreateHttp("http://www.nytimes.com");
            var client = new HttpClient();
            var filter = new HttpBaseProtocolFilter();
            filter.ClientCertificate = null; // That shoudl work.
        }
    }
}
