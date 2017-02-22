using UnityEngine;

namespace CIT_BlastAwesomenessModifier
{
    public class ModuleCustomBAM : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)] public string fixedValue;

        internal float ExplosionPotential
        {
            get
            {
                var c = Config.Instance;
                float ret;
                if (!float.TryParse(this.fixedValue, out ret))
                {
                    ret = c.Base;
                }
                return Mathf.Clamp(ret, 0f, 50000f);
            }
        }
    }
}