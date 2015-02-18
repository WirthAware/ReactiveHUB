using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ProjectTemplate.ViewModels;
using ReactiveUI;

namespace ProjectTemplate.Views
{
    /// <summary>
    /// Interaction logic for MessagesView.xaml
    /// </summary>
    public partial class MessagesView : UserControl, IViewFor<MessagesViewModel>
    {
        public MessagesView()
        {
            InitializeComponent();

            // :TODO: fix the binding 
            // this.Bind(ViewModel, x => x.MessageResults, x => x.MessageBox.Items);
        }

        public MessagesViewModel ViewModel
        {
            get { return (MessagesViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(MessagesViewModel), typeof(MessagesView), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MessagesViewModel)value; }
        }
    }
}
