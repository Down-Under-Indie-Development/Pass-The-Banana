using Utility;
using UnityEngine;


public class Timer : Singleton<Timer>
{

    [SerializeField] public float time = 10f;
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

    private void Start()
    {
        _currentTime = time;

    }

    private void Update()
    {
        if (!_counting) return;
        if (_currentTime <= 0)
        {
            _eventManager?.OnCountdownFinished?.Invoke();
            _counting = false;

        }

        _currentTime -= Time.deltaTime;



    }

    // INFO: Get current time
    public float GetCurrentTime() => Mathf.Round(_currentTime);

    #region Timer
    public void ResetCountdown()
    {
        _currentTime = time;

    }

    public void StartCountdown()
    {

        _counting = true;
        if (_debug) Debug.Log($"Timer started");

    }


    public void StopCountdown()
    {
        _counting = false;
        if (_debug) Debug.Log($"Countdown paused at: {_currentTime}");

    }
    #endregion

}