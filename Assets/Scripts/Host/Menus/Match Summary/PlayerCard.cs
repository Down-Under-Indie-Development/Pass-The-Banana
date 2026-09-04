
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using PTB.Enums;

namespace PTB.Menus
{
    public class PlayerCard : MonoBehaviour
    {
        [Header("Text Boxes")]
        [SerializeField] private TextMeshProUGUI _placeTxt;
        [SerializeField] private TextMeshProUGUI _playerNameTxt;
        [SerializeField] private TextMeshProUGUI _passesTxt;
        [SerializeField] private TextMeshProUGUI _failsTxt;
        [SerializeField] private TextMeshProUGUI _scoreTxt;

        private string GetPlaceOrdinal(int place)
        {
            return place switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => $"{place}th"
            };
        }

        public void SetPlayerCardStats(ScoreData scoreData, ulong clientId, int playerPlace)
        {
            if (_placeTxt == null || _playerNameTxt == null || _passesTxt == null || _failsTxt == null || _scoreTxt == null) { Debug.LogError($"One or more text object are null!"); return; }

            string name = SteamClient.IsValid ? SteamClient.Name : clientId.ToString();
            string place = GetPlaceOrdinal(playerPlace);

            _placeTxt.text = place;
            _playerNameTxt.text = name;
            _passesTxt.text = scoreData.passes.ToString();
            _failsTxt.text = scoreData.fails.ToString();
            _scoreTxt.text = scoreData.points.ToString();

        }
    }
}