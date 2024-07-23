Shader "Instanced/Node"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 colour : TEXCOORD1;
            };

            struct Node
            {
                uint id;
                float2 position;
                float2 velocity;
                float2 acceleration;
                uint inDegree;
                uint outDegree;
            };

            StructuredBuffer<Node> Nodes;
            float _Radius;

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                Node node = Nodes[instanceID];
                float2 position = node.position;
                uint degree = node.outDegree + node.inDegree;
                float3 centreWorld = float3(position, 0);
				float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex * _Radius * sqrt(degree));
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

				v2f o;
				o.uv = v.texcoord;
				o.pos = UnityObjectToClipPos(objectVertPos);
                o.colour = float3(0, 0.25, 0.75);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 centreOffset = (i.uv.xy - 0.5) * 2;
				float sqrDst = dot(centreOffset, centreOffset);
				float delta = fwidth(sqrt(sqrDst));
				float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

				float3 colour = i.colour;
				return float4(colour, alpha);
            }
            ENDCG
        }
    }
}