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
        public float2 velocity;
        public uint degree;

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

        public uint[] adjacency()
        {
            var adjacency = new List<List<uint>>();
            for (int i = 0; i < nodeCount(); ++i)
            {
                adjacency.Add(new List<uint>());
            }

            foreach (Link link in links)
            {
                nodes[link.target].degree++;
                nodes[link.source].degree++;
                adjacency[(int)link.target].Add(link.source);
                adjacency[(int)link.source].Add(link.target);
            }

            return adjacency.SelectMany(i => i).ToArray();
        }


        public uint[] offsets()
        {
            var offsets = new uint[nodeCount()];

            offsets[0] = 0;
            for (int i = 1; i < nodeCount(); ++i)
            {
                offsets[i] = offsets[i - 1] + nodes[i - 1].degree;
            }

            return offsets;
        }
    }
}
