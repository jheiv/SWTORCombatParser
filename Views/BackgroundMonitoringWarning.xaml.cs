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

namespace SWTORCombatParser.Views
{
    /// <summary>
    /// Interaction logic for BackgroundMonitoringWarning.xaml
    /// </summary>
    public partial class BackgroundMonitoringWarning : Window
    {
        public BackgroundMonitoringWarning()
        {
            InitializeComponent();
            OkButton.Click += (e, s) => { Close(); };
        }
    }
}
