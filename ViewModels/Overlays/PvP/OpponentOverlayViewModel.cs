﻿using Newtonsoft.Json;
using SWTORCombatParser.DataStructures;
using SWTORCombatParser.Model.CombatParsing;
using SWTORCombatParser.Model.LogParsing;
using SWTORCombatParser.Model.Overlays;
using SWTORCombatParser.ViewModels.Timers;
using SWTORCombatParser.Views.Overlay.PvP;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SWTORCombatParser.ViewModels.Overlays.PvP
{
    public class OpponentOverlayViewModel:INotifyPropertyChanged
    {
        private bool _isActive = false;
        private OpponentHpOverlay _opponentHPView;
        private OverlayInfo _settings;

        private DispatcherTimer _dTimer;
        private bool _isTriggered;
        private DateTime _lastUpdate;
        private Combat _mostRecentCombat;
        private Dictionary<string, double> _currentHps = new Dictionary<string, double>();
        private Dictionary<string, DateTime> _lastUpdatedPlayer = new Dictionary<string, DateTime>();
        public OpponentOverlayViewModel()
        {
            _dTimer = new DispatcherTimer();
            _opponentHPView = new OpponentHpOverlay(this);
            _opponentHPView.Show();
            _settings = DefaultGlobalOverlays.GetOverlayInfoForType("PvP_HP");
            EncounterTimerTrigger.NonPvpEncounterEntered += OnPvpCombatEnded;
            EncounterTimerTrigger.PvPEncounterEntered += OnPvpCombatStarted;
            CombatLogStreamer.NewLineStreamed += NewLineStreamed;
            CombatIdentifier.NewCombatAvailable += NewCombatInfo;
            SetInitialPosition();
        }



        public List<OpponentHPBarViewModel> OpponentHpBars { get; set; } = new List<OpponentHPBarViewModel>();
        private void OnPvpCombatStarted()
        {
            if (!OverlayEnabled || _isTriggered)
                return;
            _isTriggered = true;
            if (_settings.Acive)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _currentHps = new Dictionary<string, double>();
                    ShowFrame = true;
                    _mostRecentCombat = null;
                    OpponentHpBars.Clear();
                    OnPropertyChanged("ShowFrame");
                    _dTimer.Start();
                    _dTimer.Interval = TimeSpan.FromSeconds(0.1);
                    _dTimer.Tick += CheckForNewState;
                });

            }
        }
        private void OnPvpCombatEnded()
        {
            if (!OverlayEnabled)
                return;

            _isTriggered = false;
            _mostRecentCombat = null;
            OpponentHpBars.Clear();
            _currentHps = new Dictionary<string, double>();
            App.Current.Dispatcher.Invoke(() =>
            {
                ShowFrame = false;
                _dTimer.Stop();
                _dTimer.Tick -= CheckForNewState;
                OnPropertyChanged("ShowFrame");
            });

        }
        public bool OverlayEnabled
        {
            get { return _isActive; }
            set
            {
                DefaultGlobalOverlays.SetActive("PvP_HP", value);
                _isActive = value;
                if (!_isActive)
                {
                    _opponentHPView.Hide();
                }
                else
                {
                    _opponentHPView.Show();
                    if (OverlaysMoveable)
                    {
                        ShowFrame = true;
                        OnPropertyChanged("ShowFrame");
                    }
                    
                }
                OnPropertyChanged();
            }
        }
        public event Action<bool> OnLocking = delegate { };
        public bool OverlaysMoveable { get; set; }
        public bool ShowFrame { get; set; }
        public void LockOverlays()
        {
            OnLocking(true);
            OverlaysMoveable = false;
            ShowFrame = false;
            OnPropertyChanged("ShowFrame");
            OnPropertyChanged("OverlaysMoveable");
        }
        public void UnlockOverlays()
        {
            OnLocking(false);
            OverlaysMoveable = true;
            if (_settings.Acive)
                ShowFrame = true;
            OnPropertyChanged("ShowFrame");
            OnPropertyChanged("OverlaysMoveable");
        }
        private void NewCombatInfo(Combat currentCombat)
        {
            _mostRecentCombat = currentCombat;
            //var participants = currentCombat.CharacterParticipants;
            //var opponents = participants.Where(p => CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(p,currentCombat.StartTime));
            //foreach(var opponent in opponents)
            //{
            //    var lastLogForPlayer = currentCombat.GetLogsInvolvingEntity(opponent).Last();
            //    var hpOfPlayer = lastLogForPlayer.Source == opponent ? lastLogForPlayer.SourceInfo.CurrentHP : lastLogForPlayer.TargetInfo.CurrentHP;
            //    var maxHpOfPlayer = lastLogForPlayer.Source == opponent ? lastLogForPlayer.SourceInfo.MaxHP : lastLogForPlayer.TargetInfo.MaxHP;
            //    _currentHps[opponent.Name] = hpOfPlayer / maxHpOfPlayer;
            //}
        }
        private void NewLineStreamed(ParsedLogEntry newLine)
        {
            if (_mostRecentCombat == null)
                return;
            _lastUpdate = newLine.TimeStamp;
            

            if (newLine.Source == newLine.Target && CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(newLine.Source, newLine.TimeStamp) && newLine.Source.IsCharacter)
            {
                _lastUpdatedPlayer[newLine.Source.Name] = _lastUpdate;
                _currentHps[newLine.Source.Name] = newLine.SourceInfo.CurrentHP / newLine.SourceInfo.MaxHP;
                RemoveOldPlayers();
                return;
            }
            if (CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(newLine.Source, newLine.TimeStamp) && newLine.Source.Name != null && newLine.Source.IsCharacter)
            {
                _lastUpdatedPlayer[newLine.Source.Name] = _lastUpdate;
                _currentHps[newLine.Source.Name] = newLine.SourceInfo.CurrentHP / newLine.SourceInfo.MaxHP;
                RemoveOldPlayers();
                return;
            }
            if (CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(newLine.Target, newLine.TimeStamp) && newLine.Target.Name != null && newLine.Target.IsCharacter)
            {
                _lastUpdatedPlayer[newLine.Target.Name] = _lastUpdate;
                _currentHps[newLine.Target.Name] = newLine.TargetInfo.CurrentHP / newLine.TargetInfo.MaxHP;
                RemoveOldPlayers();
                return;
            }
        }
        private void RemoveOldPlayers()
        {
            if (_currentHps.Count > 8)
            {
                _currentHps.Remove(_currentHps.MinBy(p => _lastUpdatedPlayer[p.Key]).Key);
            }
        }

        private void CheckForNewState(object sender, EventArgs e)
        {
            var sorted = (from entry in _currentHps orderby entry.Key ascending select entry).ToList();
            var bars = new List<OpponentHPBarViewModel>();
            foreach (var opponent in sorted)
            {           
                var newBar = new OpponentHPBarViewModel(opponent.Key) { Value = opponent.Value, InRange = IsInRangeOfLocalPlayer(opponent.Key), IsTargeted = IsCurrentTarget(opponent.Key), Menace = GetMenaceType(opponent.Key) };
                bars.Add(newBar);
            }
            OpponentHpBars = bars;
            OnPropertyChanged("OpponentHpBars");
        }

        private MenaceTypes GetMenaceType(string key)
        {
            if (_mostRecentCombat == null || !_mostRecentCombat.EDPS.Where(kvp => CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(kvp.Key, _mostRecentCombat.StartTime)).Any())
                return MenaceTypes.None;
            var maxDPS = _mostRecentCombat.EDPS.Where(kvp=>CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(kvp.Key, _mostRecentCombat.StartTime)).MaxBy(d => d.Value);
            if (maxDPS.Key.Name == key)
                return MenaceTypes.Dps;
            var maxEHPS = _mostRecentCombat.EHPS.Where(kvp => CombatLogStateBuilder.CurrentState.IsPvpOpponentAtTime(kvp.Key, _mostRecentCombat.StartTime)).MaxBy(d => d.Value);
            if (maxEHPS.Key.Name == key)
                return MenaceTypes.Healer;
            return MenaceTypes.None;
        }

        private bool IsCurrentTarget(string key)
        {
            var target = CombatLogStateBuilder.CurrentState.GetPlayerTargetAtTime(CombatLogStateBuilder.CurrentState.LocalPlayer, _lastUpdate);
            if (target == null)
                return false;
            return target.Name == key;
        }

        private bool IsInRangeOfLocalPlayer(string key)
        {
            var characterPosition = CombatLogStateBuilder.CurrentState.CurrentLocalCharacterPosition;
            if(!CombatLogStateBuilder.CurrentState.CurrentCharacterPositions.Any(c=>c.Key.Name == key))
            {
                return false;
            }
            var targetPositionInfo = CombatLogStateBuilder.CurrentState.CurrentCharacterPositions.First(c => c.Key.Name == key).Value;
            var localClass = CombatLogStateBuilder.CurrentState.GetLocalPlayerClassAtTime(_lastUpdate);
            var requiredRange = localClass.IsRanged ? 30 : 5;
            var distanceBeween = Math.Sqrt(Math.Pow(targetPositionInfo.X - characterPosition.X, 2) + Math.Pow(targetPositionInfo.Y - characterPosition.Y, 2));
            return distanceBeween <= requiredRange;
        }

        private void SetInitialPosition()
        {
            var defaults = DefaultGlobalOverlays.GetOverlayInfoForType("PvP_HP");
            OverlayEnabled = defaults.Acive;
            _opponentHPView.Top = defaults.Position.Y;
            _opponentHPView.Left = defaults.Position.X;
            _opponentHPView.Width = defaults.WidtHHeight.X;
            _opponentHPView.Height = defaults.WidtHHeight.Y;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
