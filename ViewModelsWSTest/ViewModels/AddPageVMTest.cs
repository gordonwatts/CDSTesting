using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Threading.Tasks;
using ViewModels.Data;
using ViewModels.ViewModels;
using ViewModels.Web;

namespace ViewModelsWSTest.ViewModels
{
    [TestClass]
    public class AddPageVMTest
    {
        [TestMethod]
        public async Task TestSimpleSearch()
        {
            var searcher = new dummySearch();
            var vm = new AddPageVM(searcher);

            Assert.IsNull(vm.Title, "title");
            Assert.IsNull(vm.Abstract, "abstract");

            vm.CDSLookupString = "1234";
            await Task.Delay(5000);
            Assert.AreEqual("title", vm.Title, "set title");
            Assert.AreEqual("abstract", vm.Abstract, "set abstract");
        }

        /// <summary>
        /// Dummy (fast) search class.
        /// </summary>
        class dummySearch : ICDSSearch
        {
            public async Task<PaperData> GetPaperData(string searchinfo)
            {
                return new PaperData() { Title = "title", Abstract = "abstract" };
            }
        }

    }
}
