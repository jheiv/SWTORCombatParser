﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SWTORCombatParser.DataStructures
{
    public enum TimerKeyType
    {
        CombatStart,
        EntityHP,
        AbilityUsed,
        FightDuration,
        EffectGained,
        EffectLost,
        TimerExpired
    }
    public class Timer
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
        public string Source { get; set; } = "";
        public bool SourceIsLocal { get; set; }
        public string Target { get; set; } = "";
        public bool TargetIsLocal { get; set; }
        public double HPPercentage { get; set; }
        public string Name { get; set; }
        public TimerKeyType TriggerType { get; set; }
        public Timer ExperiationTrigger { get; set; }
        public string Ability { get; set; } = "";
        public string Effect { get; set; } = "";
        public bool IsPeriodic { get; set; }
        public bool IsAlert { get; set; }
        public double DurationSec { get; set; }
        public Color TimerColor { get; set; }
        public string SpecificBoss { get; set; }
        public string SpecificEncounter { get; set; }
        public Timer Copy()
        {
            return new Timer()
            {
                Name = Name,
                Source = Source,
                SourceIsLocal = SourceIsLocal,
                Target = Target,
                TargetIsLocal = TargetIsLocal,
                HPPercentage = HPPercentage,
                TriggerType = TriggerType,
                Ability = Ability,
                Effect = Effect,
                IsPeriodic = IsPeriodic,
                IsAlert = IsAlert,
                DurationSec = DurationSec,
                TimerColor = TimerColor,
                SpecificBoss = SpecificBoss,
                SpecificEncounter = SpecificEncounter
            };
        }
    }
}
