// --------------------------------------------------------------------------------------------------------------------
// <copyright file="User.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the User type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Contracts.Models
{
    public abstract class User
    {
        public string DisplayName { get; set; }
    }
}