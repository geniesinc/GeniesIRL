using UnityEngine;
using System.Collections.Generic;
using Pathfinding;
using System;
using GeniesIRL.GlobalEvents;

namespace GeniesIRL
{
    /// <summary>
    /// Allows us to visualize the walkable space at runtime, including on-device, so we can debug potential issues with
    /// character navigation.
    /// </summary>
    [RequireComponent(typeof(ARNavigation))]
    public class AstarGridVisualizer : MonoBehaviour
    {
        public bool EnableVisualization = false;
        public GameObject AstarGridVisualizerQuad;
        public Material AstarVisualizerQuadMaterial;
        public Material WalkableNodeMaterial;
        public Material UnwalkableNodeMaterial;
        public float quadHeightOffset = 0.05f;

        private ARNavigation _navigation;
        private bool _needsInitializing = true;
        private Renderer _visualizationQuadRenderer;
        private Texture2D _visualizationQuadTexture;
        private bool _enableVisualizationLastFrame = false;
        private GameObject _visualizationQuad;

        private void Start()
        {
            _enableVisualizationLastFrame = EnableVisualization;
            _navigation = GetComponent<ARNavigation>();
            _navigation.OnScanComplete += OnScanComplete;

            GlobalEventManager.Subscribe<GlobalEvents.DebugShowNavGrid>(OnDebugShowNavGrid);
        }

        private void OnDebugShowNavGrid(DebugShowNavGrid args)
        {
            EnableVisualization = args.Show;
        }

        private void OnScanComplete()
        {
            UpdateVisualizationQuad();
        }

        private void Update()
        {
            if (_enableVisualizationLastFrame != EnableVisualization)
            {
                _enableVisualizationLastFrame = EnableVisualization;

                UpdateVisualizationQuad();
            }
        }


        private void InitializeVisualizationQuad()
        {
            _visualizationQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(_visualizationQuad.GetComponent<MeshCollider>());

            GridGraph gridGraph = _navigation.AstarPath.data.gridGraph;

            _visualizationQuad.transform.SetParent(transform, true);

            _visualizationQuadRenderer = _visualizationQuad.GetComponent<Renderer>();

            _visualizationQuadRenderer.material = AstarVisualizerQuadMaterial;

            _visualizationQuadTexture = new Texture2D(gridGraph.width, gridGraph.depth);//, TextureFormat.Alpha8, false);
            _visualizationQuadTexture.filterMode = FilterMode.Point;

            _visualizationQuadRenderer.material.mainTexture = _visualizationQuadTexture;

        }

        private void UpdateVisualizationQuad()
        {
            // If there is a visualization quad ensure it's only active in the heirarchy if EnableVisualization is true.
            if (_visualizationQuadRenderer != null)
                _visualizationQuadRenderer.gameObject.SetActive(EnableVisualization);

            // Stop here if we're not enabling visualization.
            if (!EnableVisualization) return;

            // Do some initial setup if we haven't yet done a scan.
            if (_needsInitializing)
            {
                InitializeVisualizationQuad();

                _needsInitializing = false;
            }

            GridGraph gridGraph = _navigation.AstarPath.data.gridGraph;

            _visualizationQuad.transform.position = gridGraph.center + Vector3.up * quadHeightOffset;
            _visualizationQuad.transform.eulerAngles = new Vector3(90, 0, 0f);
            _visualizationQuad.transform.localScale = gridGraph.size;

            Color[] texturePixels = new Color[gridGraph.depth * gridGraph.width];

            // Iterate through the list as if it were a 2D array
            for (int i = 0; i < gridGraph.depth; i++) // Row index
            {
                for (int j = 0; j < gridGraph.width; j++) // Column index
                {
                    GridNodeBase gridNode = gridGraph.GetNode(j, i);

                    texturePixels[i * gridGraph.width + j] = gridNode.Walkable ? Color.cyan : Color.clear;
                }
            }

            // Texture dimensions are read-only.
            // If the grid dimensions change, recreate the texture:
            if (_visualizationQuadTexture.width != gridGraph.width || _visualizationQuadTexture.height != gridGraph.depth)
            {
                _visualizationQuadTexture = new Texture2D(gridGraph.width, gridGraph.depth);
                _visualizationQuadTexture.filterMode = FilterMode.Point;
                _visualizationQuadRenderer.material.mainTexture = _visualizationQuadTexture;
            }
            _visualizationQuadTexture.SetPixels(texturePixels);

            _visualizationQuadTexture.Apply();
        }
        
        private List<GraphNode> GetAllNodes()
        {
            GridGraph gridGraph = AstarPath.active.data.gridGraph;
            List<GraphNode> nodes = new List<GraphNode>();
            gridGraph.GetNodes((System.Action<GraphNode>)nodes.Add);

            return nodes;
        }

    }
}

