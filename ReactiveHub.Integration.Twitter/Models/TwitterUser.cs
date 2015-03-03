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

    using ProjectTemplate.Models;

    public class TwitterUser : User
    {
        internal TwitterUser()
        {
        }

        public static TwitterUser FromJsonObject(Dictionary<string, object> dictionary)
        {
            return new TwitterUser
                       {
                           DisplayName = dictionary["screen_name"].ToString()
                       };
        }
    }
}