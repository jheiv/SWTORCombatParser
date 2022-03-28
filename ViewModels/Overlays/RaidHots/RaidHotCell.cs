﻿using SWTORCombatParser.Model.Timers;
using SWTORCombatParser.ViewModels.Timers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWTORCombatParser.ViewModels.Overlays.RaidHots
{
    public class RaidHotCell
    {
        public double RowHeight { get; set; }
        public double ColumnWidth { get; set; }
        public string Name { get; set; }
        public RaidHotCell()
        {
            TimerNotifier.NewTimerTriggered += CheckForRaidHOT;
        }
        public ObservableCollection<TimerInstanceViewModel> RaidHotsOnPlayer { get; set; } = new ObservableCollection<TimerInstanceViewModel>();
        private void CheckForRaidHOT(TimerInstanceViewModel obj)
        {
            if (obj.SourceTimer.Effect == "Rejuvenate")
            {
                RaidHotsOnPlayer.Add(obj);
                obj.TimerExpired += RemoveFromList;
            }
        }

        private void RemoveFromList(TimerInstanceViewModel obj)
        {
            RaidHotsOnPlayer.Remove(obj);
        }
    }
}
