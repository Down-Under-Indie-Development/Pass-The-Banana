using Utility;
using UnityEngine;


public class Timer : Singleton<Timer>
{

    // [SerializeField] public float time = 10f;
    private float _currentTime;
    private bool _counting;

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

    }

    // INFO: Get current time
    public float GetCurrentTime() => Mathf.Round(_currentTime);

    #region Timer
    public void StartCountdown(float time)
    {
        _currentTime = time;
        _counting = true;
        Debug.Log($"<color={LogColours.Unity}>[TIMER]</color> Countdown started: {time}");

    }


    public void StopCountdown()
    {
        _counting = false;
        Debug.Log($"<color={LogColours.Unity}>[TIMER]</color> Countdown manually stopped");
        _currentTime = 0;

    }
    #endregion

}