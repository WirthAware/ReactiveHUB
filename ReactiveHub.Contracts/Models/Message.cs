// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Message.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the Message type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Contracts.Models
{
    using System;
    using System.Collections.Generic;

    public abstract class Message
    {
        protected Message()
        {
            this.Attachments = new IAttachment[] { };
            this.TimeStamp = DateTime.Now;
        }

        public string Text { get; set; }

        public User Sender { get; set; }

        public DateTime TimeStamp { get; set; }

        public IEnumerable<IAttachment> Attachments { get; set; }
    }
}
