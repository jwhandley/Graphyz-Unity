// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Instanced/Link"
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
                uint degree;
            };

            struct Link
            {
                uint source;
                uint target;
            };

            StructuredBuffer<Node> Nodes;
            StructuredBuffer<Link> Links;
            float _Thickness;

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                Link link = Links[instanceID];

                float2 sourcePos = Nodes[link.source].position;
                float2 targetPos = Nodes[link.target].position;

                // Calculate the midpoint
                float2 midpoint = (sourcePos + targetPos) * 0.5;

                // Calculate the direction vector
                float2 direction = targetPos - sourcePos;
                float len = length(direction);
                float2 normalizedDirection = normalize(direction);

                // Calculate the angle
                float angle = atan2(normalizedDirection.y, normalizedDirection.x);

                // Rotate and scale the vertex
                float4x4 translation = float4x4(1, 0, 0, midpoint.x,
                                                0, 1, 0, midpoint.y,
                                                0, 0, 1, 0,
                                                0, 0, 0, 1);

                float4x4 rotation = float4x4(cos(angle), -sin(angle), 0, 0,
                                             sin(angle), cos(angle), 0, 0,
                                             0, 0, 1, 0,
                                             0, 0, 0, 1);
                
                float4x4 scaling = float4x4(len, 0, 0, 0,
                                            0, _Thickness, 0, 0,
                                            0, 0, 1, 0,
                                            0, 0, 0, 1);

                float4x4 transform = mul(translation, mul(rotation, scaling));

                // Apply the transformation
                o.pos = UnityObjectToClipPos(mul(transform, v.vertex));
                o.uv = v.texcoord;
                o.colour = float3(0.5, 0.5, 0.5);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
				float3 colour = i.colour;
				return float4(colour, 1);
            }
            ENDCG
        }
    }
}