// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageItemViewModel.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the MessageItemViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ProjectTemplate.ViewModels
{
    public class MessageItemViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<string> messageChange;

        public MessageItemViewModel()
        {
            
        }

        public string Message
        {
            get { return messageChange.Value; }
        }
    }
}
