// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Tweet.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the Tweet type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Newtonsoft.Json;

    using ReactiveHub.Contracts.Models;

    public class Tweet : Message
    {
        internal Tweet()
        {
        }

        public long Id { get; set; }

        public static Tweet FromJsonString(string inputJson)
        {
            return Tweet.FromProxy(JsonConvert.DeserializeObject<Proxy>(inputJson));
        }

        internal static Tweet FromProxy(Proxy p)
        {
            return new Tweet
                       {
                           Id = p.id,
                           Text = p.text,
                           Sender = TwitterUser.FromProxy(p.user),
                           TimeStamp =
                               DateTime.ParseExact(
                                   p.created_at,
                                   Constants.TwitterDateFormat,
                                   CultureInfo.InvariantCulture)
                       };
        }

        internal struct Proxy
        {
            public long id;

            public string text;

            public TwitterUser.Proxy user;

            public string created_at;
        }
    }
}