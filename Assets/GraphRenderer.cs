using UnityEngine;
using Unity.Mathematics;
using GraphLoader;
using System.Runtime.InteropServices;
using System;


public class ParticleRenderer : MonoBehaviour
{
    [Header("Mesh")]
    public Mesh mesh;

    [Header("Shaders")]
    public Shader nodeShader;
    public Shader linkShader;
    public ComputeShader forces;

    [Header("Render parameters")]
    [Range(0, 1)]
    public float nodeSize = 0.1f;
    [Range(0, 0.5f)]
    public float linkThickness = 0.05f;
    public bool vSync;

    [Header("Graph")]
    public TextAsset JsonGraph;

    [Header("Simulation parameters")]
    [Range(0, 0.1f)]
    public float repulsionStrength;

    [Range(0.99f, 1)]
    public float damping;

    [Range(0, 0.1f)]
    public float minLength;

    [Range(0, 0.1f)]
    public float minDistance;

    [Range(0, 1)]
    public float gravity;

    Bounds bounds;
    ComputeBuffer nodeBuffer;
    ComputeBuffer inAdjacencyBuffer;
    ComputeBuffer inOffsetsBuffer;
    ComputeBuffer outAdjacencyBuffer;
    ComputeBuffer outOffsetsBuffer;
    ComputeBuffer linkBuffer;
    Material nodeMaterial;
    Material linkMaterial;
    int nodeCount;
    int linkCount;
    int threadGroupsNodes;

    int nodeForce;
    int integration;

    void Start()
    {
        if (vSync)
        {
            QualitySettings.vSyncCount = 1;
        }

        PrepareData();
        SetData();
    }

    void PrepareData()
    {
        Graph graph = JsonUtility.FromJson<Graph>(JsonGraph.text);
        nodeCount = graph.nodeCount();
        linkCount = graph.linkCount();

        uint[] inAdjacency = graph.inAdjacency();
        uint[] outAdjacency = graph.outAdjacency();
        uint[] inOffsets = graph.inOffsets();
        uint[] outOffsets = graph.outOffsets();

        // Initial node positions from Fibonacci spiral
        float goldenAngle = Mathf.PI * (3 - Mathf.Sqrt(5));
        for (int i = 0; i < nodeCount; ++i)
        {
            float radius = Mathf.Sqrt(i) * nodeSize;
            float angle = i * goldenAngle;

            float x = radius * Mathf.Cos(angle) + UnityEngine.Random.Range(-0.1f, 0.1f);
            float y = radius * Mathf.Sin(angle) + UnityEngine.Random.Range(-0.1f, 0.1f);

            float2 pos = new float2(x, y);
            graph.nodes[i].position = pos;
            graph.nodes[i].lastPosition = pos;
        }

        // Buffers for nodes and links
        nodeBuffer = new ComputeBuffer(nodeCount, Marshal.SizeOf(typeof(Node)));
        nodeBuffer.SetData(graph.nodes);

        linkBuffer = new ComputeBuffer(linkCount, Marshal.SizeOf(typeof(Link)));
        linkBuffer.SetData(graph.links);

        inAdjacencyBuffer = new ComputeBuffer(inAdjacency.GetLength(0), Marshal.SizeOf(typeof(uint)));
        inAdjacencyBuffer.SetData(inAdjacency);

        inOffsetsBuffer = new ComputeBuffer(inOffsets.GetLength(0), Marshal.SizeOf(typeof(uint)));
        inOffsetsBuffer.SetData(inOffsets);

        outAdjacencyBuffer = new ComputeBuffer(outAdjacency.GetLength(0), Marshal.SizeOf(typeof(uint)));
        outAdjacencyBuffer.SetData(outAdjacency);

        outOffsetsBuffer = new ComputeBuffer(outOffsets.GetLength(0), Marshal.SizeOf(typeof(uint)));
        outOffsetsBuffer.SetData(outOffsets);
    }

    void SetData()
    {
        // Material for nodes
        nodeMaterial = new Material(nodeShader);
        nodeMaterial.SetBuffer("Nodes", nodeBuffer);
        nodeMaterial.SetFloat("_Radius", nodeSize);

        // Material for links
        linkMaterial = new Material(linkShader);
        linkMaterial.SetBuffer("Nodes", nodeBuffer);
        linkMaterial.SetBuffer("Links", linkBuffer);
        linkMaterial.SetFloat("_Thickness", linkThickness);
        linkMaterial.renderQueue = 2000; // Ensure links draw before nodes

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        nodeForce = forces.FindKernel("NodeForce");
        integration = forces.FindKernel("Integration");


        forces.SetInt("nodeCount", nodeCount);
        uint xSizeNodes;
        forces.GetKernelThreadGroupSizes(nodeForce, out xSizeNodes, out _, out _);
        threadGroupsNodes = (int)Math.Ceiling(nodeCount / (float)xSizeNodes);

        forces.SetBuffer(nodeForce, "Nodes", nodeBuffer);
        forces.SetBuffer(nodeForce, "InAdjacency", inAdjacencyBuffer);
        forces.SetBuffer(nodeForce, "InOffsets", inOffsetsBuffer);
        forces.SetBuffer(nodeForce, "OutAdjacency", outAdjacencyBuffer);
        forces.SetBuffer(nodeForce, "OutOffsets", outOffsetsBuffer);

        forces.SetFloat("repulsionStrength", repulsionStrength);
        forces.SetFloat("damping", damping);
        forces.SetFloat("minDistance", minDistance);
        forces.SetFloat("gravity", gravity);
        forces.SetInt("linkCount", linkCount);
        forces.SetFloat("minLength", minLength);

        forces.SetBuffer(integration, "Nodes", nodeBuffer);
    }

    void Update()
    {
        forces.SetFloat("deltaTime", Time.deltaTime);
        forces.Dispatch(nodeForce, threadGroupsNodes, 1, 1);
        forces.Dispatch(integration, threadGroupsNodes, 1, 1);

        Graphics.DrawMeshInstancedProcedural(mesh, 0, linkMaterial, bounds, linkCount);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, nodeMaterial, bounds, nodeCount);
    }

    void OnDestroy()
    {
        nodeBuffer?.Release();
        linkBuffer?.Release();
        inAdjacencyBuffer?.Release();
        outAdjacencyBuffer?.Release();
        inOffsetsBuffer?.Release();
        outOffsetsBuffer?.Release();
    }
}