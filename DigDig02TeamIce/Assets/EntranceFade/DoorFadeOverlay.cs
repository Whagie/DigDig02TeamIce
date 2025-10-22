using System;
using UnityEngine;

[ExecuteAlways]
public class DoorFadeOverlay : MonoBehaviour
{
    public Material fadeMaterial; // Assign your DoorStencilReader material here
    [Range(0, 1)] public float fade = 1f;
    public Color fadeColor = Color.black;

    void OnRenderObject()
    {
        if (!fadeMaterial) return;

        if (Application.isPlaying != true)
            return;

        fadeMaterial.SetColor("_FadeColor", fadeColor);
        fadeMaterial.SetFloat("_Fade", fade);

        fadeMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadOrtho(); // full screen quad in normalized space

        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
        GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
        GL.End();

        GL.PopMatrix();
    }
}
