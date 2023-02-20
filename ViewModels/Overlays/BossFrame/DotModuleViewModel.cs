﻿using System;
using SWTORCombatParser.Model.Timers;
using SWTORCombatParser.ViewModels.Timers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SWTORCombatParser.DataStructures;

namespace SWTORCombatParser.ViewModels.Overlays.BossFrame
{
    public class DotModuleViewModel : INotifyPropertyChanged
    {
        private EntityInfo _bossInfo;
        private bool isActive;

        public ObservableCollection<TimerInstanceViewModel> ActiveDOTS { get; set; } = new ObservableCollection<TimerInstanceViewModel>();

        public DotModuleViewModel(EntityInfo bossInfo, bool dotTrackingEnabled)
        {
            isActive = dotTrackingEnabled;
            _bossInfo = bossInfo;
            TimerController.TimerExpired += RemoveTimer;
            TimerController.TimerTriggered += AddTimerVisual;
            TimerController.ReorderRequested += ReorderTimers;
        }
        public void SetActive(bool state)
        {
            isActive = state;
        }
        private void RemoveTimer(TimerInstanceViewModel obj, Action<TimerInstanceViewModel> callback)
        {
            App.Current.Dispatcher.Invoke(() => {
                ActiveDOTS.Remove(obj);
            });
            callback(obj);
        }

        private void AddTimerVisual(TimerInstanceViewModel obj, Action<TimerInstanceViewModel> callback)
        {
            if (!isActive)
                return;
            if (obj.TargetAddendem == _bossInfo.Entity.Name && !obj.SourceTimer.IsMechanic && !obj.SourceTimer.IsSubTimer)
            {
                App.Current.Dispatcher.Invoke(() => { ActiveDOTS.Add(obj); });
            }
            callback(obj);
        }

        private void ReorderTimers()
        {
            var currentTimers = ActiveDOTS.OrderBy(v => v.TimerValue);
            ActiveDOTS = new ObservableCollection<TimerInstanceViewModel>(currentTimers);
            OnPropertyChanged("ActiveDOTS");
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
