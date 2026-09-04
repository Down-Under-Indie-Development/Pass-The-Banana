using Utility;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;


public class Timer : NetworkedSingleton<Timer>
{

    [Header("Audio Settings")]
    [SerializeField] private List<AudioClip> _timerCountdownSFX;

    private float _currentTime;
    private bool _counting;
    private bool showCountdown;

    #region Events
    private void OnEnable()
    {
        _eventManager.OnCountdownStarted += StartCountdown;

    }

    private void OnDisable()
    {
        _eventManager.OnCountdownStarted -= StartCountdown;

    }
    #endregion

    private float _lastLoggedSecond = -1f;
    private void Update()
    {
        if (!_counting) return;

        // INFO: Only play audio when the second changes
        float roundedTime = Mathf.Round(_currentTime);

        // INFO: SFX
        if (roundedTime != _lastLoggedSecond) TellClientPlayerAudioRPC();

        if (_currentTime <= 0)
        {
            Debug.Log($"Timer finished");
            _eventManager.OnCountdownFinished?.Invoke();
            _counting = false;
            return;
        }

        _currentTime -= Time.deltaTime;

        if (showCountdown && roundedTime != _lastLoggedSecond) Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[TIMER]</color> {_lastLoggedSecond} second(s) remaining"); ;
        _lastLoggedSecond = roundedTime;

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TellClientPlayerAudioRPC()
    {
        AudioManager.Instance.PlayAudio(_timerCountdownSFX[0]);

    }

    // INFO: Get current time
    public float GetCurrentTime() => Mathf.Round(_currentTime);

    #region Timer
    public void StartCountdown(float time, bool displayCountdown)
    {
        Debug.Log($"<color={LogColours.Unity}>[TIMER]</color> Countdown started: {time}");
        _currentTime = time;
        _lastLoggedSecond = Mathf.Round(time--);
        _counting = true;
        showCountdown = displayCountdown;


    }


    public void StopCountdown()
    {
        if (_counting != false) Debug.Log($"<color={LogColours.Unity}>[TIMER]</color> Countdown manually stopped");
        _counting = false;
        _currentTime = 0;

    }
    #endregion

}