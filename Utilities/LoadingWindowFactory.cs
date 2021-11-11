﻿using SWTORCombatParser.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SWTORCombatParser.resources
{
    public static class LoadingWindowFactory
    {
        private static Window _loadingWindow;
        private static Window _mainWindow;
        public static void SetMainWindow(Window mainWindow)
        {
            _mainWindow = mainWindow;
        }
        public static void ShowLoading()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainTop = _mainWindow.Top;
                var mainLeft = _mainWindow.Left;
                var mainWidth = _mainWindow.ActualWidth;
                var mainHeight = _mainWindow.ActualHeight;
                (double, double) center = (mainLeft + (mainWidth / 2), mainTop + (mainHeight / 2));

                _loadingWindow = new LoadingSplash();
                _loadingWindow.Top = center.Item2 - 100;
                _loadingWindow.Left = center.Item1 - 300;
                _loadingWindow.Show();
            });

        }
        public static void HideLoading()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if(_loadingWindow != null)
                    _loadingWindow.Close();
            });
        }
    }
}
