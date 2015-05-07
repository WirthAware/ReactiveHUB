// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Interaction logic for App.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate
{
    using ProjectTemplate.ViewModels;
    using ProjectTemplate.Views;

    using ReactiveUI;

    using Splat;

    /// <summary>
    /// Interaction logic
    /// </summary>
    public partial class App 
    {
        public App()
        {
            Locator.CurrentMutable.Register(() => new MessagesView(), typeof(IViewFor<MessagesViewModel>));
            Locator.CurrentMutable.Register(() => new MessageListItem(), typeof(IViewFor<MessageItemViewModel>));
            Locator.CurrentMutable.Register(() => new SomeDialogView(), typeof(IViewFor<DialogViewModel>));
        }
    }
}
