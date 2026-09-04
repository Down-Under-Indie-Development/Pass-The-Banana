using System;
using UnityEngine;

namespace PTB.Enums
{
    [Serializable]
    public class ScoreData
    {
        public int points;
        public int passes;
        public int fails;

        public static ScoreData Empty() => new();

        public override string ToString() => $"Points: {points} | Passes: {passes} | Fails: {fails}";
    }
}