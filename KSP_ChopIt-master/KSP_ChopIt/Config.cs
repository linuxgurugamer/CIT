using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CIT_Util.Types;

namespace KSP_ChopIt
{
    public class Config
    {
        // ReSharper disable once InconsistentNaming
        private const string _configFilePath = "GameData/CIT/ChopIt/Plugins/ChopIt.cfg";
        private const string SettingsNodeName = "CHOP_IT_SETTINGS";
        private static Config _instance;

        private static string ConfigFilePath
        {
            get { return KSPUtil.ApplicationRootPath + _configFilePath; }
        }

        private static readonly Dictionary<string, SettingsEntry> Values = new Dictionary<string, SettingsEntry>
                                                                           {
                                                                               {"EVAOnly", new SettingsEntry(true)},
                                                                               {"SurfaceAttachedOnly", new SettingsEntry(true)},
                                                                               {"ChopContSoundFile", new SettingsEntry("CIT/ChopIt/Sounds/CI_Chop_cont")},
                                                                               {"ChopEndSoundFile", new SettingsEntry("CIT/ChopIt/Sounds/CI_Chop_end")},
                                                                               {"EjectionForce", new SettingsEntry(1.5f)},
                                                                               {"EnableHighlightParent", new SettingsEntry(false)},
                                                                               {"ParentHighlightInterval", new SettingsEntry(120)},
                                                                               {"SelfDestructionActive", new SettingsEntry(true)},
                                                                               {"SelfDestructionOnlyWithoutParent", new SettingsEntry(true)},
                                                                               {"DefaultTimerValue", new SettingsEntry(10)},
                                                                               {"ShowSelfDestructionCountdown", new SettingsEntry(true)},
                                                                               {"TimerSoundFile", new SettingsEntry("CIT/ChopIt/Sounds/CI_Beep")},
                                                                               {"PlayTimerSound", new SettingsEntry(true)},
                                                                               {"ShowTimerManipulationActions", new SettingsEntry(true)},
                                                                               {"ExplodeConnectedParts", new SettingsEntry(true)},
                                                                               {"IncreaseExplosionPotential", new SettingsEntry(true)},
                                                                               {"MultiplierForExplosionPotential", new SettingsEntry(1.25f)},
                                                                               {"IncreaseExplosionPotentialOfConnectedParts", new SettingsEntry(false)},
                                                                               {"ExplodeWholeVessel", new SettingsEntry(false)},
                                                                               {"MinChopDuration", new SettingsEntry(1.5f)},
                                                                               {"MaxChopDuration", new SettingsEntry(120f)},
                                                                               {"ChopDurationFunctionParamK", new SettingsEntry(2.8856454251540913d)},
                                                                               {"ChopDurationFunctionParamD", new SettingsEntry(9.641447136629273d)},
                                                                               {"ChopDurationFunctionStackPenaltyFactor", new SettingsEntry(2f)},
                                                                               {"MaxChopDistance", new SettingsEntry(2.5f)}
                                                                           };

        internal string ChopContSoundFileUrl
        {
            get { return _getValue<string>("ChopContSoundFile"); } // KSPUtil.ApplicationRootPath + _getValue<string>("ChopContSoundFile"); }
        }

        internal double ChopDurationFunctionParamD
        {
            get { return _getValue<double>("ChopDurationFunctionParamD"); }
        }

        internal double ChopDurationFunctionParamK
        {
            get { return _getValue<double>("ChopDurationFunctionParamK"); }
        }

        internal float ChopDurationFunctionStackPenaltyFactor
        {
            get { return _getValue<float>("ChopDurationFunctionStackPenaltyFactor"); }
        }

        internal string ChopEndSoundFileUrl
        {
            get { return _getValue<string>("ChopEndSoundFile"); } // KSPUtil.ApplicationRootPath + _getValue<string>("ChopEndSoundFile"); }
        }

        internal int DefaultTimerValue
        {
            get { return _getValue<int>("DefaultTimerValue"); }
        }

        internal float EjectionForce
        {
            get { return _getValue<float>("EjectionForce"); }
        }

        internal bool EnableHighlightParent
        {
            get { return _getValue<bool>("EnableHighlightParent"); }
        }

        internal bool EvaOnly
        {
            get { return _getValue<bool>("EVAOnly"); }
        }

        internal bool ExplodeConnectedParts
        {
            get { return _getValue<bool>("ExplodeConnectedParts"); }
        }

        internal bool ExplodeWholeVessel
        {
            get { return _getValue<bool>("ExplodeWholeVessel"); }
        }

        internal bool IncreaseExplosionPotential
        {
            get { return _getValue<bool>("IncreaseExplosionPotential"); }
        }

        internal bool IncreaseExplosionPotentialOfConnectedParts
        {
            get { return _getValue<bool>("IncreaseExplosionPotentialOfConnectedParts"); }
        }

        internal static Config Instance
        {
            get { return _instance ?? (_instance = new Config()); }
        }

        internal float MaxChopDistance
        {
            get { return _getValue<float>("MaxChopDistance"); }
        }

        internal float MaxChopDuration
        {
            get { return _getValue<float>("MaxChopDuration"); }
        }

        internal float MinChopDuration
        {
            get { return _getValue<float>("MinChopDuration"); }
        }

        internal float MultiplierForExplosionPotential
        {
            get { return _getValue<float>("MultiplierForExplosionPotential"); }
        }

        internal int ParentHighlightInterval
        {
            get { return _getValue<int>("ParentHighlightInterval"); }
        }

        internal bool PlayTimerSound
        {
            get { return _getValue<bool>("PlayTimerSound"); }
        }

        internal bool SelfDestructionActive
        {
            get { return _getValue<bool>("SelfDestructionActive"); }
        }

        internal bool SelfDestructionOnlyWithoutParent
        {
            get { return _getValue<bool>("SelfDestructionOnlyWithoutParent"); }
        }

        internal bool ShowSelfDestructionCountdown
        {
            get { return _getValue<bool>("ShowSelfDestructionCountdown"); }
        }

        internal bool ShowTimerManipulationActions
        {
            get { return _getValue<bool>("ShowTimerManipulationActions"); }
        }

        internal bool SurfaceOnly
        {
            get { return _getValue<bool>("SurfaceAttachedOnly"); }
        }

        internal string TimerSoundFileUrl
        {
            get { return _getValue<string>("TimerSoundFile"); } // KSPUtil.ApplicationRootPath + _getValue<string>("TimerSoundFile"); }
        }

        private Config()
        {
            if (!_configFileExists())
            {
                _initialSave();
                Thread.Sleep(500);
            }
            _load();
        }

        private static bool _configFileExists()
        {
            return File.Exists(ConfigFilePath);
        }

        private static T _getValue<T>(string key)
        {
            var val = Values[key];
            var ret = val.Value ?? val.DefaultValue;
            return (T) Convert.ChangeType(ret, typeof(T));
        }

        private static void _initialSave()
        {
            ConfigNode node = new ConfigNode(), settings = new ConfigNode(SettingsNodeName);
            foreach (var settingsEntry in Values)
            {
                settings.AddValue(settingsEntry.Key, settingsEntry.Value.DefaultValue);
            }
            node.AddNode(settings);
            node.Save(ConfigFilePath);
        }

        private static void _load()
        {
            var node = ConfigNode.Load(ConfigFilePath);
            var settings = node.GetNode(SettingsNodeName);
            foreach (var settingsEntry in Values)
            {
                var val = settings.GetValue(settingsEntry.Key);
                if (val != null)
                {
                    settingsEntry.Value.Value = val;
                }
            }
        }
    }
}