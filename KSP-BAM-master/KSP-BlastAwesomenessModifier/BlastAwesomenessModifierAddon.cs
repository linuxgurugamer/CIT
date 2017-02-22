using System.Collections.Generic;
using System.Linq;
using CIT_Util;
using UnityEngine;

namespace CIT_BlastAwesomenessModifier
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BlastAwesomenessModifierAddon : MonoBehaviour
    {
        private const byte UpdateInterval = 60;
        private static byte _updateCounter;
        private static Dictionary<string, PartResourceDefinition> _resDefs;

        public void Start()
        {
            _updateCounter = 120;
            _resDefs = new Dictionary<string, PartResourceDefinition>();
            foreach (var resMod in Config.Instance.ResDic)
            {
                var def = PartResourceLibrary.Instance.GetDefinition(resMod.Key);
                if (def != null)
                {
                    _resDefs.Add(resMod.Key, def);
                }
            }
        }

        private static float _calcExplosionPotential(Part part)
        {
            var cInst = Config.Instance;
            double ep = cInst.Base;
            var change = (from resDef in _resDefs
                          let pRes = part.Resources.Get(resDef.Value.id)
                          where pRes != null
                          let modVal = Config.Instance.ResDic[resDef.Key]
                          select modVal*pRes.amount).Sum();
            ep += change;
            return Mathf.Clamp((float) ep, cInst.Min, cInst.Max);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            if (_updateCounter > 0)
            {
                _updateCounter--;
                return;
            }
            _updateCounter = UpdateInterval;
            var allParts = Utilities.GetAllModulesInLoadRange();
            for (var i = 0; i < allParts.Count; i++)
            {
                var p = allParts[i];
                var module = p.FindModuleImplementing<ModuleCustomBAM>();
                p.explosionPotential = module == null ? _calcExplosionPotential(p) : module.ExplosionPotential;
                if (Config.Instance.Debug)
                {
                    var dbgModule = p.FindModuleImplementing<ModuleBAMDebug>();
                    if (dbgModule == null)
                    {
                        p.AddModule("ModuleBAMDebug");
                    }
                    dbgModule = p.FindModuleImplementing<ModuleBAMDebug>();
                    if (dbgModule != null)
                    {
                        dbgModule.UpdatePower();
                    }
                }
            }
        }
    }
}