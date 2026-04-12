using TMPro;
using UnityEngine;

public class DialogueData : MonoBehaviour
{
    [TextArea]
    public string text;

    public float fontSize = 10f;
    public Color color = Color.black;

    public TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft;
}
