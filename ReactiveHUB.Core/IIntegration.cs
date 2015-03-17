// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIntegration.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the IIntegration type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate
{
    using System;
    using System.Reactive;

    using ProjectTemplate.Models;

    public interface IIntegration
    {
        IObservable<Message> IncomingMessages();

        /*
        // How to best send messages?
        // 1) Send one message and return an observable for the success of this operation
        IObservable<Unit> SendMessage(Message message); 
        
        // 2) Messages to send are modeled as observer. No feedback is provided about success
        IObserver<Message> MessageSender();

        // 3) Messages to send are given the integration as observable. No feedback about success
        void SetOutgoingMessages(IObservable<Message> messages);

        // I personally prefer a way where we get the result of the operation somehow, but actually no of the 3 makes me happy.
         */
    }
}