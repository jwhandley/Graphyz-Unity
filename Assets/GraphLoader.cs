using System;
using Unity.Mathematics;

namespace GraphLoader
{
    [Serializable]
    struct Node
    {
        public uint id;
        public float2 position;
        public float2 lastPosition;
        public float2 acceleration;
        public uint inDegree;
        public uint outDegree;
    }

    [Serializable]
    struct Link
    {
        public uint source;
        public uint target;
    }

    [Serializable]
    struct Graph
    {
        public Node[] nodes;
        public Link[] links;
    }
}
