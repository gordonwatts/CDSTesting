using System;
using System.Reactive.Linq;
using ViewModels.Data;

namespace ViewModels.Web
{
    public class RealCDSSearch : ICDSSearch
    {
        public IObservable<PaperData> GetPaperData(string searchinfo)
        {
            return Observable
                .Return(new Data.PaperData() { Abstract = "this is the abstract", Title = "not going to happen" })
                .Delay(TimeSpan.FromSeconds(5));
        }
    }
}
