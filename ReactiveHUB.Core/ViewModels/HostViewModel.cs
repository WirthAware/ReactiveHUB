//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="HostViewModel.cs" company="Zühlke Engineering GmbH">
//    Zühlke Engineering GmbH
//  </copyright>
//  <summary>
//    HostViewModel.cs
//  </summary>
//  --------------------------------------------------------------------------------------------------------------------
namespace ProjectTemplate.ViewModels
{
    using ProjectTemplate.WebRequests;

    using ReactiveHub.Integration.Twitter;

    using ReactiveUI;

    public class HostViewModel : ReactiveObject, IScreen
    {
        public HostViewModel(IRoutingState router = null)
        {
            this.Router = router ?? new RoutingState();

            var viewModel = new MessagesViewModel(this);
            
            // TODO: Remove hard coded dependency to twitter integration (Issue #7)
            viewModel.MessageService.Add(new TwitterIntegration(new WebRequestService()));

            this.Router.Navigate.Execute(viewModel);
        }

        public IRoutingState Router { get; private set; }
    }
}