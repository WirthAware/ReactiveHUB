// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageItemViewModel.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the MessageItemViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate.ViewModels
{
    using System;
    using System.Globalization;
    using System.Reactive.Linq;

    using ProjectTemplate.Models;

    using ReactiveUI;

    public class MessageItemViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<string> message;

        private readonly ObservableAsPropertyHelper<string> timestamp;

        private readonly ObservableAsPropertyHelper<string> sender;

        public MessageItemViewModel(Message model)
        {
            this.message = Observable.Timer(TimeSpan.FromMilliseconds(100)).Select(_ => model.Text).ToProperty(this, x => x.Message);
            this.timestamp =
                Observable.Timer(TimeSpan.FromMilliseconds(100))
                    .Select(_ => DateTime.Now.ToString("R", CultureInfo.CurrentUICulture))
                    .ToProperty(this, x => x.Timestamp);

            this.sender =
                Observable.Timer(TimeSpan.FromMilliseconds(100))
                    .Select(_ => model.Sender.DisplayName)
                    .ToProperty(this, x => x.Sender);

        }

        public string Sender
        {
            get
            {
                return this.sender.Value;
            }
        }

        public string Message
        {
            get
            {
                return this.message.Value;
            }
        }

        public string Timestamp
        {
            get
            {
                return this.timestamp.Value;
            }
        }
    }
}
