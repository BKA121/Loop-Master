Shader "Custom/Mask2"
{
    Properties
    {
         _Stencil_Order ("Stencil Order", Int) = 10
    }
    SubShader
    {
        Tags { "Queue"="Geometry-1" }
        ColorMask 0
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref [_Stencil_Order]
                Comp Always
                Pass Replace
            }
        }
    }
}
