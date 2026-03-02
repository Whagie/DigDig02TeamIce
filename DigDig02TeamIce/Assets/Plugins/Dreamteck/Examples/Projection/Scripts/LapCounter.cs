namespace Dreamteck.Splines.Examples
{
    using UnityEngine;

    public class LapCounter : MonoBehaviour
    {
        int currentLap;
        public TextMesh text;

        public void CountLap()
        {
            currentLap++;
            text.text = "LAP " + currentLap;
        }
    }
}
