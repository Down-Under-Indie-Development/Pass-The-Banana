using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Doc.Networking.Data
{
    public class PlayerUIInfo : MonoBehaviour
    {
        [HideInInspector] public string playerName;
        [HideInInspector] public string playerPing;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _playerNameTxt;
        [SerializeField] private TextMeshProUGUI _playerPingTxt;

        private void Start()
        {
            if (_playerNameTxt != null) _playerNameTxt.text = playerName;
            if (_playerPingTxt != null) _playerPingTxt.text = playerPing;

        }

    }
}