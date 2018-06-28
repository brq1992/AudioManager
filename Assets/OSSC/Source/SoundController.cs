using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OSSC.Model;

namespace OSSC
{
    /// <summary>
    /// The main class that is used for Playing and controlling all sounds.
    /// </summary>
    [RequireComponent(typeof(ObjectPool))]
    public class SoundController : MonoBehaviour
    {
        #region Serialized Data
        /// <summary>
        /// Default prefab with SoundObject and AudioSource.
        /// It is used by the Soundcontroller to play SoundCues.
        /// </summary>
        public GameObject _defaultPrefab;

        #endregion

        #region Private fields

        /// <summary>
        /// Gives instances of GameObjects thrown in it.
        /// </summary>
        private ObjectPool _pool;
        /// <summary>
        /// Manages all created SoundCues.
        /// </summary>
        private CueManager _cueManager;
        /// <summary>
        /// Initial pool size of SoundCues for CueManager.
        /// </summary>
        private int _initialCueManagerSize = 10;

        private Dictionary<string, ISoundCue> audioDictionary; 
        #endregion

        #region Public methods and properties

        /// <summary>
        /// Set the default Prefab with SoundObject and AudioSource in it.
        /// </summary>
        public GameObject defaultPrefab
        {
            set
            {
                _defaultPrefab = value;
            }
        }

        public void Stop(string audioName)
        {
            if (audioDictionary.ContainsKey(audioName))
            {
                audioDictionary[audioName].Stop();
            }
        }


        /// <summary>
        /// Stop all Playing Sound Cues.
        /// </summary>
        /// <param name="shouldCallOnEndCallback">Control whether to call the OnEnd event, or not.</param>
        public void StopAll(bool shouldCallOnEndCallback = true)
        {
            _cueManager.StopAllCues(shouldCallOnEndCallback);
        }


        public ISoundCue Play(PlaySoundSettings settings)
        {
            if (string.IsNullOrEmpty(settings.name))
            {
                return null;
            }

            float fadeInTime = settings.fadeInTime;
            float fadeOutTime = settings.fadeOutTime;
            bool isLooped = settings.isLooped;
            Transform parent = settings.parent;
            GameObject prefab = null;
            SoundItem item = new SoundItem();
            item.name = settings.name;

            //todo: audio load should be 
            AudioClip clip = Resources.Load(settings.Path) as AudioClip;
            if (!clip)
            {
                Debug.LogError("Can't find clip! " + settings.Path);
                return null;
            }
            item.clips = new[]{clip};
            item.isRandomPitch = false;
            item.volume = 1;
            item.isRandomVolume = false;
            List<SoundItem> items = new List<SoundItem> {item};
            List<float> catVolumes = new List<float> {settings.Volumn};
            List<CategoryItem> categories = new List<CategoryItem>();

            //todo? prefab choose
            prefab = _defaultPrefab;



            if (items.Count == 0)
                return null;

            SoundCue cue = _cueManager.GetSoundCue();
            SoundCueData data;
            data.audioPrefab = prefab;
            data.sounds = items.ToArray();
            data.categoryVolumes = catVolumes.ToArray();
            data.categoriesForSounds = categories.ToArray();
            data.fadeInTime = fadeInTime;
            data.fadeOutTime = fadeOutTime;
            data.isFadeIn = data.fadeInTime >= 0.1f;
            data.isFadeOut = data.fadeOutTime >= 0.1f;
            data.isLooped = isLooped;
            data.Name = settings.name;
            cue.AudioObject = _pool.GetFreeObject(prefab).GetComponent<SoundObject>();
            if (parent != null)
                cue.AudioObject.transform.SetParent(parent, false);

            SoundCueProxy proxy = new SoundCueProxy();
            proxy.SoundCue = cue;
            proxy.Play(data);
            return proxy;
        }

        #endregion

