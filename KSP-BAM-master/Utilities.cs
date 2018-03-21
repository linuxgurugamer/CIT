using System;
using System.Collections.Generic;
using System.Linq;
//using CIT_Util.Types;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CIT_BlastAwesomenessModifier
{
    public static class Utilities
    {

        public static List<T> GetAllModulesInLoadRange<T>(string moduleName, Func<PartModule, T> convFunc) where T : class
        {
            var moduleList = new List<T>();
            if (HighLogic.LoadedSceneIsEditor)
            {
                moduleList = EditorLogic.fetch.ship.Parts.Where(part => part.Modules.Contains(moduleName))
                                        .Select(part => convFunc(part.Modules[moduleName]))
                                        .ToList();
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                moduleList = FlightGlobals.Vessels.Where(v => v.loaded)
                                      .Where(v => v.Parts.Any(p => p.Modules.Contains(moduleName)))
                                      .SelectMany(v => v.Parts)
                                      .Where(part => part.Modules.Contains(moduleName))
                                      .Select(part => convFunc(part.Modules[moduleName]))
                                      .ToList();
            }
            return moduleList;

        }

        public static List<Part> GetAllModulesInLoadRange(Func<Part, bool> filterFunc = null)
        {
            var cond = filterFunc ?? ((p) => true);
            if (HighLogic.LoadedSceneIsFlight)
            {
                return FlightGlobals.Vessels.Where(v => v.loaded)
                                    .Where(v => v.Parts.Any(cond))
                                    .SelectMany(v => v.Parts)
                                    .ToList();
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                return EditorLogic.fetch.ship.Parts.Where(cond)
                                  .Select(part => part)
                                  .ToList();
            }
            return new List<Part>();
        }

    }
}