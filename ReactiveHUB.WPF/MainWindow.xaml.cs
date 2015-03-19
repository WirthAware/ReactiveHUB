// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate
{
    using ProjectTemplate.ViewModels;
    using ProjectTemplate.WebRequests;

    using ReactiveHub.Integration.Twitter;

    /// <summary>
    /// Interaction logic 
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();

            var viewModel = new MessagesViewModel();
            ViewHost.ViewModel = viewModel;

            viewModel.MessageService.Add(new TwitterIntegration(new WebRequestService()));
        }
    }
}
