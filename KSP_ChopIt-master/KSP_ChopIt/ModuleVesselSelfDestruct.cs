using System;
using System.Linq;

namespace KSP_ChopIt
{
    public class ModuleVesselSelfDestruct : PartModule
    {
        private const string LabelSelfDestruct = "SelfDestruct";
        private const int TimerInterval = 10;
        private const byte UpdateInterval = 15;
        private const byte VesselTypeCheckInterval = 90;
        private double _lastTimerUpdate;
        private bool _selfDestructActive;
        private double _timerStartTime;
        private byte _updateCnt;
        private byte _vesselCheckCnt;

        [KSPEvent(name = LabelSelfDestruct, active = true, guiActive = true, guiName = "Self Destruct Vessel", guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5)]
        public void SelfDestruct()
        {
            if (this.part.vessel == null)
            {
                return;
            }
            if (this._selfDestructActive)
            {
                ScreenMessages.PostScreenMessage("Self Destruct already started. Shutdown impossible.", 3f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            this._selfDestructActive = true;
            this._timerStartTime = Planetarium.GetUniversalTime();
        }

        private static void _playTimerSound(bool doubleVolume = false)
        {
            ChopItAddon.PlayTimerSound(doubleVolume);
        }

        private void _postCountdownMessage(int remainingTime)
        {
            try
            {
                var msgPart = remainingTime == 0 ? "now" : string.Format("in {0} seconds", remainingTime);
                var msg = "Vessel " + this.part.vessel.vesselName + " will explode " + msgPart + "!";
                ScreenMessages.PostScreenMessage(msg, remainingTime == 0 ? 2 : 0.9f, ScreenMessageStyle.UPPER_CENTER);
            }
            catch (NullReferenceException)
            {
                //no-op
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !HighLogic.LoadedSceneHasPlanetarium)
            {
                return;
            }
            this._checkForNonKerbal();
            this._checkForDebrisType();
            if (!this._selfDestructActive)
            {
                return;
            }
            var now = Planetarium.GetUniversalTime();
            var diff = Math.Floor(now - this._timerStartTime);
            var remainingTime = (int) (TimerInterval - diff);
            if (Config.Instance.ShowSelfDestructionCountdown || Config.Instance.PlayTimerSound)
            {
                if (this._lastTimerUpdate < diff)
                {
                    this._lastTimerUpdate = diff;
                    if (Config.Instance.ShowSelfDestructionCountdown)
                    {
                        this._postCountdownMessage(remainingTime >= 0 ? remainingTime : 0);
                    }
                    if (Config.Instance.PlayTimerSound)
                    {
                        _playTimerSound();
                    }
                }
            }
            if (remainingTime > 0)
            {
                return;
            }
            this._processExplosion();
        }

        private void _checkForNonKerbal()
        {
            if (this._updateCnt > 0)
            {
                this._updateCnt--;
                return;
            }
            this._updateCnt = UpdateInterval;
            var e = this.Events[LabelSelfDestruct];
            if (this.part.vessel == null || this.part.vessel.isEVA)
            {
                e.active = e.guiActive = e.guiActiveUnfocused = false;
            }
            else
            {
                e.active = e.guiActive = e.guiActiveUnfocused = true;
            }
        }

        private bool HasVesselEcLeft()
        {
            try
            {
                var res = this.part.vessel.GetActiveResource(PartResourceLibrary.Instance.GetDefinition("ElectricCharge"));
                return res.amount > 0d;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void _checkForDebrisType()
        {
            if (this._vesselCheckCnt > 0)
            {
                this._vesselCheckCnt--;
                return;
            }
            this._vesselCheckCnt = VesselTypeCheckInterval;
            if (this.part.vessel == null)
            {
                return;
            }
            if (this.part.vessel.vesselType != VesselType.Debris)
            {
                return;
            }
            if (this.HasVesselEcLeft())
            {
                this.part.vessel.vesselType = VesselType.Probe;
            }
        }

        private void _processExplosion()
        {
            if (this.part.vessel == null)
            {
                this.part.explosionPotential *= 2f;
                this.part.explode();
                return;
            }
            var allParts = this.part.vessel.Parts.Where(p => p != this.part).ToList();
            foreach (var p in allParts)
            {
                p.explosionPotential *= 1.5f;
                p.explode();
            }
            this.part.explode();
        }
    }
}