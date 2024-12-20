﻿using SWTORCombatParser.DataStructures;
using SWTORCombatParser.DataStructures.ClassInfos;
using SWTORCombatParser.Model.Challenge;
using SWTORCombatParser.Model.CloudRaiding;
using SWTORCombatParser.Model.CombatParsing;
using SWTORCombatParser.Model.LogParsing;
using SWTORCombatParser.Model.Overlays;
using SWTORCombatParser.Model.Timers;
using SWTORCombatParser.Utilities;
using SWTORCombatParser.ViewModels.Challenges;
using SWTORCombatParser.ViewModels.Combat_Monitoring;
using SWTORCombatParser.ViewModels.Overlays.AbilityList;
using SWTORCombatParser.ViewModels.Overlays.Notes;
using SWTORCombatParser.ViewModels.Overlays.Personal;
using SWTORCombatParser.ViewModels.Timers;
using SWTORCombatParser.Views.Challenges;
using SWTORCombatParser.Views.Overlay;
using SWTORCombatParser.Views.Timers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SWTORCombatParser.ViewModels.Avalonia_TEMP;

namespace SWTORCombatParser.ViewModels.Overlays
{
    public class OverlayViewModel : INotifyPropertyChanged
    {

        private List<OverlayInstanceViewModel> _currentOverlays = new List<OverlayInstanceViewModel>();
        private Dictionary<string, OverlayInfo> _overlayDefaults = new Dictionary<string, OverlayInfo>();
        private string _currentCharacterRole = Role.DPS.ToString();
        private string _currentCharacterDiscipline = "";
        private bool overlaysLocked = true;
        private LeaderboardType selectedLeaderboardType;
        private TimersCreationViewModel _timersViewModel;
        private ChallengeSetupViewModel _challengesViewModel;
        private OthersOverlaySetupViewModel _otherOverlayViewModel;
        private AbilityListSetupViewModel _abilityListSetup;
        private double maxScalar = 1.5d;
        private double minScalar = 0.1d;
        private double sizeScalar = 1d;
        private string sizeScalarString = "1";
        private bool historicalParseFinished = false;
        private bool usePersonalOverlay;
        private PersonalOverlayViewModel _personalOverlayViewModel;
        private string selectedType = "Damage";
        private bool useDynamicLayout;

        public event Action OverlayLockStateChanged = delegate { };

        public TimersCreationView TimersView { get; set; }
        public ChallengeSetupView ChallengesView { get; set; }
        public OtherOverlaySetupView OthersSetupView { get; set; }

