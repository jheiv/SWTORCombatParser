﻿using SWTORCombatParser.Model.CombatParsing;
using SWTORCombatParser.Model.LogParsing;
using SWTORCombatParser.Model.Overlays;
using SWTORCombatParser.Views.Overlay.BossFrame;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SWTORCombatParser.ViewModels.Overlays.BossFrame
{
    public class BossFrameConfigViewModel : INotifyPropertyChanged
    {
        private bool overlaysMoveable = false;
        private bool bossFrameEnabled;
        private bool dotTrackingEnabled;
        private bool mechPredictionsEnabled;
        private int combatDuration;
        private DispatcherTimer _timer;

        public BrossFrameView View { get; set; }
        public bool OverlaysMoveable
        {
            get => overlaysMoveable; set
            {
                overlaysMoveable = value;
                OnPropertyChanged("ShowFrame");
            }
        }
        public bool BossFrameEnabled
        {
            get => bossFrameEnabled; set
            {
                bossFrameEnabled = value;
                if (!bossFrameEnabled)
                    View.Hide();
                if (bossFrameEnabled)
                    View.Show();
                DefaultBossFrameManager.SetActiveState(bossFrameEnabled);
                OnPropertyChanged();
            }
        }
        public bool DotTrackingEnabled
        {
            get => dotTrackingEnabled; set
            {
                dotTrackingEnabled = value;
                DefaultBossFrameManager.SetDotTracking(dotTrackingEnabled);
                UpdateBossFrameStates();
                OnPropertyChanged();
            }
        }
        public bool MechPredictionsEnabled
        {
            get => mechPredictionsEnabled; set
            {
                mechPredictionsEnabled = value;
                DefaultBossFrameManager.SetPredictMechs(mechPredictionsEnabled);
                UpdateBossFrameStates();
                OnPropertyChanged();
            }
        }
        public bool ShowFrame => BossesDetected.Any() || OverlaysMoveable;
        public event Action CloseRequested = delegate { };
        public event Action<bool> OnLocking = delegate { };
        public event Action<BossFrameConfigViewModel> OverlayClosed = delegate { };
        public ObservableCollection<BossFrameViewModel> BossesDetected { get; set; } = new ObservableCollection<BossFrameViewModel>();
        public int CombatDuration
        {
            get => combatDuration; set
            {
                combatDuration = value;
                OnPropertyChanged();
            }
        }
        private DateTime _lastUpdateTime;
        private double _accurateDuration;
        public BossFrameConfigViewModel()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (e,r) => 
            {
                _accurateDuration += (DateTime.Now - _lastUpdateTime).TotalSeconds;
                CombatDuration = (int)_accurateDuration;
                _lastUpdateTime = DateTime.Now;
            };

            CombatLogStreamer.CombatUpdated += OnNewLog;
            View = new BrossFrameView(this);
            var currentDefaults = DefaultBossFrameManager.GetDefaults();

            View.Left = currentDefaults.Position.X;
            View.Top = currentDefaults.Position.Y;
            View.Width = currentDefaults.WidtHHeight.X;
            View.MainArea.MinHeight = currentDefaults.WidtHHeight.Y;

            BossFrameEnabled = currentDefaults.Acive;
            DotTrackingEnabled = currentDefaults.TrackDOTS;
            MechPredictionsEnabled = currentDefaults.PredictMechs;

            if (currentDefaults.Acive)
                View.Show();
        }
        public void LockOverlays()
        {
            OnLocking(true);
            OverlaysMoveable = false;
            OnPropertyChanged("OverlaysMoveable");
        }
        public void UnlockOverlays()
        {
            OnLocking(false);
            OverlaysMoveable = true;
            OnPropertyChanged("OverlaysMoveable");
        }
        public void OverlayClosing()
        {
            OverlayClosed(this);
        }
        private void UpdateBossFrameStates()
        {
            foreach (var boss in BossesDetected)
            {
                boss.UpdateBossFrameState(DotTrackingEnabled, MechPredictionsEnabled);
            }
        }
        public void OnNewLog(CombatStatusUpdate update)
        {
            if(update.Type == UpdateType.Start)
            {
                StartTimer();
            }
            if (update.Type == UpdateType.Stop)
            {
                HideFrames();
                StopTimer();
            }
            if (update.Logs == null || update.Type == UpdateType.Stop || update.Logs.Count == 0)
                return;
            var logs = update.Logs;
            var currentEncounterBossTargets = CombatLogStateBuilder.CurrentState.GetEncounterActiveAtTime(update.Logs.Last().TimeStamp).BossInfos?.SelectMany(bi => bi.TargetNames).ToList();
            if (currentEncounterBossTargets == null)
                return;
            foreach (var log in logs)
            {
                if (currentEncounterBossTargets.Contains(log.Source.Name) || currentEncounterBossTargets.Contains(log.Target.Name))
                {
                    EntityInfo boss = currentEncounterBossTargets.Contains(log.Source.Name) ? log.SourceInfo : log.TargetInfo;
                    if (!BossesDetected.Any(b => b.CurrentBoss.Name == boss.Entity.Name))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            BossesDetected.Add(new BossFrameViewModel(boss, DotTrackingEnabled, MechPredictionsEnabled));
                            OnPropertyChanged("ShowFrame");
                        });
                    }
                    else
                    {
                        var activeBoss = BossesDetected.First(b => b.CurrentBoss.Name == boss.Entity.Name);
                        activeBoss.LogWithBoss(boss);
                    }
                }
            }
        }

        private void StopTimer()
        {
            _timer.Stop();
            CombatDuration = 0;
            _accurateDuration = 0;
        }

        private void StartTimer()
        {
            _lastUpdateTime = DateTime.Now;
            CombatDuration = 0;
            _accurateDuration = 0;
            _timer.Start();
        }

        private void HideFrames()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                BossesDetected.Clear();
                OnPropertyChanged("ShowFrame");
            });
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
