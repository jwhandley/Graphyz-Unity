using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class GraphLoader
{
    [Serializable]
    public struct Node
    {
        public uint id;
        public float2 position;
        public float2 velocity;
        public uint degree;

    }

    [Serializable]
    public struct Link
    {
        public uint source;
        public uint target;
    }

    [Serializable]
    public struct Graph
    {
        public Node[] nodes;
        public Link[] links;

        public uint[] adjacency;
        public uint[] offsets;

        public Graph(TextAsset file)
        {
            this = JsonUtility.FromJson<Graph>(file.text);
            getAdjacency();
            getOffsets();
        }

        public int nodeCount()
        {
            return nodes.Length;
        }

        public int linkCount()
        {
            return links.Length;
        }

        void getAdjacency()
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

            this.adjacency = adjacency.SelectMany(i => i).ToArray();
        }


        void getOffsets()
        {
            var offsets = new uint[nodeCount()];

            offsets[0] = 0;
            for (int i = 1; i < nodeCount(); ++i)
            {
                offsets[i] = offsets[i - 1] + nodes[i - 1].degree;
            }

            this.offsets = offsets;
        }
    }
}
