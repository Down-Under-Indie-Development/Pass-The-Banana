using Utility;
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource _audioSource;

    public void PlayAudio(AudioClip audioClip, float volume = 1f)
    {
        if (audioClip == null) { Debug.LogWarning($"Audio Clip is null!"); return; }
        _audioSource?.PlayOneShot(audioClip, volume);

    }

    public void PlayerRandomClip(List<AudioClip> audioClips)
    {

        if (audioClips == null || audioClips.Count <= 0) { Debug.LogWarning($"No audio clips provided, cannot play!"); return; }

        int rnd = audioClips.Count > 1 ? Random.Range(0, audioClips.Count - 1) : 0;
        PlayAudio(audioClips[rnd]);

    }

}