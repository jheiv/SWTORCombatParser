﻿using SWTORCombatParser.ViewModels;
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
using System.Windows.Shapes;

namespace SWTORCombatParser.Views
{
    /// <summary>
    /// Interaction logic for MetricColorPickerWindow.xaml
    /// </summary>
    public partial class MetricColorPickerWindow : Window
    {
        public MetricColorPickerWindow(MetricColorPickerViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
