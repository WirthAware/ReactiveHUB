//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DialogViewModel.cs" company="Zühlke Engineering GmbH">
//    Zühlke Engineering GmbH
//  </copyright>
//  <summary>
//    DialogViewModel.cs
//  </summary>
//  --------------------------------------------------------------------------------------------------------------------
namespace ProjectTemplate.ViewModels
{
    using System;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using ReactiveUI;

    public class DialogViewModel : ReactiveObject, IRoutableViewModel
    {
        public DialogViewModel(IScreen hostScreen)
        {
            this.HostScreen = hostScreen;
            this.UrlPathSegment = "somedialog";

            this.CloseCommand = hostScreen.Router.NavigateBack;

            this.validationResult = this.ObservableForProperty(x => x.EnteredValue)
                    .Select(change => !string.IsNullOrWhiteSpace(change.Value))
                    .ToProperty(this, x => x.ValidationResult);

            var saveCommand = new ReactiveCommand(this.ObservableForProperty(x => x.EnteredValue)
                    .Select(change => !string.IsNullOrWhiteSpace(change.Value))
                    .StartWith(false));

            saveCommand.Select(x => this.enteredValue).Subscribe(this.SaveValue);

            this.SaveCommand = saveCommand;
        }

        public ICommand CloseCommand { get; private set; }

        private string enteredValue;
        public string EnteredValue
        {
            get
            {
                return this.enteredValue;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.enteredValue, value);
            }
        }

        private readonly ObservableAsPropertyHelper<bool> validationResult;
        public bool ValidationResult
        {
            get
            {
                return this.validationResult.Value;
            }
        }

        private string savedValue;
        public string SavedValue
        {
            get
            {
                return this.savedValue;
            }

            private set
            {
                this.RaiseAndSetIfChanged(ref this.savedValue, value);
            }
        }

        public ICommand SaveCommand { get; private set; }

        public string UrlPathSegment { get; private set; }

        public IScreen HostScreen { get; private set; }

        private void SaveValue(string value)
        {
            this.SavedValue = value;
        }
    }
}