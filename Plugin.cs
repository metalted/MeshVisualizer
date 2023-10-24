using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using ZeepSDK.LevelEditor;
using BepInEx.Configuration;

namespace MeshVisualizer
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginName = "Mesh Visualizer";
        public const string pluginGuid = "com.metalted.zeepkist.meshvisualizer";
        public const string pluginVersion = "1.0";
        public static bool wireFrameAllowed = false;
        public ConfigEntry<KeyCode> refresh;
        public ConfigEntry<KeyCode> remove;

        public List<GameObject> currentBoxes = new List<GameObject>();

        public GameObject CreateWireframeBox(Bounds bounds, Color color)
        {
            GameObject boxObject = new GameObject("WireframeBox");
            LineRenderer lineRenderer = boxObject.AddComponent<LineRenderer>();

            // Set the material and color for the LineRenderer
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;

            // Set the number of positions (vertices) for the LineRenderer (8 for a box)
            lineRenderer.positionCount = 16;

            // Define the vertices of the wireframe box
            Vector3[] bv = new Vector3[8];

            float halfWidth = bounds.size.x / 2.0f;
            float halfHeight = bounds.size.y / 2.0f;
            float halfDepth = bounds.size.z / 2.0f;

            bv[0] = bounds.center + new Vector3(-halfWidth, -halfHeight, -halfDepth);
            bv[1] = bounds.center + new Vector3(halfWidth, -halfHeight, -halfDepth);
            bv[2] = bounds.center + new Vector3(halfWidth, -halfHeight, halfDepth);
            bv[3] = bounds.center + new Vector3(-halfWidth, -halfHeight, halfDepth);
            bv[4] = bounds.center + new Vector3(-halfWidth, halfHeight, -halfDepth);
            bv[5] = bounds.center + new Vector3(halfWidth, halfHeight, -halfDepth);
            bv[6] = bounds.center + new Vector3(halfWidth, halfHeight, halfDepth);
            bv[7] = bounds.center + new Vector3(-halfWidth, halfHeight, halfDepth);

            // Set the positions for the LineRenderer
            lineRenderer.SetPositions(new Vector3[] { bv[0], bv[1], bv[2], bv[3], bv[0], bv[4], bv[5], bv[1], bv[5], bv[6], bv[2], bv[6], bv[7], bv[3], bv[7], bv[4] });

            return boxObject;
        }


        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginGuid} is loaded!");

            LevelEditorApi.EnteredLevelEditor += () => { wireFrameAllowed = true; };
            LevelEditorApi.ExitedLevelEditor += () => { wireFrameAllowed = false; DestroyWireframes(); };

            refresh = Config.Bind("Controls", "Refresh", KeyCode.Keypad9, "Refresh the bounding boxes.");
            remove = Config.Bind("Controls", "Remove", KeyCode.Keypad8, "Remove the bounding boxes.");

        }

        private void DestroyWireframes()
        {
            //Destroy all current boxes
            foreach (GameObject go in currentBoxes)
            {
                if (go != null)
                {
                    GameObject.Destroy(go);
                }
            }
            currentBoxes.Clear();
        }

        private void Update()
        {
            if (wireFrameAllowed)
            {
                if (Input.GetKeyDown((KeyCode)refresh.BoxedValue))
                {
                    DestroyWireframes();

                    //Get all the current triggers
                    List<MeshCollider> triggers = GetTriggers();

                    //Go over all triggers and create wireframe boxes
                    foreach (MeshCollider t in triggers)
                    {
                        Bounds b = t.bounds;
                        GameObject wireframe = CreateWireframeBox(b, Color.red);
                        currentBoxes.Add(wireframe);
                    }
                }

                if (Input.GetKeyDown((KeyCode)remove.BoxedValue))
                {
                    DestroyWireframes();
                }
            }
        }

        public List<MeshCollider> GetTriggers()
        {
            List<MeshCollider> triggerColliders = new List<MeshCollider>();
            
            // Find all mesh colliders in the scene
            MeshCollider[] allColliders = FindObjectsOfType<MeshCollider>();

            foreach (MeshCollider collider in allColliders)
            {
                // Check if the collider is set as a trigger
                if (collider.isTrigger)
                {
                    // Add the trigger collider to the list
                    triggerColliders.Add(collider);
                }
            }

            return triggerColliders;
        }
    }

}
