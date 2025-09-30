using System;
using UnityEngine;

[ExecuteAlways]
public class FullscreenVignette : MonoBehaviour
{
    public Material vignetteMaterial;

    [Range(0f, 1f)] public float intensity = 0.5f;
    [Range(0f, 1f)] public float edgeWidth = 0.2f;
    [Range(0f, 1f)] public float fadeAmount = 0.25f;
    [Range(0.1f, 5f)] public float falloff = 1;
    [Range(0f, 1f)] public float cornerFade = 0.05f;
    [Range(0.1f, 2f)] public float widthRatio = 1f;
    [Range(0.1f, 2f)] public float heightRatio = 1f;
    public Color color = Color.black;

    private Mesh _quadMesh;

    void Awake()
    {
        // Create a simple fullscreen quad
        _quadMesh = new Mesh();
        _quadMesh.vertices = new Vector3[]
        {
            new Vector3(-1,-1,0),
            new Vector3(1,-1,0),
            new Vector3(1,1,0),
            new Vector3(-1,1,0)
        };
        _quadMesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1)
        };
        _quadMesh.triangles = new int[] { 0,1,2, 0,2,3 };
        _quadMesh.RecalculateBounds();
    }

    void OnRenderObject()
    {
        if (!vignetteMaterial) return;

        vignetteMaterial.SetFloat("_Intensity", intensity);
        vignetteMaterial.SetFloat("_EdgeWidth", edgeWidth);
        vignetteMaterial.SetFloat("_FadeAmount", fadeAmount);
        vignetteMaterial.SetFloat("_Falloff", falloff);
        vignetteMaterial.SetFloat("_CornerFade", cornerFade);
        vignetteMaterial.SetFloat("_WidthRatio", widthRatio);
        vignetteMaterial.SetFloat("_HeightRatio", heightRatio);
        vignetteMaterial.SetColor("_Color", color);

        vignetteMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadOrtho(); // Now coordinates 0..1 cover the screen

        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
        GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
        GL.End();

        GL.PopMatrix();
    }
}
