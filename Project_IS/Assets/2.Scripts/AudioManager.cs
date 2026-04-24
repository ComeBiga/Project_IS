using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public struct AudioClipInfo
    {
        public string name;
        public AudioClip audioClip;
        public bool loop;
        public float volume;
        public float pitch;
        public float spatialBlend;
    }

    public static AudioManager instance = null;

    [SerializeField] private AudioSource[] _audioSources;
    [SerializeField] private AudioClipInfo[] _audioClipInfos;

    [Header("BGM Clip")]
    [SerializeField] private AudioClip _bgmClip;

    [Header("Character Clips")]
    [SerializeField] private AnimationEventReceiver _animationEventReceiver;
    [SerializeField] private AudioClip _footStepClip;
    [SerializeField] private AudioClip _handTouchClip;
    [SerializeField] public AudioClip _handTouchLadderClip;
    [SerializeField] public AudioClip _jumpVoiceClip;
    [SerializeField] public AudioClip _craneClip;

    private Dictionary<string, AudioSource> mAudioSourceDict = new Dictionary<string, AudioSource>();

    public void PlayOneShot(string clipName)
    {
        AudioClipInfo audioClipInfo = getAudioClipInfo(clipName);
        AudioSource audioSource = getAudioSource();
        // Debug.Log(audioSource.isPlaying);

        audioSource.clip = audioClipInfo.audioClip;
        audioSource.loop = audioClipInfo.loop;
        audioSource.volume = audioClipInfo.volume;
        audioSource.pitch = audioClipInfo.pitch;
        audioSource.spatialBlend = audioClipInfo.spatialBlend;

        audioSource.PlayOneShot(audioClipInfo.audioClip);
    }

    public void PlayOneShot(string clipName, float volume)
    {
        AudioClipInfo audioClipInfo = getAudioClipInfo(clipName);
        AudioSource audioSource = getAudioSource();
        // Debug.Log(audioSource.isPlaying);

        audioSource.clip = audioClipInfo.audioClip;
        audioSource.loop = audioClipInfo.loop;
        audioSource.volume = volume;
        audioSource.pitch = audioClipInfo.pitch;
        audioSource.spatialBlend = audioClipInfo.spatialBlend;

        audioSource.PlayOneShot(audioClipInfo.audioClip);
    }

    [Obsolete]
    public void PlayOneShot(AudioClip audioClip)
    {
        // AudioSource audioSource = getAudioSource();
        AudioSource audioSource = _audioSources[0];
        // Debug.Log(audioSource.isPlaying);

        audioSource.PlayOneShot(audioClip);
    }

    [Obsolete]
    public void PlayOneShot(int audioSourceIndex, AudioClip audioClip)
    {
        // AudioSource audioSource = getAudioSource();
        AudioSource audioSource = _audioSources[audioSourceIndex];
        // Debug.Log(audioSource.isPlaying);

        audioSource.PlayOneShot(audioClip);
    }

    public void Play(string clipName)
    {
        AudioClipInfo audioClipInfo = getAudioClipInfo(clipName);
        AudioSource audioSource = getAudioSource();

        audioSource.clip = audioClipInfo.audioClip;
        audioSource.loop = audioClipInfo.loop;
        audioSource.volume = audioClipInfo.volume;
        audioSource.pitch = audioClipInfo.pitch;
        audioSource.spatialBlend = audioClipInfo.spatialBlend;

        audioSource.Play();

        if(audioClipInfo.loop)
        {
            mAudioSourceDict.Add(clipName, audioSource);
        }
    }

    public void Play(string clipName, float volume)
    {
        AudioClipInfo audioClipInfo = getAudioClipInfo(clipName);
        AudioSource audioSource = getAudioSource();

        audioSource.clip = audioClipInfo.audioClip;
        audioSource.loop = audioClipInfo.loop;
        audioSource.volume = volume;
        audioSource.pitch = audioClipInfo.pitch;
        audioSource.spatialBlend = audioClipInfo.spatialBlend;

        audioSource.Play();

        if(audioClipInfo.loop)
        {
            mAudioSourceDict.Add(clipName, audioSource);
        }
    }

    [Obsolete]
    public void Play(AudioClip audioClip)
    {
        AudioSource audioSource = getAudioSource();

        audioSource.clip = audioClip;
        audioSource.Play();
    }

    [Obsolete]
    public void PlayLoop(AudioClip audioClip)
    {
        AudioSource audioSource = _audioSources[4];
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public bool Stop(string clipName)
    {
        if(mAudioSourceDict.Remove(clipName, out AudioSource audioSource))
        {
            audioSource.Stop();

            return true;
        }

        return false;
    }

    [Obsolete]
    public void StopLoop()
    {
        AudioSource audioSource = _audioSources[4];
        audioSource.Stop();
        audioSource.loop = false;
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        // Play("BackgroundNoise");
    }

    private AudioSource getAudioSource()
    {
        for (int i = 0; i < _audioSources.Length; ++i)
        {
            if (!_audioSources[i].isPlaying)
                return _audioSources[i];
        }

        return _audioSources[0];
    }

    private AudioClipInfo getAudioClipInfo(string clipName)
    {
        for (int i = 0; i < _audioClipInfos.Length; ++i)
        {
            if (_audioClipInfos[i].name == clipName)
                return _audioClipInfos[i];
        }

        return new AudioClipInfo();
    }
}
