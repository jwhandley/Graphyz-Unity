using UnityEngine;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using System;
using static GraphLoader;


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
    [Range(0, 5)]
    public float repulsionStrength;
    [Range(0, 5)]
    public float attractionStrength;
    [Range(0, 5)]
    public float gravity;

    [Range(0, 1)]
    public float damping;

    private TextAsset _JsonGraph;

    Bounds bounds;
    ComputeBuffer nodeBuffer;
    ComputeBuffer adjacencyBuffer;
    ComputeBuffer offsetsBuffer;
    ComputeBuffer linkBuffer;
    Material nodeMaterial;
    Material linkMaterial;
    int nodeCount;
    int linkCount;
    int threadGroupsNodes;

    int nodeForce;

    bool needsUpdate;

    void Start()
    {
        PrepareData();
        SetData();
    }

    void OnValidate()
    {
        if (JsonGraph != _JsonGraph)
        {
            needsUpdate = true;
            _JsonGraph = JsonGraph;
        }

        if (vSync)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = int.MaxValue;
        }
    }

    void PrepareData()
    {
        var graph = new Graph(JsonGraph);
        nodeCount = graph.nodeCount();
        linkCount = graph.linkCount();

        uint[] adjacency = graph.adjacency;
        uint[] offsets = graph.offsets;


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
        }

        // Buffers for nodes and links
        nodeBuffer = new ComputeBuffer(nodeCount, Marshal.SizeOf(typeof(Node)));
        nodeBuffer.SetData(graph.nodes);

        linkBuffer = new ComputeBuffer(linkCount, Marshal.SizeOf(typeof(Link)));
        linkBuffer.SetData(graph.links);

        adjacencyBuffer = new ComputeBuffer(adjacency.GetLength(0), Marshal.SizeOf(typeof(uint)));
        adjacencyBuffer.SetData(adjacency);

        offsetsBuffer = new ComputeBuffer(offsets.GetLength(0), Marshal.SizeOf(typeof(uint)));
        offsetsBuffer.SetData(offsets);
    }

    void SetData()
    {
        if (nodeMaterial != null) DestroyImmediate(nodeMaterial);
        if (linkMaterial != null) DestroyImmediate(linkMaterial);

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
        forces.SetInt("nodeCount", nodeCount);
        forces.SetInt("linkCount", linkCount);
        uint xSizeNodes;
        forces.GetKernelThreadGroupSizes(nodeForce, out xSizeNodes, out _, out _);
        threadGroupsNodes = (int)Math.Ceiling(nodeCount / (float)xSizeNodes);

        forces.SetBuffer(nodeForce, "Nodes", nodeBuffer);
        forces.SetBuffer(nodeForce, "Adjacency", adjacencyBuffer);
        forces.SetBuffer(nodeForce, "Offsets", offsetsBuffer);
    }

    void Update()
    {
        if (needsUpdate)
        {
            ReleaseBuffers();
            PrepareData();
            SetData();
            needsUpdate = false;
        }

        forces.SetFloat("repulsionStrength", repulsionStrength);
        forces.SetFloat("attractionStrength", attractionStrength);
        forces.SetFloat("damping", damping);
        forces.SetFloat("gravity", gravity);
        forces.SetFloat("deltaTime", Time.deltaTime);
        forces.Dispatch(nodeForce, threadGroupsNodes, 1, 1);

        nodeMaterial.SetFloat("_Radius", nodeSize);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, linkMaterial, bounds, linkCount);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, nodeMaterial, bounds, nodeCount);
    }

    void ReleaseBuffers()
    {
        nodeBuffer?.Release();
        linkBuffer?.Release();
        adjacencyBuffer?.Release();
        offsetsBuffer?.Release();

        nodeBuffer = null;
        linkBuffer = null;
        adjacencyBuffer = null;
        offsetsBuffer = null;
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }
}