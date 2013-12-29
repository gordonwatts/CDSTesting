using System;
using System.Threading.Tasks;

namespace ViewModels.Web
{
    class RealCDSSearch : ICDSSearch
    {
        public async Task<Data.PaperData> GetPaperData(string searchinfo)
        {
            // the real guy takes some time. :-)
            await Task.Delay(TimeSpan.FromMilliseconds(5000));
            return new Data.PaperData() { Abstract = "this is the abstract", Title = "not going to happen" };
        }
    }
}
