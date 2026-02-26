Shader "Custom/DoorStencilWriter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }

        Pass
        {
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            ColorMask 0  // Don’t draw any color, just stencil
        }
    }
}