        public ObservableCollection<OverlayOptionViewModel> AvailableDamageOverlays { get; set; } = new ObservableCollection<OverlayOptionViewModel>();
        public ObservableCollection<OverlayOptionViewModel> AvailableHealOverlays { get; set; } = new ObservableCollection<OverlayOptionViewModel>();
        public ObservableCollection<OverlayOptionViewModel> AvailableMitigationOverlays { get; set; } = new ObservableCollection<OverlayOptionViewModel>();
        public ObservableCollection<OverlayOptionViewModel> AvailableGeneralOverlays { get; set; } = new ObservableCollection<OverlayOptionViewModel>();
        public ObservableCollection<UtilityOverlayOptionViewModel> AvailableUtilityOverlays { get; set; } = new ObservableCollection<UtilityOverlayOptionViewModel>();
        public List<LeaderboardType> LeaderboardTypes { get; set; } = new List<LeaderboardType>();
        public LeaderboardType SelectedLeaderboardType
        {
            get => selectedLeaderboardType;
            set
            {
                selectedLeaderboardType = value;
                Leaderboards.UpdateLeaderboardType(selectedLeaderboardType);
            }
        }
        public string SizeScalarString
        {
            get => sizeScalarString; set
            {
                sizeScalarString = value;
                var stringVal = 0d;
                if (double.TryParse(sizeScalarString, out stringVal))
                {
                    SizeScalar = stringVal;
                }
                OnPropertyChanged();
            }
        }
        public double SizeScalar
        {
            get { return sizeScalar; }
            set
            {
                sizeScalar = value;
                if (sizeScalar > maxScalar)
                {
                    SizeScalarString = maxScalar.ToString();
                    return;
                }
                if (sizeScalar < minScalar)
                {
                    SizeScalarString = minScalar.ToString();
                    return;
                }
                _currentOverlays.ForEach(overlay => overlay.SizeScalar = sizeScalar);

                SetOverlaysScale();

                Settings.WriteSetting<double>("overlay_bar_scale", sizeScalar);
                OnPropertyChanged();
            }
        }
        private void SetOverlaysScale()
        {
            _abilityListSetup.SetScalar(sizeScalar);
            _otherOverlayViewModel.SetScalar(sizeScalar);
            _timersViewModel.SetScalar(sizeScalar);
            _challengesViewModel.SetScalar(sizeScalar);
            _personalOverlayViewModel.UpdateScale(sizeScalar);
        }
        public void CombatSeleted(Combat selectedCombat)
        {
            _challengesViewModel.CombatSelected(selectedCombat);

        }
        public void CombatUpdated(Combat combat)
        {
            _challengesViewModel.CombatUpdated(combat);
        }
        public int SelectedOverlayTab
        {
            get => selectedOverlayTab; set
            {
                selectedOverlayTab = value;
                if (selectedOverlayTab == 1)
                {
                    _timersViewModel.RefreshEncounterSelection();
                }
                if (selectedOverlayTab == 2)
                {
                    _challengesViewModel.RefreshEncounterSelection();
                }
            }
        }
        public OverlayViewModel()
        {
            CombatLogStateBuilder.PlayerDiciplineChanged += UpdateOverlaysForClass;
            CombatLogStreamer.HistoricalLogsFinished += FinishHistoricalParse;
            CombatLogStreamer.HistoricalLogsStarted += HistoricalLogsStarted;
            sizeScalar = Settings.ReadSettingOfType<double>("overlay_bar_scale");
            useDynamicLayout = Settings.ReadSettingOfType<bool>("DynamicLayout");
            sizeScalarString = sizeScalar.ToString();
            LeaderboardTypes = EnumUtil.GetValues<LeaderboardType>().ToList();
            SelectedLeaderboardType = LeaderboardSettings.ReadLeaderboardSettings();
            DefaultCharacterOverlays.Init();
            DefaultGlobalOverlays.Init();
            DefaultPersonalOverlaysManager.Init();
            DefaultChallengeManager.Init();
            
            //todo THIS WILL BE GONE once avalonia is embedded
            AvaloniaTimelineBuilder.Init();
            var enumVals = EnumUtil.GetValues<OverlayType>().OrderBy(d => d.ToString());
            foreach (var enumVal in enumVals.Where(e => e != OverlayType.None))
            {
                if (enumVal == OverlayType.DPS || enumVal == OverlayType.Damage || enumVal == OverlayType.BurstDPS || enumVal == OverlayType.FocusDPS || enumVal == OverlayType.NonEDPS || enumVal == OverlayType.RawDamage || enumVal == OverlayType.SingleTargetDPS)
                    AvailableDamageOverlays.Add(new OverlayOptionViewModel() { Type = enumVal });
                if (enumVal == OverlayType.HPS || enumVal == OverlayType.RawHealing || enumVal == OverlayType.EHPS || enumVal == OverlayType.EffectiveHealing || enumVal == OverlayType.BurstEHPS || enumVal == OverlayType.HealReactionTime || enumVal == OverlayType.SingleTargetEHPS || enumVal == OverlayType.HealReactionTimeRatio || enumVal == OverlayType.TankHealReactionTime)
                    AvailableHealOverlays.Add(new OverlayOptionViewModel() { Type = enumVal });
                if (enumVal == OverlayType.Mitigation || enumVal == OverlayType.ShieldAbsorb || enumVal == OverlayType.ProvidedAbsorb || enumVal == OverlayType.DamageTaken || enumVal == OverlayType.DamageAvoided || enumVal == OverlayType.DamageSavedDuringCD)
                    AvailableMitigationOverlays.Add(new OverlayOptionViewModel() { Type = enumVal });
                if (enumVal == OverlayType.APM || enumVal == OverlayType.InterruptCount || enumVal == OverlayType.ThreatPerSecond || enumVal == OverlayType.Threat)
                    AvailableGeneralOverlays.Add(new OverlayOptionViewModel() { Type = enumVal });
            }
            AvailableUtilityOverlays = new ObservableCollection<UtilityOverlayOptionViewModel>
            {
                new UtilityOverlayOptionViewModel{ Name = "Personal Stats", Type = UtilityOverlayType.Personal},
                new UtilityOverlayOptionViewModel{ Name = "Raid HOTS", Type = UtilityOverlayType.RaidHot},
                new UtilityOverlayOptionViewModel{ Name = "Boss HP", Type = UtilityOverlayType.RaidBoss},
                new UtilityOverlayOptionViewModel{ Name = "Challenges", Type = UtilityOverlayType.RaidChallenge},
                new UtilityOverlayOptionViewModel{ Name = "Encounter Timers", Type = UtilityOverlayType.RaidTimer},
                new UtilityOverlayOptionViewModel{ Name = "Discipline Timers", Type = UtilityOverlayType.DisciplineTimer, Enabled = false},
                new UtilityOverlayOptionViewModel{ Name = "Room Hazards", Type = UtilityOverlayType.RoomHazard},
                new UtilityOverlayOptionViewModel{ Name = "Time Trial", Type = UtilityOverlayType.Timeline},
                new UtilityOverlayOptionViewModel{ Name = "PvP Opponent HP", Type = UtilityOverlayType.PvPHP},
                new UtilityOverlayOptionViewModel{ Name = "PvP Mini-map", Type = UtilityOverlayType.PvPMap},
                new UtilityOverlayOptionViewModel{ Name = "Ability List", Type = UtilityOverlayType.AbilityList},
                new UtilityOverlayOptionViewModel{Name= "Raid Notes", Type=UtilityOverlayType.RaidNotes},
            };

            TimersView = new TimersCreationView();
            _timersViewModel = new TimersCreationViewModel();
            TimersView.DataContext = _timersViewModel;

            ChallengesView = new ChallengeSetupView();
            _challengesViewModel = new ChallengeSetupViewModel();
            ChallengesView.DataContext = _challengesViewModel;

            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RaidChallenge).IsSelected = _challengesViewModel.ChallengesEnabled;

