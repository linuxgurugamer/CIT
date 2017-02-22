using System;
using System.Collections.Generic;

namespace CIT_BlastAwesomenessModifier
{
    public class Config
    {
        // ReSharper disable once InconsistentNaming
        private const string _configFilePath = "GameData/CIT/BAM/Plugins/BAM.cfg";
        private const string SettingsNodeName = "CIT_BAM_SETTINGS";
        private static Config _instance;

        private static string ConfigFilePath
        {
            get { return KSPUtil.ApplicationRootPath + _configFilePath; }
        }

        internal static Config Instance
        {
            get { return _instance ?? (_instance = new Config()); }
        }

        private Config()
        {
            this.ResDic = new Dictionary<string, float>();
            this._load();
        }

        internal Dictionary<string, float> ResDic;
        internal float Base;
        internal float Min;
        internal float Max;
        internal bool Debug;

        private void _load()
        {
            var node = ConfigNode.Load(ConfigFilePath);
            var settings = node.GetNode(SettingsNodeName);
            var resDefs = settings.GetValue("resdef").Split(';');
            foreach (var resDef in resDefs)
            {
                try
                {
                    var parts = resDef.Split(',');
                    var name = parts[0];
                    var val = float.Parse(parts[1]);
                    this.ResDic.Add(name, val);
                }
                catch (Exception)
                {
                }
            }
            this.Max = float.Parse(settings.GetValue("max"));
            this.Min = float.Parse(settings.GetValue("min"));
            this.Base = float.Parse(settings.GetValue("base"));
            this.Debug = bool.Parse(settings.GetValue("dbg"));
        }
    }
}