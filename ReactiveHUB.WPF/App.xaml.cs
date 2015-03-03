﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Interaction logic for App.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ProjectTemplate.ViewModels;
using ProjectTemplate.Views;
using ReactiveUI;
using Splat;

namespace ProjectTemplate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Locator.CurrentMutable.Register(() => new ColorPickerView(), typeof(IViewFor<ColorPickerViewModel>));
            Locator.CurrentMutable.Register(() => new MessagesView(), typeof(IViewFor<MessagesViewModel>));
        }
    }
}
