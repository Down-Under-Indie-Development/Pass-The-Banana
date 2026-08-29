using Utility;
using UnityEngine;


public class Timer : Singleton<Timer>
{

    // [SerializeField] public float time = 10f;
    private float _currentTime;
    private bool _counting;
    public bool showCountdown;

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
        if (_currentTime <= 0)
        {
            Debug.Log($"Timer finished");
            _eventManager.OnCountdownFinished?.Invoke();
            _counting = false;
            return;
        }

        _currentTime -= Time.deltaTime;
        if (showCountdown) DisplayCountdown(_currentTime);

    }

    private void DisplayCountdown(float currentTime)
    {
        float roundedTime = Mathf.Round(currentTime);

        if (roundedTime == _lastLoggedSecond) return;
        Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[TIMER]</color> {roundedTime} second(s) remaining");
        _lastLoggedSecond = roundedTime;
    }

    // INFO: Get current time
    public float GetCurrentTime() => Mathf.Round(_currentTime);

    #region Timer
    public void StartCountdown(float time, bool displayCountdown)
    {
        _currentTime = time;
        _counting = true;
        showCountdown = displayCountdown;

        Debug.Log($"<color={LogColours.Unity}>[TIMER]</color> Countdown started: {time}");

    }


    public void StopCountdown()
    {
        if (_counting != false) Debug.Log($"<color={LogColours.Unity}>[TIMER]</color> Countdown manually stopped");
        _counting = false;
        _currentTime = 0;

    }
    #endregion

}