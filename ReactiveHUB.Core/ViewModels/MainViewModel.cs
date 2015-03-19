// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MainViewModel.cs" company="Zühlke Engineering GmbH">
// //   Zühlke Engineering GmbH
// // </copyright>
// // <summary>
// //   MainViewModel.cs
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------
namespace ProjectTemplate.ViewModels
{
    using ReactiveUI;

    public class MainViewModel : ReactiveObject, IScreen
    {
        protected MainViewModel(IRoutingState router)
        {
            this.Router = router ?? new RoutingState();

            this.Router.Navigate.Execute(new MessagesViewModel());
        }

        public IRoutingState Router { get; private set; }
    }
}