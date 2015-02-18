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
