using Utility;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource _audioSource;

    public void PlayerAudio(AudioClip audioClip, float volume = 1f)
    {
        if (audioClip == null) { Debug.LogWarning($"Audio Clip is null!"); return; }
        _audioSource?.PlayOneShot(audioClip, volume);

    }

}