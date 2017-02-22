/*
The MIT License (MIT)
Copyright (c) 2014 marce

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSP_ChopIt
{
    public class ModuleChopIt : PartModule
    {
        private const string SelfDestructLabel = "Self Destruct";
        private const byte GuiUpdateInterval = 30;
        private const string StartTimerLabel = "StartTimer";
        private const string StopTimerLabel = "StopTimer";
        private const string IncreaseTimerLabel = "IncreaseTimer";
        private const string DecreaseTimerLabel = "DecreaseTimer";
        private const string ChopItLabel = "ChopIt";
        private const string HighlightParentLabel = "HighlightParent";
        private const float UnfocusedRangeDetach = 25f;
        private const string DetachFromVesselGuiName = "ChopIt!";
        private double _chopDuration;
        private bool _chopping;
        private double _choppingStartTime;
        private double _distance;
        private bool _distanceWarningPlayed;
        private byte _guiUpdateCounter;
        private double _lastTimerUpdate;
        private int _parentHighlightCounter;
        private bool _selfDestructionActive;
        private int _timer;
        private double _timerStartTime;
        internal bool TimerActive;
        internal bool ActionMenuOpen { get; set; }
        internal ChopItAddon AddonRef { get; set; }
        internal bool Initialized { get; set; }

        private bool SurfaceAttached
        {
            get { return this.part.attachMode == AttachModes.SRF_ATTACH && this.part.srfAttachNode != null && this.part.srfAttachNode.attachedPart != null; }
        }

        [KSPEvent(name = ChopItLabel, active = false, guiActive = false, guiName = DetachFromVesselGuiName, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = UnfocusedRangeDetach)]
        public void ChopIt()
        {
            if (!FlightGlobals.ActiveVessel.isEVA)
            {
                this._detachPart();
            }
            if (this._distance > Config.Instance.MaxChopDistance)
            {
                ScreenMessages.PostScreenMessage("[ChopIt] Too far away!", 3f, ScreenMessageStyle.UPPER_CENTER);
                if (!this._chopping)
                {
                    return;
                }
            }
            if (this._chopping)
            {
                this._chopping = false;
                this._distanceWarningPlayed = false;
                ScreenMessages.PostScreenMessage("[ChopIt] Abort", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                this._chopping = true;
                this._choppingStartTime = Planetarium.GetUniversalTime();
                this._distanceWarningPlayed = false;
            }
        }

        [KSPEvent(name = DecreaseTimerLabel, active = false, guiActive = false, guiName = "Decrease Timer", guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5)]
        public void DecreaseTimer()
        {
            if (this._timer > 0)
            {
                this._timer--;
                this.Events[StartTimerLabel].guiName = SelfDestructLabel + string.Format(" ({0}s)", this._timer);
            }
        }

        [KSPEvent(name = HighlightParentLabel, active = false, guiActive = false, guiName = "Highlight Parent", guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = UnfocusedRangeDetach)]
        public void HighlightParent()
        {
            if (this.part.parent == null)
            {
                return;
            }
            this._parentHighlightCounter = Config.Instance.ParentHighlightInterval;
            this.part.parent.SetHighlightColor(Color.blue);
            this.part.parent.SetHighlight(true, false);
        }

        [KSPEvent(name = IncreaseTimerLabel, active = false, guiActive = false, guiName = "Increase Timer", guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5)]
        public void IncreaseTimer()
        {
            if (this._timer < int.MaxValue)
            {
                this._timer++;
                this.Events[StartTimerLabel].guiName = SelfDestructLabel + string.Format(" ({0}s)", this._timer);
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !this.ActionMenuOpen | !this.Initialized)
            {
                return;
            }
            this._distance = Vector3.Distance(FlightGlobals.ActiveVessel.rootPart.transform.position, this.part.transform.position);
            var maxChopDistance = Config.Instance.MaxChopDistance;
            if (!this._chopping)
            {
                var label = DetachFromVesselGuiName + string.Format(" ({0:0.00}s | {1:0.00}/{2:0.00}m)", this._chopDuration, this._distance, maxChopDistance);
                this.Events[ChopItLabel].guiName = label;
            }
            if ((this._distance/maxChopDistance) > 0.8 && this._chopping)
            {
                if (this._distance > maxChopDistance)
                {
                    ScreenMessages.PostScreenMessage("[ChopIt] Too far away - aborting operation!", 3f, ScreenMessageStyle.UPPER_CENTER);
                    this._chopping = false;
                }
                else if (!this._distanceWarningPlayed)
                {
                    _playTimerSound(true);
                    ScreenMessages.PostScreenMessage("[ChopIt] Attention: Distance!", 3f, ScreenMessageStyle.UPPER_CENTER);
                    this._distanceWarningPlayed = true;
                }
            }
            else
            {
                this._distanceWarningPlayed = false;
            }
            if (this._chopping)
            {
                var now = Planetarium.GetUniversalTime();
                var timeChopped = now - this._choppingStartTime;
                var diff = this._chopDuration - timeChopped;
                diff = diff >= 0d ? diff : 0d;
                this.Events[ChopItLabel].guiName = DetachFromVesselGuiName + string.Format(" ({0:0.00}s | {1:0.00}/{2:0.00}m)", diff, this._distance, maxChopDistance);
                if (diff > 0d)
                {
                    _playChopSound(false);
                    return;
                }
                this._chopping = false;
                this._detachPart();
            }
        }

        public override void OnStart(StartState state)
        {
            this.Start();
        }

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            //this.isEnabled = true;
            this._selfDestructionActive = Config.Instance.SelfDestructionActive;
            this._parentHighlightCounter = -1;
            this._guiUpdateCounter = 0;
            this._chopDuration = this.part.CalculateChopTime();
            if (!Config.Instance.SelfDestructionActive || !HighLogic.LoadedSceneHasPlanetarium)
            {
                this._setEventStatus(StartTimerLabel, false);
                this._setEventStatus(StopTimerLabel, false);
                this._setEventStatus(IncreaseTimerLabel, false);
                this._setEventStatus(DecreaseTimerLabel, false);
            }
            else
            {
                this._timer = Config.Instance.DefaultTimerValue;
                this.TimerActive = false;
                this.Events[StartTimerLabel].guiName = SelfDestructLabel + string.Format(" ({0}s)", this._timer);
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !this.Initialized)
            {
                return;
            }
            if (this.TimerActive && this._selfDestructionActive)
            {
                var now = Planetarium.GetUniversalTime();
                var diff = Math.Floor(now - this._timerStartTime);
                var remainingTime = (int) (this._timer - diff);
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
                this.TimerActive = false;
                this._processExplosion();
            }
            if (this._parentHighlightCounter > 0)
            {
                this._parentHighlightCounter--;
            }
            else if (this._parentHighlightCounter == 0)
            {
                if (this.part.parent != null)
                {
                    this.part.parent.SetHighlightDefault();
                }
                this._parentHighlightCounter = -1;
            }
            if (this._guiUpdateCounter > 0)
            {
                this._guiUpdateCounter--;
                return;
            }
            this._guiUpdateCounter = GuiUpdateInterval;
            this._chopDuration = this.part.CalculateChopTime();
            if (this._selfDestructionActive)
            {
                var onlyWithoutParent = Config.Instance.SelfDestructionOnlyWithoutParent;
                if ((onlyWithoutParent && this.part.parent == null) || !onlyWithoutParent)
                {
                    if (this.TimerActive)
                    {
                        this._setEventStatus(StartTimerLabel, false);
                        this._setEventStatus(StopTimerLabel, true);
                        this._setEventStatus(IncreaseTimerLabel, false);
                        this._setEventStatus(DecreaseTimerLabel, false);
                    }
                    else
                    {
                        this._setEventStatus(StartTimerLabel, true);
                        this._setEventStatus(StopTimerLabel, false);
                        if (Config.Instance.ShowTimerManipulationActions)
                        {
                            this._setEventStatus(IncreaseTimerLabel, true);
                            this._setEventStatus(DecreaseTimerLabel, true);
                        }
                    }
                }
                else
                {
                    this._setEventStatus(StartTimerLabel, false);
                    this._setEventStatus(StopTimerLabel, false);
                    this._setEventStatus(IncreaseTimerLabel, false);
                    this._setEventStatus(DecreaseTimerLabel, false);
                }
            }
            if (!this.part.isAttached || this.part.parent == null || (Config.Instance.EvaOnly && !FlightGlobals.ActiveVessel.isEVA) || (Config.Instance.SurfaceOnly && !this.SurfaceAttached))
            {
                this._setEventStatus(ChopItLabel, false);
                this._setEventStatus(HighlightParentLabel, false);
                return;
            }
            if (!this.part.isAttached)
            {
                return;
            }
            this._setEventStatus(ChopItLabel, true);
            if (Config.Instance.EnableHighlightParent)
            {
                this._setEventStatus(HighlightParentLabel, true);
            }
        }

        [KSPEvent(name = StartTimerLabel, active = false, guiActive = false, guiName = SelfDestructLabel, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5)]
        public void StartTimer()
        {
            this.TimerActive = true;
            this._timerStartTime = Planetarium.GetUniversalTime();
            this._lastTimerUpdate = -1;
        }

        [KSPEvent(name = StopTimerLabel, active = false, guiActive = false, guiName = "Stop Self Destruction", guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5)]
        public void StopTimer()
        {
            this.TimerActive = false;
        }

        private void _detachPart()
        {
            if (this.part.parent == null)
            {
                return;
            }
            _playChopSound(true);
            var parentPos = this.part.parent.transform.position;
            this.part.decouple();
            this.Events[ChopItLabel].active = this.Events[ChopItLabel].guiActive = false;
            var forceVector = Vector3.Normalize(this.part.transform.position - parentPos);
            this.part.Rigidbody.AddForce(forceVector*Config.Instance.EjectionForce, ForceMode.Force);
        }

        private List<Part> _getConnectedParts()
        {
            if (Config.Instance.ExplodeWholeVessel)
            {
                if (this.part.vessel != null && this.part.vessel.parts != null)
                {
                    return this.part.vessel.parts;
                }
            }
            var connectedParts = new HashSet<Part>();
            if (this.part.parent != null)
            {
                connectedParts.Add(this.part.parent);
            }
            if (this.part.children != null)
            {
                foreach (var childPart in this.part.children)
                {
                    connectedParts.Add(childPart);
                }
            }
            if (this.part.vessel != null && this.part.vessel.parts != null)
            {
                var connVesselParts = this.part.vessel.parts.Where(vesselPart =>
                                                                   (vesselPart.parent != null && vesselPart.parent == this.part)
                                                                   || (vesselPart.isAttached && vesselPart.attachJoint != null
                                                                       && ((vesselPart.attachJoint.Host != null && vesselPart.attachJoint.Host == this.part)
                                                                           || (vesselPart.attachJoint.Target != null && vesselPart.attachJoint.Target == this.part)
                                                                           || (vesselPart.attachJoint.Parent != null && vesselPart.attachJoint.Parent == this.part)
                                                                           || (vesselPart.attachJoint.Child != null && vesselPart.attachJoint.Child == this.part))));
                foreach (var connVesselPart in connVesselParts)
                {
                    connectedParts.Add(connVesselPart);
                }
            }
            return connectedParts.ToList();
        }

        private static void _playChopSound(bool end)
        {
            ChopItAddon.PlayChopSound(end);
        }

        private static void _playTimerSound(bool doubleVolume = false)
        {
            ChopItAddon.PlayTimerSound(doubleVolume);
        }

        private void _postCountdownMessage(int remainingTime)
        {
            var msgPart = remainingTime == 0 ? "now" : string.Format("in {0} seconds", remainingTime);
            var msg = this.part.name + " will explode " + msgPart + "!";
            ScreenMessages.PostScreenMessage(msg, remainingTime == 0 ? 2 : 0.9f, ScreenMessageStyle.UPPER_CENTER);
        }

        private void _processExplosion()
        {
            var toExplode = new List<Part>(5);
            var conf = Config.Instance;
            if (conf.IncreaseExplosionPotential)
            {
                this.part.explosionPotential *= conf.MultiplierForExplosionPotential;
            }
            if (conf.ExplodeConnectedParts)
            {
                var connParts = this._getConnectedParts();
                if (conf.IncreaseExplosionPotentialOfConnectedParts)
                {
                    var mult = conf.MultiplierForExplosionPotential;
                    foreach (var connPart in connParts)
                    {
                        connPart.explosionPotential *= mult;
                    }
                }
                toExplode.AddRange(connParts);
            }
            toExplode.Add(this.part);
            foreach (var partToExplode in toExplode)
            {
                partToExplode.explode();
            }
        }

        private void _setEventStatus(string label, bool state)
        {
            var eventObj = this.Events[label];
            eventObj.active = state;
            eventObj.guiActive = state;
        }
    }
}