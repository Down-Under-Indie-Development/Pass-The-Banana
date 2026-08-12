using Utility;
using UnityEngine;
using Steamworks.Data;

public class EventManager : Singleton<EventManager>
{
    public delegate void NoArgs();
    public delegate void OneArg<T1>(T1 t1);
    public delegate void TwoArgs<T1, T2>(T1 t1, T2 t2);
    public delegate void ThreeArgs<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
    public delegate void FourArgs<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);

    public NoArgs OnQuestionFinished;

    #region Countdown Events
    public NoArgs OnCountdownStarted;
    public NoArgs OnCountdownFinished;

    #endregion

    #region Game Events
    public NoArgs OnGameStart;
    public NoArgs OnGameEnd;

    #endregion

    #region Main Menu Events
    public NoArgs OnHostGame;
    public OneArg<int> OnCreateLobbyRequest;
    public NoArgs OnJoinGame;
    public NoArgs OnQuitGame;
    #endregion

    #region Network Events

    #region Steam
    public NoArgs OnConnectedToSteam;
    public OneArg<Lobby> OnLobbyCreated;
    public NoArgs OnClientDisconnect;
    public NoArgs OnClientConnect;
    public NoArgs OnStartHost;
    public NoArgs OnStartClient;

    #endregion

    #region Unity
    #endregion

    #endregion

}