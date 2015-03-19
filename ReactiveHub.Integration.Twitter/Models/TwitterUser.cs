// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwitterUser.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the TwitterUser type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter.Models
{
    using System.Collections.Generic;

    using ReactiveHub.Contracts.Models;

    public class TwitterUser : User
    {
        internal TwitterUser()
        {
        }

        internal static TwitterUser FromProxy(Proxy user)
        {
            return new TwitterUser { DisplayName = user.screen_name };
        }

        internal struct Proxy
        {
            public string screen_name;
        }
    }
}