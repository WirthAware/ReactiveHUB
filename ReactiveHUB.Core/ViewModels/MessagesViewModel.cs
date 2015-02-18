using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ProjectTemplate.Models;
using ReactiveUI;

namespace ProjectTemplate.ViewModels
{
    public class MessagesViewModel : ReactiveObject
    {
        public MessagesViewModel()
        {
            // MessageResults = new ReactiveList<MessageItemViewModel>();
        }

        public ReactiveList<object> MessageResults { get; set; } 

        public IIntegration<Message> MessageService { get; private set; } 
    }
}
