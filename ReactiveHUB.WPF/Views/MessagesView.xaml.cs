// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessagesView.xaml.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Interaction logic for MessagesView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate.Views
{
    using System.Windows;

    using ProjectTemplate.ViewModels;

    using ReactiveUI;

    /// <summary>
    /// Interaction logic 
    /// </summary>
    public partial class MessagesView : IViewFor<MessagesViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(MessagesViewModel), typeof(MessagesView), new PropertyMetadata(null));

        public MessagesView()
        {
            InitializeComponent();

            this.OneWayBind(this.ViewModel, x => x.Messages, x => x.MessageBox.ItemsSource);
            this.BindCommand(this.ViewModel, x => x.ComposeMessageCommand, x => x.AddButton);
        }

        public MessagesViewModel ViewModel
        {
            get { return (MessagesViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MessagesViewModel)value; }
        }
    }
}
