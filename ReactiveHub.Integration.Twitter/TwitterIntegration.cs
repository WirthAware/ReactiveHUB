// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwitterIntegration.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   TwitterIntegration.cs
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter
{
    using System;

    using ProjectTemplate;
    using ProjectTemplate.Models;
    using ProjectTemplate.WebRequests;

    public class TwitterIntegration : IIntegration, IDisposable
    {
        // TODO: Replace with token/secret from "ReactiveHUB" Twitter-App (Issue #19)
        private const string AppToken = "RMVtxmVpgUIqG1LzkVFqvSOzt";

        private const string AppSecret = "LwqZLELMTu51AeQcrgc7j2O6P5nzgosXWpXsKbF9VL7Kn1eb8M";

        // TODO: Replace the user token/secret with plugin configuration (Issue #18)
        private const string UserToken = "2585484907-fFlgpVfHbgXBPI0Ct9FaV0BdyAnFDemY0zMR7Ca";

        private const string UserSecret = "JiJfP4tRBicmXd5QVWTBr7OxnhWnVMnM0LmaoaddPb7nP";

        private readonly UserContext context;

        public TwitterIntegration()
        {
            this.context = new UserContext(AppToken, AppSecret, UserToken, UserSecret, new WebRequestService());
        }

        public IObservable<Message> IncomingMessages()
        {
            return context.UserTimeline();
        }

        public void Dispose()
        {
            this.context.Dispose();
        }
    }
}