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
        [SerializeField] private TextMeshProUGUI _pointsTxt;

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

        public void SetPlayerCardStats(string name, ScoreData scoreData, int playerPlace)
        {
            string place = GetPlaceOrdinal(playerPlace);

            if (_placeTxt != null) _placeTxt.text = place;
            if (_playerNameTxt != null) _playerNameTxt.text = name;
            if (_passesTxt != null) _passesTxt.text = scoreData.passes.ToString();
            if (_failsTxt != null) _failsTxt.text = scoreData.incorrectGuesses.ToString();
            if (_pointsTxt != null) _pointsTxt.text = scoreData.correctGuesses.ToString();

        }
    }
}