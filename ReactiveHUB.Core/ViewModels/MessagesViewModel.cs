// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessagesViewModel.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the MessagesViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate.ViewModels
{
    using ProjectTemplate.Models;

    using ReactiveUI;

    public class MessagesViewModel : ReactiveObject
    {
        public MessagesViewModel()
        {
            // MessageResults = new ReactiveList<MessageItemViewModel>();
        }

        public ReactiveList<object> MessageResults { get; set; } 

        public IIntegration<Message> MessageService { get; private set; } 
    }
}
