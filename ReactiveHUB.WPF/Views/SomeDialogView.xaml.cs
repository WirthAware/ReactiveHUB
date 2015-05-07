namespace ProjectTemplate.Views
{
    using System.Windows;

    using ProjectTemplate.ViewModels;

    using ReactiveUI;

    public partial class SomeDialogView : IViewFor<DialogViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(DialogViewModel), typeof(SomeDialogView), new PropertyMetadata(null));

        public DialogViewModel ViewModel
        {
            get { return (DialogViewModel)this.GetValue(ViewModelProperty); }
            set
            {
                this.SetValue(ViewModelProperty, value);
            }
        }

        object IViewFor.ViewModel
        {
            get { return this.ViewModel; }
            set
            {
                this.ViewModel = (DialogViewModel)value;
            }
        }

        public SomeDialogView()
        {
            this.InitializeComponent();

            this.BindCommand(this.ViewModel, x => x.CloseCommand, x => x.NavigateBackButton);

            this.Bind(this.ViewModel, x => x.EnteredValue, x => x.InputBox.Text);
            this.OneWayBind(this.ViewModel, x => x.SavedValue, x => x.Output.Text);
            this.BindCommand(this.ViewModel, x => x.SaveCommand, x => x.SaveButton);
        }
    }
}
