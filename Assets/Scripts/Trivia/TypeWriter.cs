using System.Collections;
using TMPro;
using UnityEngine;
using Utility;

public class TypeWriter : Singleton<TypeWriter>
{
    public void WriteText(string textToDisplay, TextMeshProUGUI textBox, float timeDelay = 0f)
    {
        StartCoroutine(DelayedText(textToDisplay, textBox, timeDelay));
    }

    private IEnumerator DelayedText(string textToDisplay, TextMeshProUGUI textBox, float timeDelay = 0f)
    {
        yield return new WaitForSecondsRealtime(5f);
        textBox.text = "";
        for (int i = 0; i < textToDisplay.Length; i++)
        {
            textBox.text += textToDisplay[i];
            yield return new WaitForSecondsRealtime(timeDelay);

        }

    }

}