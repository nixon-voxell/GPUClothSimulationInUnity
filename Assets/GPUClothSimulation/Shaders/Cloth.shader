Shader "Custom/Cloth"
{
  Properties
  {
    _Color ("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0
    _firstVertexIdx ("First Vertex ID", int) = 0
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    LOD 200

    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
    #pragma target 5.0

    #include "./DataStruct.cginc"

    // #ifdef SHADER_API_D3D11
    // StructuredBuffer<float3> pos;
    // #endif

    sampler2D _MainTex;
    half _Glossiness;
    half _Metallic;
    fixed4 _Color;
    int _firstVertexIdx;

    struct Input
    {
      float2 uv_MainTex;
    };

    struct appdata
    {
      float4 vertex : POSITION;
      float3 normal : NORMAL;
      float4 texcoord : TEXCOORD0;
      float4 texcoord1 : TEXCOORD1;
      float4 texcoord2 : TEXCOORD2;

      uint idx : SV_VertexID;
    };

    UNITY_INSTANCING_BUFFER_START(Props)
      // put more per-instance properties here
    UNITY_INSTANCING_BUFFER_END(Props)

    void vert(inout appdata v, out Input o)
    {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      #ifdef SHADER_API_D3D11
      v.vertex.xyz = pos[v.idx + _firstVertexIdx];
      #endif
    }

    void surf (Input IN, inout SurfaceOutputStandard o)
    {
      // Albedo comes from a texture tinted by color
      fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
      o.Albedo = c.rgb;
      // Metallic and smoothness come from slider variables
      o.Metallic = _Metallic;
      o.Smoothness = _Glossiness;
      o.Alpha = c.a;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
