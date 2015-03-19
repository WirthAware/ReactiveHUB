// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="SomeDialogViewModel.cs" company="Zühlke Engineering GmbH">
// //   Zühlke Engineering GmbH
// // </copyright>
// // <summary>
// //   SomeDialogViewModel.cs
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------
namespace ProjectTemplate.ViewModels
{
    using System.Windows.Input;

    using ReactiveUI;

    public class SomeDialogViewModel
    {
        public SomeDialogViewModel(IScreen host)
        {
            this.CloseCommand = host.Router.NavigateBack;
        }

        public ICommand CloseCommand { get; private set; }
    }
}