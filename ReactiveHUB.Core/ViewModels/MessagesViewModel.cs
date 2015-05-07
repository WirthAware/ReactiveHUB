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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using ReactiveHub.Contracts;
    using ReactiveHub.Contracts.Models;

    using ReactiveUI;

    public class MessagesViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly Dictionary<IIntegration, IDisposable> integrationSubscriptions;

        public MessagesViewModel(IScreen hostScreen)
        {
            this.HostScreen = hostScreen;
            this.UrlPathSegment = "messagelist";

            var composeMessage = new ReactiveCommand();
            composeMessage.Select(x => new DialogViewModel(hostScreen))
                .Subscribe(hostScreen.Router.Navigate.Execute);

            this.ComposeMessageCommand = composeMessage;

            this.Messages = new ReactiveList<MessageItemViewModel>();
            this.MessageService = new ReactiveList<IIntegration>();
            this.integrationSubscriptions = new Dictionary<IIntegration, IDisposable>();

            this.MessageService.ItemsAdded.Subscribe(this.HandleAddedMessageService);
            this.MessageService.ItemsRemoved.Subscribe(this.HandleRemovedMessageService);
            this.MessageService.ShouldReset.Subscribe(this.SynchronizeMessageServices);
        }

        public ReactiveList<MessageItemViewModel> Messages { get; set; }

        public ReactiveList<IIntegration> MessageService { get; private set; }

        public ICommand ComposeMessageCommand { get; private set; }

        public string UrlPathSegment { get; private set; }

        public IScreen HostScreen { get; private set; }

        private void HandleAddedMessageService(IIntegration service)
        {
            this.integrationSubscriptions[service] = service.IncomingMessages().ObserveOn(RxApp.MainThreadScheduler).Subscribe(this.AddMessage);
        }

        private void HandleRemovedMessageService(IIntegration service)
        {
            this.integrationSubscriptions[service].Dispose();
            this.integrationSubscriptions.Remove(service);
        }

        private void SynchronizeMessageServices(Unit _)
        {
            var foundIntegrations = integrationSubscriptions.ToDictionary(x => x.Key, x => false);

            foreach (var service in this.MessageService)
            {
                // Handle addition of services previously not in the list
                if (!foundIntegrations.ContainsKey(service))
                {
                    this.HandleAddedMessageService(service);
                }

                // Mark service as present in the new list
                foundIntegrations[service] = true;
            }

            // Now get all services that have not been marked and handle their removal
            foreach (var service in foundIntegrations.Where(x => !x.Value).Select(x => x.Key))
            {
                this.HandleRemovedMessageService(service);
            }
        }

        private void AddMessage(Message m)
        {
            this.Messages.Add(new MessageItemViewModel(m));
        }
    }
}
