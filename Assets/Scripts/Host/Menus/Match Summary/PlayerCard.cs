using Steamworks;
using TMPro;
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

        public void SetPlayerCardStats(ScoreData scoreData, string clientId, int playerPlace, string steamName)
        {
            if (_placeTxt == null || _playerNameTxt == null || _passesTxt == null || _failsTxt == null || _scoreTxt == null) { Debug.LogError($"One or more text object are null!"); return; }
            string place = GetPlaceOrdinal(playerPlace);

            name = string.IsNullOrEmpty(steamName) ? clientId : steamName;

            _placeTxt.text = place;
            _playerNameTxt.text = name;
            _passesTxt.text = scoreData.passes.ToString();
            _failsTxt.text = scoreData.fails.ToString();
            _scoreTxt.text = scoreData.points.ToString();

        }
    }
}