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
    public TwoArgs<float, bool> OnCountdownStarted;
    public NoArgs OnCountdownFinished;

    #endregion

    #region Game Events
    public NoArgs OnGameStart;
    public NoArgs OnGameEnd;

    #endregion

    #region Main Menu Events
    public OneArg<LobbyData> OnCreateLobbyRequest;
    public NoArgs OnQuitGame;
    #endregion

    #region Network Events

    #region Steam
    public NoArgs OnConnectedToSteam;
    public NoArgs OnSteamHostConnect;
    public NoArgs OnSteamHostDisconnect;
    public NoArgs OnSteamClientConnect;
    public NoArgs OnSteamClientDisconnect;
    public OneArg<Lobby> OnSteamLobbyCreated;
    #endregion

    #region Unity
    #region Host
    public NoArgs OnStartUnityHost;
    public NoArgs OnStopUnityHost;
    #endregion

    #region Client
    public NoArgs OnStopUnityClient;
    public NoArgs OnStartUnityClient;
    public NoArgs OnUnityClientDisconnected;
    public OneArg<string> OnQuestionSelected;
    #endregion
    #endregion

    #endregion

}