﻿using SWTORCombatParser.DataStructures.RaidInfos;
using SWTORCombatParser.Model.Alerts;
using SWTORCombatParser.Model.CloudRaiding;
using SWTORCombatParser.Model.CombatParsing;
using SWTORCombatParser.Model.LogParsing;
using SWTORCombatParser.resources;
using SWTORCombatParser.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SWTORCombatParser
{
    public class CombatLogStreamer
    {
        public static event Action<CombatStatusUpdate> CombatUpdated = delegate { };
        public event Action<string> NewSoftwareLog = delegate { };
        public static event Action HistoricalLogsFinished = delegate { };
        public event Action<Entity> LocalPlayerIdentified = delegate { };

        private bool _isInCombat = false;
        private long _numberOfEntries;
        private long _newNumberOfEntries;
        private string _logToMonitor; 
        private bool _monitorLog;
        private DateTime _lastUpdateTime;
        private List<ParsedLogEntry> _currentFrameData = new List<ParsedLogEntry>();
        private List<ParsedLogEntry> _currentCombatData = new List<ParsedLogEntry>();
        private DateTime _currentCombatStartTime;

        public void MonitorLog(string logToMonitor)
        {
            Task.Run(() =>
            {
                ResetMonitoring();
                _logToMonitor = logToMonitor;
                ParseExisitingLogs();
                _monitorLog = true;
                PollForUpdates();
            });
        }
        public void ParseCompleteLog(string log)
        {
            ResetMonitoring();
            _logToMonitor = log;
            Task.Run(() =>
            {
                ParseExisitingLogs();
            });
        }

        private void ParseExisitingLogs()
        {
            var currentLogs = CombatLogParser.ParseAllLines(CombatLogLoader.LoadSpecificLog(_logToMonitor));
            _numberOfEntries = currentLogs.Count;
            _newNumberOfEntries = _numberOfEntries;
            ParseHistoricalLog(currentLogs);
        }

        public void StopMonitoring()
        {
            _monitorLog = false;
            EndCombat();
            _currentFrameData.Clear();
        }
        private void ResetMonitoring()
        {
            _newNumberOfEntries = 0;
            _numberOfEntries = 0;
            _currentCombatStartTime = DateTime.MinValue;
            _lastUpdateTime = DateTime.MinValue;
        }

        private void PollForUpdates()
        {
            Task.Run(() => {
                while (_monitorLog)
                {
                    GenerateNewFrame();
                    Thread.Sleep(250);
                }
            });
        }
        private void GenerateNewFrame()
        {
            if (!CheckIfStale())
                return;
            ParseLogFile();
        }
        internal void ParseLogFile()
        {
            _currentFrameData = new List<ParsedLogEntry>();
            using (var fs = new FileStream(_logToMonitor, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF7))
            {
                var allLogEntries = sr.ReadToEnd().Split('\n');
                _newNumberOfEntries = allLogEntries.Where(s=>!string.IsNullOrEmpty(s)).Count();
                if (_newNumberOfEntries <= _numberOfEntries)
                    return;

                for (var line = _numberOfEntries; line < allLogEntries.Length; line++)
                {
                    ProcessNewLine(allLogEntries[line], line, Path.GetFileName(_logToMonitor));
                }

                _numberOfEntries = _newNumberOfEntries;
                if (!_isInCombat)
                    return; 
                CombatTimestampRectifier.RectifyTimeStamps(_currentFrameData);
                var updateMessage = new CombatStatusUpdate { Type = UpdateType.Update, Logs = _currentFrameData, CombatStartTime = _currentCombatStartTime };
                CombatUpdated(updateMessage);
            }
        }
        private void ParseHistoricalLog(List<ParsedLogEntry> logs)
        {
            _currentCombatData.Clear();
            for (var l = 0; l < logs.Count;l++)
            {
                if (logs[l].Source.IsLocalPlayer)
                    LocalPlayerIdentified(logs[l].Source);
                CheckForCombatState(l, logs[l], false);
                if (_isInCombat)
                {
                    _currentCombatData.Add(logs[l]);
                }
            }
            HistoricalLogsFinished();
        }
        private bool CheckIfStale()
        {
            var mostRecentFile = CombatLogLoader.GetMostRecentLogPath();
            if (mostRecentFile != _logToMonitor)
            {
                _logToMonitor = mostRecentFile;
                ResetMonitoring();
                return true;
            }
            var fileInfo = new FileInfo(_logToMonitor);
            if (fileInfo.LastWriteTime == _lastUpdateTime)
                return false;
            _lastUpdateTime = fileInfo.LastWriteTime;
            return true;
        }
        private void ProcessNewLine(string line,long lineIndex,string logName)
        {
            var parsedLine = CombatLogParser.ParseLine(line,lineIndex);

            if (parsedLine.Error == ErrorType.IncompleteLine)
            {
                return;
            }
            if (parsedLine.Source.IsLocalPlayer)
                LocalPlayerIdentified(parsedLine.Source);
            parsedLine.LogName = Path.GetFileName(logName);
            CheckForCombatState(lineIndex, parsedLine);
            if (_isInCombat)
            {
                _currentFrameData.Add(parsedLine);
                _currentCombatData.Add(parsedLine);
            }
        }
        
        private void CheckForCombatState(long lineIndex, ParsedLogEntry parsedLine, bool shouldUpdateOnNewCombat = true)
        {
            var currentCombatState = CombatDetector.CheckForCombatState(parsedLine);
            if(currentCombatState == CombatState.ExitedByEntering)
            {
                EndCombat();
                EnterCombat(parsedLine, shouldUpdateOnNewCombat);
            }
            if (currentCombatState == CombatState.EnteredCombat)
            {
                EnterCombat(parsedLine, shouldUpdateOnNewCombat);
            }
            if(currentCombatState == CombatState.ExitedCombat)
            {
                EndCombat();
            }
        }
        private void EnterCombat(ParsedLogEntry parsedLine, bool shouldUpdateOnNewCombat)
        {
            _currentFrameData.Clear();
            _currentCombatData.Clear();
            _isInCombat = true;
            _currentCombatStartTime = parsedLine.TimeStamp;
            _currentCombatData.Add(parsedLine);
            _currentFrameData.Add(parsedLine);
            var updateMessage = new CombatStatusUpdate { Type = UpdateType.Start, CombatStartTime = _currentCombatStartTime, CombatLocation = CombatLogStateBuilder.CurrentState.GetEncounterActiveAtTime(parsedLine.TimeStamp).Name };
            if (shouldUpdateOnNewCombat)
                CombatUpdated(updateMessage);
        }
        private void EndCombat()
        {
            CombatTimestampRectifier.RectifyTimeStamps(_currentFrameData);
            CombatTimestampRectifier.RectifyTimeStamps(_currentCombatData);
            _isInCombat = false;

            if (string.IsNullOrEmpty(_logToMonitor))
                return;
            var updateMessage = new CombatStatusUpdate { Type = UpdateType.Stop, Logs = _currentCombatData, CombatStartTime = _currentCombatStartTime };
            CombatUpdated(updateMessage);
        }
    }
}
