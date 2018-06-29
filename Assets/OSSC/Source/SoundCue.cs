using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using OSSC.Model;

namespace OSSC
{
    /// <summary>
    /// Plays a whole cue of soundItems
    /// </summary>
    public class SoundCue : ISoundCue
    {
        /// <summary>
        /// Check ISoundCue
        /// </summary>
        public Action<string> OnPlayEnded { get; set; }

        /// <summary>
        /// Check ISoundCue
        /// </summary>
        public Action<SoundCue> OnPlayCueEnded { get; set; }

        /// <summary>
        /// Called whenever the sound cue has finished playing or was stopped
        /// </summary>
        public Action<SoundCue, SoundCueProxy> OnPlayKilled { get; set; }

        /// <summary>
        /// Check ISoundCue
        /// </summary>
        public SoundObject AudioObject { get; set; }

        /// <summary>
        /// Check ISoundCue
        /// </summary>
        public SoundCueData Data { get { return _data; } }

        /// <summary>
        /// Check ISoundCue
        /// </summary>
        public bool IsPlaying
        {
            get;
            private set;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="count"></param>
        public SoundCue(int count)
        {
        }

        /// <summary>
        /// SoundCue data
        /// </summary>
        private SoundCueData _data;
        /// <summary>
        /// The proxy that the user uses to control the SoundCue.
        /// </summary>
        private SoundCueProxy _currentProxy;

        /// <summary>
        /// Will start playing the cue.
        /// NOTE: It is called from SoundCueProxy that is created by the SoundController.
        /// </summary>
        public void Play(SoundCueData data)
        {
            _data = data;
            AudioObject.isDespawnOnFinishedPlaying = !data.isLooped;
            AudioObject.OnFinishedPlaying = OnFinishedPlaying_handler;
            // audioObject.isDespawnOnFinishedPlaying = false;
            if (TryPlayNext() == false)
            {
                return;
            }
            IsPlaying = true;
        }

        /// <summary>
        /// Plays the SoundCue.
        /// </summary>
        /// <param name="data">SoundCue's data</param>
        /// <param name="proxy">Proxy created by SoundController that called this method.</param>
        public void Play(SoundCueData data, SoundCueProxy proxy)
        {
            Play(data);
            _currentProxy = proxy;
        }

        /// <summary>
        /// Will pause the cue;
        /// </summary>
        public void Pause()
        {
            AudioObject.Pause();
        }

        /// <summary>
        /// Resume the cue from where it was paused.
        /// </summary>
        public void Resume()
        {
            AudioObject.Resume();
        }

        /// <summary>
        /// Stops the SoundCue.
        /// </summary>
        /// <param name="shouldCallOnFinishedCue">Checks whether to call OnEnd events, or not.</param>
        public void Stop(bool shouldCallOnFinishedCue = true)
        {
            if (IsPlaying == false)
                return;
            AudioObject.OnFinishedPlaying = null;
            // ((IPoolable)audioObject).pool.Despawn(audioObject.gameObject);
            AudioObject.Stop();
            AudioObject = null;
            IsPlaying = false;

            if (shouldCallOnFinishedCue)
            {
                if (OnPlayCueEnded != null)
                {
                    OnPlayCueEnded(this);
                }
            }

            if (OnPlayKilled != null)
            {
                OnPlayKilled(this, _currentProxy);
                _currentProxy = null;
            }
        }

        /// <summary>
        /// Internal event handler.
        /// </summary>
        /// <param name="obj"></param>
        private void OnFinishedPlaying_handler(SoundObject obj)
        {
            if (OnPlayEnded != null) {
                OnPlayEnded(Data.Name);
            }

            if (_data.isLooped)
            {
                if (TryPlayNext() == false)
                {
                    Stop(true);
                }
            }
            else
            {
                Stop(true);
            }
        }

        /// <summary>
        /// Tries to play the next SoundItem in SoundCue.
        /// </summary>
        /// <returns>True - can play, False - Cannot</returns>
        private bool TryPlayNext()
        {
            SoundItem item = _data.sounds[0];
            float itemVolume = item.isRandomVolume
                ? item.volumeRange.GetRandomRange()
                : item.volume;

            float realPitch = item.isRandomPitch
                ? item.pitchRange.GetRandomRange()
                : 1f;

            AudioObject.Setup(
                   item.name,
                   item.clips,
                   itemVolume,
                   _data.fadeInTime,
                   _data.fadeOutTime,
                   item.mixer,
                   realPitch);
            AudioObject.Play();
            return true;
        }
    }

    /// <summary>
    /// Used for sending data to play to AudioCue
    /// </summary>
    public struct SoundCueData
    {
        /// <summary>
        /// sound items that played by the SoundCue.
        /// </summary>
        public SoundItem[] sounds;
        /// <summary>
        /// Prefab with SoundObject to play Sound items.
        /// </summary>
        public GameObject audioPrefab;
        /// <summary>
        /// Fade In time.
        /// </summary>
        public float fadeInTime;
        /// <summary>
        /// Fade Out time.
        /// </summary>
        public float fadeOutTime;
        /// <summary>
        /// Should SoundCue Fade In?
        /// </summary>
        public bool isFadeIn;
        /// <summary>
        /// Should SoundCue Fade Out?
        /// </summary>
        public bool isFadeOut;

        /// <summary>
        /// Should SoundCue be looped?
        /// </summary>
        public bool isLooped;
        /// <summary>
        /// Audio Name
        /// </summary>
        public string Name;
    }
}