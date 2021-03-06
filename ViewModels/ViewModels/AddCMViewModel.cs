﻿using Caliburn.Micro;
using Caliburn.Micro.Portable;
using Caliburn.Micro.ReactiveUI;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using ViewModels.Web;
namespace ViewModels.ViewModels
{
    /// <summary>
    /// Do the add page
    /// </summary>
    public class AddCMViewModel : Screen
    {
        /// <summary>
        /// Get/Set the CDS lookup string. We use this to do the search on the website of what
        /// doc the user wants to look at.
        /// </summary>
        public string CDSLookupString
        {
            get { return _CDSLookupString; }
            set { this.NotifyAndSetIfChanged(ref _CDSLookupString, value); }
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
        /// True when a search is on-going (i.e. wait...)
        /// </summary>
        public bool SearchInProgress { get { return _SearchInProgress.Value; } }
        private ObservableAsPropertyHelper<bool> _SearchInProgress;

        /// <summary>
        /// Do the actual search.
        /// </summary>
        private ReactiveCommand ExecuteSearch;

        /// <summary>
        /// Setup the view model with default initial settings.
        /// </summary>
        public AddCMViewModel(ICDSSearch searcher)
        {
            if (searcher == null)
                throw new ArgumentNullException("We must have a valid searcher");

            // This command fires off when we need to execute the command.
            ExecuteSearch = new ReactiveCommand();
            var searchResults = ExecuteSearch.RegisterAsync(x => searcher.GetPaperData(CDSLookupString));

            searchResults
                .Select(x => x.Title)
                .ToPropertyCM(this, x => x.Title, out _TitleOAPH);
            searchResults
                .Select(x => x.Abstract)
                .ToPropertyCM(this, x => x.Abstract, out _AbstractOAPH);

            ExecuteSearch.Select(x => true)
                .Merge(searchResults.Select(x => false))
                .ToPropertyCM(this, x => x.SearchInProgress, out _SearchInProgress, false);

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
