using ReactiveUI;
using System;
using System.Reactive.Linq;
using ViewModels.Web;

namespace ViewModels.ViewModels
{
    /// <summary>
    /// Main view model that implements simple model for adding something.
    /// 1. User types something into the search property (text box)
    /// 2. After some amount of time we fire off a search which takes a few seconds
    /// 3. Some sort of "busy" indicator goes off during this time.
    /// 4. After the result is done the final fields are populated and an "Add" button is enabled.
    /// 5. Hitting the add button does nothing for now, but would in the real app...
    /// </summary>
    public class AddPageVM : ReactiveObject
    {
        /// <summary>
        /// Get/Set the CDS lookup string. We use this to do the search on the website of what
        /// doc the user wants to look at.
        /// </summary>
        public string CDSLookupString
        {
            get { return _CDSLookupString; }
            set { this.RaiseAndSetIfChanged(ref _CDSLookupString, value); }
        }
        string _CDSLookupString;

        /// <summary>
        /// When executed will add the successfully searched for document to the collection.
        /// Only enabled when there has been a successful search.
        /// </summary>
        public ReactiveCommand AddDocToCollection;

        /// <summary>
        /// Readonly property that is the title we've found on CDS after doing the search.
        /// </summary>
        public string Title { get { return _TitleOAPH.Value; } }
        private ObservableAsPropertyHelper<string> _TitleOAPH;

        /// <summary>
        /// Readonly property that is the abstract we've found on CDS after doing the search.
        /// </summary>
        public string Abstract { get { return _AbstractOAPH.Value; } }
        private ObservableAsPropertyHelper<string> _AbstractOAPH;

        /// <summary>
        /// Do the actual search.
        /// </summary>
        private ReactiveCommand ExecuteSearch;

        /// <summary>
        /// Setup the view model with default initial settings.
        /// </summary>
        public AddPageVM(ICDSSearch searcher)
        {
            // This command fires off when we need to execute the command.
            ExecuteSearch = new ReactiveCommand();
            var searchResults = ExecuteSearch.RegisterAsync(x => searcher.GetPaperData(CDSLookupString));

            searchResults
                .Select(x => x.Title)
                .ToProperty(this, x => x.Title, out _TitleOAPH);
            searchResults
                .Select(x => x.Abstract)
                .ToProperty(this, x => x.Abstract, out _AbstractOAPH);

            // When the user has finished typing in some amount of "stuff", we
            // should do the search.
            this.ObservableForProperty(p => p.CDSLookupString)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .Select(x => x.Value)
                .DistinctUntilChanged()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Subscribe(x => ExecuteSearch.Execute(x));
        }
    }
}
