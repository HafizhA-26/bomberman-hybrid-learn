using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL
{
    public class AudioHandler : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource _bgmAudioSource;
        [SerializeField] private AudioSource _sfxAudioSource;
        [Header("Audio Data")]
        [SerializeField] private List<AudioClip> _bgmSounds = new List<AudioClip>();
        [SerializeField] private List<AudioClip> _sfxSounds = new List<AudioClip>();

        private bool _isMuteBGM = false;
        private bool _isMuteSFX = false;

        private readonly Dictionary<string, AudioClip> _bgmDict = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _sfxDict = new Dictionary<string, AudioClip>();

        public bool IsMuteBGM { get => _isMuteBGM; }
        public bool IsMuteSFX { get => _isMuteSFX; }

        public event Action<bool> OnBGMMute;
        public event Action<bool> OnSFXMute;

        private void Awake()
        {
            foreach (AudioClip item in _bgmSounds)
            {
                _bgmDict[item.name] = item;
            }

            foreach (AudioClip item in _sfxSounds)
            {
                _sfxDict[item.name] = item;
            }

            _isMuteBGM = PlayerPrefs.GetInt("MuteBGM", 0) == 1;
            _isMuteSFX = PlayerPrefs.GetInt("MuteSFX", 0) == 1;
        }

        public void PlayBGM(string name)
        {
            if (!_bgmDict.ContainsKey(name))
            {
                Debug.LogWarning("Can't find BGM named " + name);
                return;
            }
            _bgmAudioSource.clip = _bgmDict[name];
            _bgmAudioSource.Play();
        }

        public void PlaySFX(string name, bool oneShot = true)
        {
            if (!_sfxDict.ContainsKey(name))
            {
                Debug.LogWarning("Can't find SFX named " + name);
                return;
            }
            if(oneShot) _sfxAudioSource.PlayOneShot(_sfxDict[name]);
            else
            {
                _sfxAudioSource.clip = _sfxDict[name];
                _sfxAudioSource.Play();
            }
        }

        public bool ToggleMuteBGM()
        {
            MuteBGM(!_isMuteBGM);
            return _isMuteBGM;
        }
        public bool ToggleMuteSFX()
        {
            MuteSFX(!_isMuteSFX);
            return _isMuteSFX;
        }

        public void MuteBGM(bool mute)
        {
            _isMuteBGM = mute;
            _bgmAudioSource.mute = mute;
            OnBGMMute?.Invoke(_isMuteBGM);
        }

        public void MuteSFX(bool mute)
        {
            _isMuteSFX = mute;
            _sfxAudioSource.mute = mute;
            OnSFXMute?.Invoke(_isMuteSFX);
        }
    }
}