            _otherOverlayViewModel = new OthersOverlaySetupViewModel();
            OthersSetupView = new OtherOverlaySetupView();
            OthersSetupView.DataContext = _otherOverlayViewModel;

            _abilityListSetup = new AbilityListSetupViewModel();
            _abilityListSetup.OnEnabledChanged += b => {
                AvailableUtilityOverlays.First(t => t.Type == UtilityOverlayType.AbilityList).IsSelected = b;
            };

            _raidNotesSetup = new RaidNotesSetupViewModel();
            _raidNotesSetup.OnEnabledChanged += b => {
                AvailableUtilityOverlays.First(t => t.Type == UtilityOverlayType.RaidNotes).IsSelected = b;
            };

            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RaidHot).IsSelected = _otherOverlayViewModel._raidHotsConfigViewModel.RaidHotsEnabled;
            _otherOverlayViewModel._raidHotsConfigViewModel.EnabledChanged += e => {
                AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RaidHot).IsSelected = e;
            };
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RaidBoss).IsSelected = _otherOverlayViewModel._bossFrameViewModel.BossFrameEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RaidTimer).IsSelected = _otherOverlayViewModel._bossFrameViewModel.MechPredictionsEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RoomHazard).IsSelected = _otherOverlayViewModel._roomOverlayViewModel.OverlayEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.Timeline).IsSelected = AvaloniaTimelineBuilder.TimelineEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.PvPHP).IsSelected = _otherOverlayViewModel._PvpOverlaysConfigViewModel.OpponentHPEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.PvPMap).IsSelected = _otherOverlayViewModel._PvpOverlaysConfigViewModel.MiniMapEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.AbilityList).IsSelected = _abilityListSetup.AbilityListEnabled;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.RaidNotes).IsSelected = _raidNotesSetup.RaidNotesEnabled;
            _personalOverlayViewModel = new PersonalOverlayViewModel(sizeScalar);
            usePersonalOverlay = _personalOverlayViewModel.Active;
            AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.Personal).IsSelected = usePersonalOverlay;
            _personalOverlayViewModel.ActiveChanged += UpdatePersonalOverlayActive;

            SetOverlaysScale();
            RefreshOverlays();
            HotkeyHandler.OnLockOverlayHotkey += () => OverlaysLocked = !OverlaysLocked;
        }

        private void UpdateOverlaysForClass(Entity character, SWTORClass arg2)
        {
            var nextDiscipline = arg2.Discipline;
            if(nextDiscipline != _currentCharacterDiscipline)
            {
                _currentCharacterDiscipline = nextDiscipline;
                if (CombatMonitorViewModel.IsLiveParseActive())
                {
                    var timerToggle = AvailableUtilityOverlays.First(v => v.Type == UtilityOverlayType.DisciplineTimer);
                    timerToggle.Name = $"{arg2.Discipline}: Timers";
                    timerToggle.Enabled = true;
                    timerToggle.IsSelected = DefaultTimersManager.GetTimersActive(arg2.Discipline);
                }
            }
            var nextRole = arg2.Role.ToString();
            if (_currentCharacterRole == nextRole)
                return;
            _currentCharacterRole = arg2.Role.ToString();
            if (historicalParseFinished)
            {
                RefreshOverlays();
            }
        }

        private void RefreshOverlays()
        {
            ResetOverlays();
            _timersViewModel.TryShow();
            if (!DefaultCharacterOverlays.DoesKeyExist(_currentCharacterRole))
            {
                InitializeRoleBasedOverlays();
            }
            if (UseDynamicLayout)
            {
                SetOverlaysToRole();
            }
            else
            {
                SetOverlaysToCustom();
            }
        }

        private void SetOverlaysToCustom()
        {
            SetPersonalByClass(Role.DPS);
            _overlayDefaults = DefaultCharacterOverlays.GetCharacterDefaults("DPS");

            UpdateOverlays();

        }
        private void SetOverlaysToRole()
        {
            SetPersonalByClass(Enum.Parse<Role>(_currentCharacterRole));
            _overlayDefaults = DefaultCharacterOverlays.GetCharacterDefaults(_currentCharacterRole);

            UpdateOverlays();

        }
        private void InitializeRoleBasedOverlays()
        {
            var mostUsedOverlayLayout = DefaultCharacterOverlays.GetMostUsedLayout();
            if (!string.IsNullOrEmpty(mostUsedOverlayLayout))
            {
                DefaultCharacterOverlays.CopyFromKey(mostUsedOverlayLayout, "DPS");
                DefaultCharacterOverlays.CopyFromKey(mostUsedOverlayLayout, "Healer");
                DefaultCharacterOverlays.CopyFromKey(mostUsedOverlayLayout, "Tank");
            }
            else
            {
                DefaultCharacterOverlays.InitializeCharacterDefaults("DPS");
                DefaultCharacterOverlays.InitializeCharacterDefaults("Healer");
                DefaultCharacterOverlays.InitializeCharacterDefaults("Tank");
            }
        }
        private void SetPersonalByClass(Role role)
        {
            switch (role)
            {
                case Role.DPS:
                    SelectedType = "Damage";
                    break;
                case Role.Healer:
                    SelectedType = "Heals";
                    break;
                case Role.Tank:
                    SelectedType = "Tank";
                    break;
                default:
                case Role.Unknown:
                    SelectedType = "Damage";
                    break;
            }
        }
        private Role GetRoleFromSelectedType(string type)
        {
            switch (type)
            {
                case "Damage":
                    return Role.DPS;
                case "Heals":
                    return Role.Healer;
                case "Tank":
                    return Role.Tank;
                default:
                    return Role.Unknown;
            }
        }

        private void UpdateOverlays()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (_overlayDefaults.Count == 0)
                    return;
                if (_overlayDefaults.First().Value.Locked)
                {
                    OverlaysLocked = true;
                    OnPropertyChanged("OverlaysLocked");
                }
                var enumVals = EnumUtil.GetValues<OverlayType>();
                foreach (var enumVal in enumVals.Where(e => e != OverlayType.None))
                {
                    if (!_overlayDefaults.ContainsKey(enumVal.ToString()))
                        continue;
                    if (_overlayDefaults[enumVal.ToString()].Acive)
                        CreateOverlay(GetType(enumVal), false);
                }
                _currentOverlays.ForEach(o => o.CharacterDetected(_currentCharacterRole));
            });
        }
        private void FinishHistoricalParse(DateTime combatEndTime, bool localPlayerIdentified)
        {
            historicalParseFinished = true;
            if (!localPlayerIdentified)
                return;
            var localPlayer = CombatLogStateBuilder.CurrentState.LocalPlayer;
            var currentDiscipline = CombatLogStateBuilder.CurrentState.GetLocalPlayerClassAtTime(combatEndTime);
            if (localPlayer == null)
                return;
            UpdateOverlaysForClass(localPlayer, currentDiscipline);
        }
        private void HistoricalLogsStarted()
        {
            historicalParseFinished = false;
        }
        public ICommand ToggleUtilityCommand => new CommandHandler(v => ToggleUtility(((UtilityOverlayOptionViewModel)v)));
        private void ToggleUtility(UtilityOverlayOptionViewModel utility)
        {
            utility.IsSelected = !utility.IsSelected;
            switch (utility.Type)
            {
                case UtilityOverlayType.Personal:
                    UsePersonalOverlay = !UsePersonalOverlay; 
                    break;
                case UtilityOverlayType.RaidHot:
                    _otherOverlayViewModel._raidHotsConfigViewModel.RaidHotsEnabled = !_otherOverlayViewModel._raidHotsConfigViewModel.RaidHotsEnabled;
                    break;
                case UtilityOverlayType.RaidBoss:
                    _otherOverlayViewModel._bossFrameViewModel.BossFrameEnabled = !_otherOverlayViewModel._bossFrameViewModel.BossFrameEnabled;
                    break;
                case UtilityOverlayType.RaidTimer:
                    _otherOverlayViewModel._bossFrameViewModel.MechPredictionsEnabled = !_otherOverlayViewModel._bossFrameViewModel.MechPredictionsEnabled;
                    break;
                case UtilityOverlayType.DisciplineTimer:
                    _timersViewModel.DisciplineTimersActive = !_timersViewModel.DisciplineTimersActive;
                    break;
                case UtilityOverlayType.RoomHazard:
                    _otherOverlayViewModel._roomOverlayViewModel.OverlayEnabled = !_otherOverlayViewModel._roomOverlayViewModel.OverlayEnabled;
                    break;
                case UtilityOverlayType.PvPHP:
                    _otherOverlayViewModel._PvpOverlaysConfigViewModel.OpponentHPEnabled = !_otherOverlayViewModel._PvpOverlaysConfigViewModel.OpponentHPEnabled;
                    break;
                case UtilityOverlayType.PvPMap:
                    _otherOverlayViewModel._PvpOverlaysConfigViewModel.MiniMapEnabled = !_otherOverlayViewModel._PvpOverlaysConfigViewModel.MiniMapEnabled;
                    break;
                case UtilityOverlayType.RaidChallenge:
                    _challengesViewModel.ChallengesEnabled = !_challengesViewModel.ChallengesEnabled;
                    break;
                case UtilityOverlayType.AbilityList:
                    _abilityListSetup.AbilityListEnabled = !_abilityListSetup.AbilityListEnabled;
                    break;
                case UtilityOverlayType.RaidNotes:
                    _raidNotesSetup.RaidNotesEnabled = !_raidNotesSetup.RaidNotesEnabled;
                    break;
                case UtilityOverlayType.Timeline:
                    AvaloniaTimelineBuilder.TimelineEnabled = !AvaloniaTimelineBuilder.TimelineEnabled;
                    break;
                default:
                    return;

            }
        }
        public ICommand GenerateOverlay => new CommandHandler(v => CreateOverlay((OverlayOptionViewModel)v, true));

        private void CreateOverlay(OverlayOptionViewModel type, bool canDelete)
        {
            OverlayOptionViewModel overlayType = type;
            if (_currentOverlays.Any(o => o.CreatedType == overlayType.Type))
            {
                if (!canDelete)
                    return;
                var currentOverlay = _currentOverlays.First(o => o.CreatedType == overlayType.Type);
                currentOverlay.RequestClose();
                RemoveOverlay(currentOverlay);
                return;
            }
            overlayType.IsSelected = true;
            var viewModel = new OverlayInstanceViewModel(overlayType.Type);
            DefaultCharacterOverlays.SetActiveStateCharacter(viewModel.Type.ToString(), true, _currentCharacterRole);
            viewModel.OverlayClosed += RemoveOverlay;
            viewModel.OverlaysMoveable = !OverlaysLocked;
            viewModel.SizeScalar = SizeScalar;
            _currentOverlays.Add(viewModel);
            var overlay = new InfoOverlay(viewModel);
            overlay.SetPlayer(_currentCharacterRole);
            var stringType = viewModel.Type.ToString();
            if (_overlayDefaults.ContainsKey(stringType))
            {
                overlay.Top = _overlayDefaults[stringType].Position.Y;
                overlay.Left = _overlayDefaults[stringType].Position.X;
                overlay.Width = _overlayDefaults[stringType].WidtHHeight.X;
                overlay.Height = _overlayDefaults[stringType].WidtHHeight.Y;
            }
            overlay.Show();
            viewModel.Refresh(CombatIdentifier.CurrentCombat);
            if (OverlaysLocked)
                viewModel.LockOverlays();
        }

        private void RemoveOverlay(OverlayInstanceViewModel obj)
        {
            DefaultCharacterOverlays.SetActiveStateCharacter(obj.CreatedType.ToString(), false, _currentCharacterRole);
            _currentOverlays.Remove(obj);
            SetSelected(false, obj.CreatedType);
        }
        public void HideOverlays()
        {
            ResetOverlays();
            _otherOverlayViewModel.HideAll();
        }
        public void ResetOverlays()
        {
            foreach (var overlay in _currentOverlays.ToList())
            {
                SetSelected(false, overlay.CreatedType);
                overlay.RequestClose();
            }
            _timersViewModel.HideTimers();
            _currentOverlays.Clear();
        }
        public bool OverlaysLocked
        {
            get => overlaysLocked;
            set
            {
                overlaysLocked = value;
                _timersViewModel.UpdateLock(value);
                _challengesViewModel.UpdateLock(value);
                _otherOverlayViewModel.UpdateLock(overlaysLocked);
                _personalOverlayViewModel.UpdateLock(overlaysLocked);
                _abilityListSetup.UpdateLock(overlaysLocked);
                _raidNotesSetup.UpdateLock(overlaysLocked);
                ToggleOverlayLock();
                OverlayLockStateChanged();
                if (value)
                {
                    AvaloniaTimelineBuilder.LockOverlay();
                }         
                else
                {
                    AvaloniaTimelineBuilder.UnlockOverlay();
                }
                OnPropertyChanged();
            }
        }
        private void UpdatePersonalOverlayActive(bool obj)
        {
            AvailableUtilityOverlays.First(t=>t.Type == UtilityOverlayType.Personal).IsSelected = obj;
        }
        public List<string> AvailableTypes { get; private set; } = new List<string> { "Damage", "Heals", "Tank" };
        public string SelectedType
        {
            get => selectedType; set
            {
                if (selectedType != value)
                {
                    selectedType = value;
                    _currentCharacterRole = GetRoleFromSelectedType(selectedType).ToString();
                    RefreshOverlays();
                    DefaultPersonalOverlaysManager.SelectNewDefault(selectedType);
                    OnPropertyChanged();
                }


            }
        }

        public bool UsePersonalOverlay
        {
            get => usePersonalOverlay; set
            {
                _personalOverlayViewModel.Active = value;
                usePersonalOverlay = value;
                OnPropertyChanged();
            }
        }
        private string _previousRole;
        private int selectedOverlayTab;
        private RaidNotesSetupViewModel _raidNotesSetup;

        public bool UseDynamicLayout
        {
            get => useDynamicLayout; set
            {
                useDynamicLayout = value;
                if (useDynamicLayout && !string.IsNullOrEmpty(_previousRole))
                {
                    _currentCharacterRole = _previousRole;
                }
                if (!useDynamicLayout)
                {
                    _previousRole = _currentCharacterRole;
                    _currentCharacterRole = "Custom";
                }
                Settings.WriteSetting<bool>("DynamicLayout", useDynamicLayout);
                RefreshOverlays();
                OnPropertyChanged();
            }
        }
        private void SetSelected(bool selected, OverlayType overlay)
        {
            foreach (var overlayOption in AvailableDamageOverlays)
            {
                if (overlayOption.Type == overlay)
                    overlayOption.IsSelected = selected;
            }
            foreach (var overlayOption in AvailableHealOverlays)
            {
                if (overlayOption.Type == overlay)
                    overlayOption.IsSelected = selected;
            }
            foreach (var overlayOption in AvailableMitigationOverlays)
            {
                if (overlayOption.Type == overlay)
                    overlayOption.IsSelected = selected;
            }
            foreach (var overlayOption in AvailableGeneralOverlays)
            {
                if (overlayOption.Type == overlay)
                    overlayOption.IsSelected = selected;
            }
        }
        private OverlayOptionViewModel GetType(OverlayType overlay)
        {
            foreach (var overlayOption in AvailableDamageOverlays)
            {
                if (overlayOption.Type == overlay)
                    return overlayOption;
            }
            foreach (var overlayOption in AvailableHealOverlays)
            {
                if (overlayOption.Type == overlay)
                    return overlayOption;
            }
            foreach (var overlayOption in AvailableMitigationOverlays)
            {
                if (overlayOption.Type == overlay)
                    return overlayOption;
            }
            foreach (var overlayOption in AvailableGeneralOverlays)
            {
                if (overlayOption.Type == overlay)
                    return overlayOption;
            }
            return new OverlayOptionViewModel();
        }
        private void ToggleOverlayLock()
        {
            if (!OverlaysLocked)
                _currentOverlays.ForEach(o => o.UnlockOverlays());
            else
                _currentOverlays.ForEach(o => o.LockOverlays());
            DefaultCharacterOverlays.SetLockedStateCharacter(OverlaysLocked, _currentCharacterRole);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