        #region Private methods
        /// <summary>
        /// This method is called only when PlaySoundSettings has a SoundCue reference in it.
        /// Same as Play(), but much faster.
        /// </summary>
        /// <param name="settings">PlaySoundSettings instance with SoundCue reference in it.</param>
        /// <returns>Same SoundCue from PlaySoundSettings</returns>
        private SoundCueProxy PlaySoundCue(PlaySoundSettings settings)
        {
            SoundCueProxy proxy = settings.soundCueProxy as SoundCueProxy;
            Transform parent = settings.parent;
            float fadeInTime = settings.fadeInTime;
            float fadeOutTime = settings.fadeOutTime;
            bool isLooped = settings.isLooped;
            SoundCue ncue = _cueManager.GetSoundCue();
            ncue.AudioObject = _pool.GetFreeObject(proxy.Data.audioPrefab).GetComponent<SoundObject>();
            if (parent != null)
                ncue.AudioObject.transform.SetParent(parent, false);
            SoundCueData data = proxy.Data;
            data.fadeInTime = fadeInTime;
            data.fadeOutTime = fadeOutTime;
            data.isFadeIn = data.fadeInTime >= 0.1f;
            data.isFadeOut = data.fadeOutTime >= 0.1f;
            data.isLooped = isLooped;
            data.Name = settings.name;
            proxy.SoundCue = ncue;
            proxy.Play(data);
            return proxy;
        }

        /// <summary>
        /// for user to play audio
        /// </summary>
        /// <param name="settings"></param>
        private void PlayAudio(PlaySoundSettings settings)
        {
            if (audioDictionary.ContainsKey(settings.name))
            {
                if(settings.PlayState)
                {
                    settings.soundCueProxy = audioDictionary[settings.name];
                    PlaySoundCue(settings);
                }
                else
                {
                    audioDictionary[settings.name].Stop();
                }
            }
            else
            {
                if (settings.PlayState == false)
                {
                    return;
                }
                ISoundCue iSoundCue = Play(settings);
                if (iSoundCue != null)
                {
                    audioDictionary.Add(settings.name, iSoundCue);
                }
            }
            
        }

        #region Internal tests
        [ContextMenu("Test Active_Disable_menu Button")]
        void Test()
        {
            PlaySoundSettings settings = new PlaySoundSettings(
                "Fly Catchers", "Audios/BGM/Fly Catchers", 5f, 5f, false, 1f, null);
        }

        [ContextMenu("Test Active_Disable_menu Button looped")]
        void TestLoop()
        {
            PlaySoundSettings settings = new PlaySoundSettings(
                "Fly Catchers", "Audios/BGM/Fly Catchers", 5f, 0f, false, 1f, null, false);
        }

        [ContextMenu("Test Test Active_Disable_menu Button looped plays 2 times")]
        void TestSequence2TimesPlay()
        {
            PlaySoundSettings settings = new PlaySoundSettings(
                "Active_Disable_menu Button", "UI_Auidos/Active_Disable_menu Button", 0f, 0f, false, 1f, null);
        }
        #endregion
        
        #endregion

        #region MonoBehaviour methods

        void Awake()
        {
            _pool = GetComponent<ObjectPool>();
            _cueManager = new CueManager(_initialCueManagerSize);
            audioDictionary = new Dictionary<string, ISoundCue>();
        }
        #endregion
    }

    /// <summary>
    /// Set the settings to play a particular cue with particular preferences.
    /// </summary>
    public struct PlaySoundSettings
    {
        public string Path;
        /// <summary>
        /// Name of the soundItem to be played
        /// </summary>
        public string name;
        /// <summary>
        /// Attach the Playing sound to a Specific GameObject
        /// </summary>
        public Transform parent;
        /// <summary>
        /// Fade In time of the whole SoundCue
        /// </summary>
        public float fadeInTime;
        /// <summary>
        /// Fade Out time of the whole SoundCue
        /// </summary>
        public float fadeOutTime;
        /// <summary>
        /// Control whether the SoundCue should loop
        /// </summary>
        public bool isLooped;
        /// <summary>
        /// Use the same SoundCue to play again the sounds played in that SoundCue
        /// This is recommended to do, because searching by names all the Sounds to play is very expensive.
        /// </summary>
        public ISoundCue soundCueProxy;
        /// <summary>
        /// the volumn when the audio clip is playing
        /// </summary>
        public float Volumn;
        /// <summary>
        /// play or stop false: stop, true: play
        /// </summary>
        public bool PlayState;
        public PlaySoundSettings(string name, string path, float fadeInTime, float fadeOutTime, bool isLooped, float volumn, Transform parent, bool playState = true)
            : this()
        {
            Path = path;
            this.name = name;
            this.fadeInTime = fadeInTime;
            this.fadeOutTime = fadeOutTime;
            this.isLooped = isLooped;
            this.parent = parent;
            Volumn = volumn;
            PlayState = playState;
        }
    }

}
