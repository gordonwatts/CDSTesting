using Caliburn.Micro;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace SimpleCMTest.ViewModels
{
    /// <summary>
    /// Very simple reactive object. Changes the text every few seconds.
    /// </summary>
    public class ReactiveViewModel : Screen
    {
        /// <summary>
        /// The button to get me going.
        /// </summary>
        public ReactiveCommand ButtonGoCommand { get; private set; }

        /// <summary>
        /// Setup the thing to go.
        /// </summary>
        public ReactiveViewModel(INavigationService nav)
        {

            var trueAndFalse = Observable
                .Return(false)
                .Delay(TimeSpan.FromMilliseconds(500))
                .Concat(Observable.Return(true)
                    .Delay(TimeSpan.FromMilliseconds(500))
                );
            var goingToChange = Observable.Repeat(trueAndFalse);
            ButtonGoCommand = new ReactiveCommand(goingToChange);
            ButtonGoCommand.Subscribe(_ => nav.NavigateToViewModel(typeof(MyViewDudeViewModel)));

            _nav = nav;
        }

        public INavigationService _nav { get; set; }
    }
}
