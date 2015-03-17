// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListItem.xaml.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Interaction logic for MessageListItem.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate.Views
{
    using ProjectTemplate.ViewModels;

    using ReactiveUI;

    /// <summary>
    /// Interaction logic
    /// </summary>
    public partial class MessageListItem : IViewFor<MessageItemViewModel>
    {
        public MessageListItem()
        {
            InitializeComponent();

            // TODO: Bind here instead of XAML (Issue #20)
        }

        object IViewFor.ViewModel
        {
            get
            {
                return ViewModel;
            }

            set
            {
                ViewModel = (MessageItemViewModel)value;
            }
        }

        public MessageItemViewModel ViewModel { get; set; }
    }
}
