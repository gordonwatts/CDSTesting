using System.Threading.Tasks;
using ViewModels.Data;

namespace ViewModels.Web
{
    public interface ICDSSearch
    {
        /// <summary>
        /// Do the search for paper data
        /// </summary>
        /// <param name="searchinfo"></param>
        /// <returns></returns>
        Task<PaperData> GetPaperData(string searchinfo);
    }
}
