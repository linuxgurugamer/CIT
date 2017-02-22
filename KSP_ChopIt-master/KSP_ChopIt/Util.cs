using System;
using CIT_Util;

namespace KSP_ChopIt
{
    internal static class Util
    {
        internal static double CalculateChopTime(this Part part)
        {
            if (part == null || part.parent == null || !part.isAttached)
            {
                return 0f;
            }
            var conf = Config.Instance;
            var mass = part.GetMassOfPartAndChildren();
            var duration = ((conf.ChopDurationFunctionParamK*Math.Log(mass) + conf.ChopDurationFunctionParamD)*(1 + mass*0.01))/1.5d;
            if (part.attachMode == AttachModes.STACK)
            {
                duration *= conf.ChopDurationFunctionStackPenaltyFactor;
            }
            duration = Math.Max(duration, conf.MinChopDuration);
            duration = Math.Min(duration, conf.MaxChopDuration);
            return duration;
        }
    }
}