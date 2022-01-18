﻿using SWTORCombatParser.DataStructures;
using SWTORCombatParser.Model.CombatParsing;
using SWTORCombatParser.Model.Timers;
using SWTORCombatParser.Views.Timers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SWTORCombatParser.ViewModels.Timers
{
    public class TimersWindowViewModel:INotifyPropertyChanged
    {
        private string _currentPlayer;
        private TimersWindow _timerWindow;
        private bool _timersEnabled;

        public event Action CloseRequested = delegate { };
        public event Action<bool> OnLocking = delegate { };
        public event Action<string> OnCharacterDetected = delegate { };
        public event PropertyChangedEventHandler PropertyChanged;

        public bool OverlaysMoveable { get; set; } = true;
        public ObservableCollection<TimerInstanceViewModel> SwtorTimers { get; set; } = new ObservableCollection<TimerInstanceViewModel>();
        public string TimerTitle { get; set; }
        public TimersWindowViewModel()
        {
            CombatLogStreamer.HistoricalLogsFinished += EnableTimers;
            CombatLogStreamer.CombatUpdated += NewLogs;
            CombatLogStreamer.NewLineStreamed += NewLogInANDOutOfCombat;
            _timerWindow = new TimersWindow(this);
        }
        public void ShowTimers()
        {
            _timerWindow.Show();        
        }
        public void HideTimers()
        {
            _timerWindow.Hide();
        }
        public void SetPlayer(string player, SWTORClass swtorclass)
        {
            _currentPlayer = player + " " + swtorclass.Discipline;
            TimerTitle = _currentPlayer + " Timers";
            OnPropertyChanged("TimerTitle");
            SwtorTimers = new ObservableCollection<TimerInstanceViewModel>();
            _timerWindow.SetPlayer(_currentPlayer);
            RefreshTimers();
            App.Current.Dispatcher.Invoke(() => {
                var defaultTimersInfo = DefaultTimersManager.GetDefaults(_currentPlayer);
                _timerWindow.Top = defaultTimersInfo.Position.Y;
                _timerWindow.Left = defaultTimersInfo.Position.X;
                _timerWindow.Width = defaultTimersInfo.WidtHHeight.X;
                _timerWindow.Height = defaultTimersInfo.WidtHHeight.Y;

                ShowTimers();
            });

        }
        public void RefreshTimers()
        {
            if (string.IsNullOrEmpty(_currentPlayer))
                return;
            var defaultTimersInfo = DefaultTimersManager.GetDefaults(_currentPlayer);
            var sharedTimers = DefaultTimersManager.GetDefaults("Shared").Timers;
            var timers = defaultTimersInfo.Timers;
            var allTimers = timers.Concat(sharedTimers);
            var timerInstances = allTimers.Select(t => new TimerInstanceViewModel(t) { IsEnabled = t.IsEnabled, TrackOutsideOfCombat = t.TrackOutsideOfCombat}).ToList();
            foreach(var timerInstance in timerInstances)
            {
                if (!string.IsNullOrEmpty(timerInstance.ExperiationTimerId))
                {
                    var trigger = timerInstances.First(t => t.SourceTimer.Id == timerInstance.ExperiationTimerId);
                    timerInstance.ExpirationTimer = trigger;
                }
            }
            timerInstances.ForEach(t => t.TimerTriggered += OrderTimers);
            SwtorTimers = new ObservableCollection<TimerInstanceViewModel>(timerInstances);
            OnPropertyChanged("SwtorTimers");
        }
        private void NewLogInANDOutOfCombat(ParsedLogEntry log)
        {
            foreach (var timer in SwtorTimers.Where(t => t.TrackOutsideOfCombat))
            {
                timer.CheckForTrigger(log, DateTime.Now);
            }
        }
        private void NewLogs(CombatStatusUpdate obj)
        {
            if(obj.Type == UpdateType.Start)
            {
                UncancellBeforeCombat();
            }
            if(obj.Type == UpdateType.Stop)
            {
                CancelAfterCombat();
            }
            if (obj.Logs == null || !_timersEnabled || obj.Type == UpdateType.Stop)
                return;
            var logs = obj.Logs;
            foreach (var log in logs)
            {
                foreach (var timer in SwtorTimers.Where(t=>!t.TrackOutsideOfCombat))
                {
                    timer.CheckForTrigger(log, obj.CombatStartTime);
                }
            }
        }
        private void UncancellBeforeCombat()
        {
            foreach (var timer in SwtorTimers)
            {
                timer.UnCancel();
            }
        }
        private void CancelAfterCombat()
        {
            foreach (var timer in SwtorTimers)
            {
                timer.Cancel();
            }
        }
        private void OrderTimers()
        {
            SwtorTimers = new ObservableCollection<TimerInstanceViewModel>(SwtorTimers.OrderBy(t => t.TimerValue));
            OnPropertyChanged("SwtorTimers");
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private void EnableTimers()
        {
            _timersEnabled = true;
        }
        internal void UpdateLock(bool value)
        {
            OverlaysMoveable = value;
            OnLocking(value);
        }

        internal void EnabledChangedForTimer(bool isEnabled, string id)
        {
            var timerToUpdate = SwtorTimers.FirstOrDefault(t => t.SourceTimer.Id == id);
            if (timerToUpdate == null)
                return;
            timerToUpdate.IsEnabled = isEnabled;
        }
    }
}
