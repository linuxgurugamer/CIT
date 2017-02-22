using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSP_ChopIt
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ChopItAddon : MonoBehaviour
    {
        private const string ModuleName = "ModuleChopIt";
        private const short CatchAllInterval = 1800;
        private const short FastCatchAllInterval = 30;
        private const short CurrPartUpdateInterval = 10;
        private static AudioSource _soundChopCont;
        private static AudioSource _soundChopEnd;
        private static AudioSource _soundTimer;
        private static GameObject _audioPlayer;
        private short _catchAllCounter;
        private Part _currentSelPart;
        private short _currPartUpdateCounter;
        private object _listLock;
        private List<VesselWrapper> _vesselsToProcess;

        public void Awake()
        {
            GameEvents.onPartActionUICreate.Add(this.HandleActionMenuOpened);
            GameEvents.onPartActionUIDismiss.Add(this.HandleActionMenuClosed);
            GameEvents.onVesselChange.Add(this.HandleVesselChange);
            GameEvents.onPartPack.Add(this.HandlePartPack);
            GameEvents.onVesselGoOnRails.Add(this.HandleVesselGoOnRails);
            GameEvents.onVesselWasModified.Add(this.HandleVesselChange);
            GameEvents.onVesselGoOffRails.Add(this.HandleVesselOfRails);
            GameEvents.onVesselLoaded.Add(this.HandleVesselOfRails);
            GameEvents.onCrewOnEva.Add(this.HandleEvaStart);
            this._vesselsToProcess = new List<VesselWrapper>();
            this._listLock = new object();
            this._catchAllCounter = FastCatchAllInterval;
            this._currPartUpdateCounter = 0;
            _audioPlayer = new GameObject();
            _soundChopCont = _audioPlayer.AddComponent<AudioSource>();
            _soundChopEnd = _audioPlayer.AddComponent<AudioSource>();
            _soundTimer = _audioPlayer.AddComponent<AudioSource>();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            if (this._currentSelPart != null)
            {
                if (this._currPartUpdateCounter > 0)
                {
                    this._currPartUpdateCounter--;
                }
                else
                {
                    this._currPartUpdateCounter = CurrPartUpdateInterval;
                    var module = _getModuleFromPart(this._currentSelPart);
                    if (module != null)
                    {
                        module.ActionMenuOpen = true;
                        module.isEnabled = true;
                    }
                }
            }
            if (this._catchAllCounter > 0)
            {
                this._catchAllCounter--;
                return;
            }
            this._catchAllCounter = CatchAllInterval;
            lock (this._listLock)
            {
                this._vesselsToProcess.Add(new VesselWrapper(FlightGlobals.ActiveVessel));
                foreach (var vesselWrapper in this._vesselsToProcess)
                {
                    if (vesselWrapper.Vessel != null)
                    {
                        this._addModuleToValidParts(vesselWrapper.Vessel);
                    }
                }
                this._vesselsToProcess.Clear();
            }
        }

        private void HandleActionMenuClosed(Part data)
        {
            var module = _getModuleFromPart(data);
            if (module != null)
            {
                module.ActionMenuOpen = false;
            }
            this._currentSelPart = null;
        }

        private void HandleActionMenuOpened(Part data)
        {
            var module = _getModuleFromPart(data);
            if (module != null)
            {
                module.ActionMenuOpen = true;
                this._currentSelPart = data;
            }
        }

        private void HandleEvaStart(GameEvents.FromToAction<Part, Part> data)
        {
            lock (this._listLock)
            {
                foreach (var vessel in FlightGlobals.Vessels.Where(v => v.loaded))
                {
                    this._vesselsToProcess.Add(new VesselWrapper(vessel));
                }
            }
            this._catchAllCounter = 0;
        }

        private void HandlePartPack(Part data)
        {
            _processPartPack(data);
        }

        private void HandleVesselChange(Vessel data)
        {
            lock (this._listLock)
            {
                this._vesselsToProcess.Add(new VesselWrapper(data));
            }
            this._catchAllCounter = FastCatchAllInterval;
        }

        private void HandleVesselGoOnRails(Vessel data)
        {
            foreach (var part in data.Parts)
            {
                _processPartPack(part);
            }
        }

        private void HandleVesselOfRails(Vessel data)
        {
            lock (this._listLock)
            {
                this._vesselsToProcess.Add(new VesselWrapper(data));
                this._catchAllCounter = FastCatchAllInterval;
            }
        }

        public void OnDestroy()
        {
            GameEvents.onPartActionUICreate.Remove(this.HandleActionMenuOpened);
            GameEvents.onPartActionUIDismiss.Remove(this.HandleActionMenuClosed);
            GameEvents.onVesselChange.Remove(this.HandleVesselChange);
            GameEvents.onPartPack.Remove(this.HandlePartPack);
            GameEvents.onVesselGoOnRails.Remove(this.HandleVesselGoOnRails);
            GameEvents.onVesselWasModified.Remove(this.HandleVesselChange);
            GameEvents.onVesselGoOffRails.Remove(this.HandleVesselOfRails);
            GameEvents.onVesselLoaded.Remove(this.HandleVesselOfRails);
            GameEvents.onCrewOnEva.Remove(this.HandleEvaStart);
        }

        internal static void PlayChopSound(bool end)
        {
            if (end)
            {
                if (_soundChopEnd == null)
                {
                    return;
                }
                if (!_soundChopEnd.isPlaying)
                {
                    _soundChopEnd.Play();
                }
            }
            else
            {
                if (_soundChopCont == null)
                {
                    return;
                }
                if (!_soundChopCont.isPlaying)
                {
                    _soundChopCont.Play();
                }
            }
        }

        internal static void PlayTimerSound(bool doubleVolume = false)
        {
            if (doubleVolume)
            {
                _soundTimer.volume *= 25f;
            }
            if (_soundTimer == null)
            {
                return;
            }
            _soundTimer.Play();
            _soundTimer.volume = GameSettings.SHIP_VOLUME;
        }

        public void Start()
        {
            if (!GameDatabase.Instance.ExistsAudioClip(Config.Instance.ChopContSoundFileUrl))
            {
                if (_soundChopCont == null)
                {
                    Debug.Log("[CI] soundchopcont is null addon");
                    return;
                }
                Debug.Log("[CI] sound not found addon");
                return;
            }
            _soundChopCont.clip = GameDatabase.Instance.GetAudioClip(Config.Instance.ChopContSoundFileUrl);
            _soundChopCont.dopplerLevel = 0f;
            _soundChopCont.rolloffMode = AudioRolloffMode.Linear;
            _soundChopCont.maxDistance = 30f;
            _soundChopCont.loop = false;
            _soundChopCont.playOnAwake = false;
            _soundChopCont.volume = GameSettings.SHIP_VOLUME;
            if (!GameDatabase.Instance.ExistsAudioClip(Config.Instance.ChopEndSoundFileUrl))
            {
                if (_soundChopEnd == null)
                {
                    Debug.Log("[CI] soundchopend is null addon");
                    return;
                }
                Debug.Log("[CI] sound not found addon");
                return;
            }
            _soundChopEnd.clip = GameDatabase.Instance.GetAudioClip(Config.Instance.ChopEndSoundFileUrl);
            _soundChopEnd.dopplerLevel = 0f;
            _soundChopEnd.rolloffMode = AudioRolloffMode.Linear;
            _soundChopEnd.maxDistance = 30f;
            _soundChopEnd.loop = false;
            _soundChopEnd.playOnAwake = false;
            _soundChopEnd.volume = GameSettings.SHIP_VOLUME;
            if (!GameDatabase.Instance.ExistsAudioClip(Config.Instance.TimerSoundFileUrl) || _soundTimer == null)
            {
                if (_soundTimer == null)
                {
                    Debug.Log("[CI] soundtimer is null addon");
                    return;
                }
                Debug.Log("[CI] sound not found addon");
                return;
            }
            _soundTimer.clip = GameDatabase.Instance.GetAudioClip(Config.Instance.TimerSoundFileUrl);
            _soundTimer.dopplerLevel = 0f;
            _soundTimer.rolloffMode = AudioRolloffMode.Logarithmic;
            _soundTimer.maxDistance = 50f;
            _soundTimer.loop = false;
            _soundTimer.playOnAwake = false;
            _soundTimer.volume = GameSettings.SHIP_VOLUME;
        }

        private void _addModuleToPart(Part part)
        {
            part.AddModule(ModuleName);
            this.StartCoroutine(this.CatchModuleAndStartIt(part));
        }

        private IEnumerator CatchModuleAndStartIt(Part part)
        {
            var run = true;
            while (run)
            {
                var module = _getModuleFromPart(part);
                if (module != null)
                {
                    run = false;
                    module.Start();
                    module.Initialized = true;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        private void _addModuleToValidParts(Vessel vessel)
        {
            var surfaceOnly = Config.Instance.SurfaceOnly;
            var selfDestructActive = Config.Instance.SelfDestructionActive;
            foreach (var part in vessel.Parts)
            {
                if (part != null
                    && (part.attachMode == AttachModes.SRF_ATTACH || !surfaceOnly)
                    && (part.parent != null || selfDestructActive)
                    && !part.Modules.Contains(ModuleName))
                {
                    this._addModuleToPart(part);
                }
            }
        }

        private static ModuleChopIt _getModuleFromPart(Part part)
        {
            ModuleChopIt ret = null;
            if (part.Modules.Contains(ModuleName))
            {
                ret = part.Modules[ModuleName] as ModuleChopIt;
            }
            return ret;
        }

        private static void _processPartPack(Part part)
        {
            var module = _getModuleFromPart(part);
            if (module != null)
            {
                part.RemoveModule(module);
            }
        }

        internal class VesselWrapper
        {
            internal VesselWrapper(Vessel vessel)
            {
                this.Vessel = vessel;
            }

            internal Vessel Vessel { get; set; }
        }
    }
}