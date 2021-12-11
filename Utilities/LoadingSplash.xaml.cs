﻿using System;
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

namespace SWTORCombatParser.Utilities
{
    /// <summary>
    /// Interaction logic for LoadingSplash.xaml
    /// </summary>
    public partial class LoadingSplash : Window
    {
        public LoadingSplash()
        {
            InitializeComponent();
        }
        public void SetString(string value)
        {
            Dispatcher.Invoke(() => {
                LoadingText.Text = value;
            });

        }
    }
}
