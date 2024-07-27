using System;
using System.Collections.Generic;
using System.Linq;
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


        public int nodeCount()
        {
            return nodes.Length;
        }

        public int linkCount()
        {
            return links.Length;
        }

        public uint[] inAdjacency()
        {
            var inAdjacency = new List<List<uint>>();
            for (int i = 0; i < nodeCount(); ++i)
            {
                inAdjacency.Add(new List<uint>());
            }

            foreach (Link link in links)
            {
                nodes[link.target].inDegree++;
                inAdjacency[(int)link.target].Add(link.source);
            }

            return inAdjacency.SelectMany(i => i).ToArray();
        }

        public uint[] outAdjacency()
        {
            var outAdjacency = new List<List<uint>>();
            for (int i = 0; i < nodeCount(); ++i)
            {
                outAdjacency.Add(new List<uint>());
            }

            foreach (Link link in links)
            {
                nodes[link.source].outDegree++;
                outAdjacency[(int)link.source].Add(link.target);
            }

            return outAdjacency.SelectMany(i => i).ToArray();
        }

        public uint[] inOffsets()
        {
            var inOffsets = new uint[nodeCount()];

            inOffsets[0] = 0;
            for (int i = 1; i < nodeCount(); ++i)
            {
                inOffsets[i] = inOffsets[i - 1] + nodes[i - 1].inDegree;
            }

            return inOffsets;
        }

        public uint[] outOffsets()
        {
            var outOffsets = new uint[nodeCount()];

            outOffsets[0] = 0;
            for (int i = 1; i < nodeCount(); ++i)
            {
                outOffsets[i] = outOffsets[i - 1] + nodes[i - 1].outDegree;
            }

            return outOffsets;
        }
    }
}
