using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using ReactiveUI;
using ReactiveUI.Testing;
using System;
using System.Reactive.Linq;
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

        [TestMethod]
        public void TestSimpleSearchWithTime()
        {
            new TestScheduler().With(sched =>
            {
                var searcher = new dummySearchWithTime();
                var vm = new AddPageVM(searcher);

                vm.CDSLookupString = "1234"; // Fire off a search.

                sched.AdvanceByMs(1000); // Should have started running now. We can test the spinner...

                sched.AdvanceByMs(10 * 1000); // Should have completed the look up by now.

                Assert.AreEqual("title", vm.Title, "set title");
                Assert.AreEqual("abstract", vm.Abstract, "set abstract");
            });
        }

        /// <summary>
        /// Dummy (fast) search class.
        /// </summary>
        class dummySearch : ICDSSearch
        {
            public IObservable<PaperData> GetPaperData(string searchinfo)
            {
                var r = Observable.Return<PaperData>(new PaperData() { Title = "title", Abstract = "abstract" });
                return r;
            }
        }

        class dummySearchWithTime : ICDSSearch
        {
            public IObservable<PaperData> GetPaperData(string searchinfo)
            {
                var r = Observable
                    .Return<PaperData>(new PaperData() { Title = "title", Abstract = "abstract" })
                    .Delay(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler);
                return r;
            }
        }
    }
}
