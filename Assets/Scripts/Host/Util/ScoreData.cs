using System;
using UnityEngine;

namespace PTB.Enums
{
    [Serializable]
    public class ScoreData
    {
        public int correctGuesses;
        public int passes;
        public int incorrectGuesses;

        public static ScoreData Empty() => new();

        public override string ToString() => $"Points: {correctGuesses} | Passes: {passes} | Fails: {incorrectGuesses}";
    }
}