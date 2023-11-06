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
        public const string pluginName = "Area of Effect";
        public const string pluginGuid = "com.metalted.zeepkist.areaofeffect";
        public const string pluginVersion = "1.4.0";
        public static bool wireFrameAllowed = false;

        // Configuration options
        public ConfigEntry<KeyCode> refresh;
        public ConfigEntry<KeyCode> remove;
        public ConfigEntry<KeyCode> autoUpdateKey;
        public ConfigEntry<float> lineWidth;
        public ConfigEntry<string> lineColor;
        public ConfigEntry<bool> autoUpdate;
        public string[] colors = new string[] { "Black", "Blue", "Cyan", "Gray", "Green", "Magenta", "Red", "White", "Yellow" };

        // List to keep track of wireframe boxes
        public List<GameObject> currentBoxes = new List<GameObject>();

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginGuid} is loaded!");

            // Event handlers for entering and exiting the level editor
            LevelEditorApi.EnteredLevelEditor += () => { wireFrameAllowed = true; };
            LevelEditorApi.ExitedLevelEditor += () => { wireFrameAllowed = false; DestroyWireframes(); };

            // Configuration options
            refresh = Config.Bind("Controls", "Refresh", KeyCode.Keypad7, "Refresh the bounding boxes.");
            remove = Config.Bind("Controls", "Remove", KeyCode.Keypad8, "Remove the bounding boxes.");
            autoUpdateKey = Config.Bind("Controls", "Toggle Auto Update", KeyCode.Keypad9, "Enable or disable the auto wireframe update.");
            lineWidth = Config.Bind("Preferences", "Line Width", 0.2f, "The width of the line in the wireframe.");
            lineColor = Config.Bind("Preferences", "Line Color", "Red", new ConfigDescription("Selected Color", new AcceptableValueList<string>(colors)));
            autoUpdate = Config.Bind("Preferences", "Auto Update", false, "Update the wireframes automatically.");
        }

        private void Update()
        {
            if (wireFrameAllowed)
            {
                if (Input.GetKeyDown((KeyCode)refresh.BoxedValue) || (bool)autoUpdate.BoxedValue)
                {
                    DestroyWireframes();

                    // Get all the current triggers
                    List<Collider> triggers = GetTriggers();

                    // Create wireframe boxes for each trigger
                    foreach (Collider trigger in triggers)
                    {
                        Bounds bounds = GetBoundingBox(trigger, trigger.transform);
                        GameObject wireframe = CreateWireframeBox(bounds, GetColor((string)lineColor.BoxedValue), trigger.transform);
                        currentBoxes.Add(wireframe);
                    }
                }

                if (Input.GetKeyDown((KeyCode)remove.BoxedValue))
                {
                    DestroyWireframes();
                }

                if (Input.GetKeyDown((KeyCode)autoUpdateKey.BoxedValue))
                {
                    autoUpdate.Value = !((bool)autoUpdate.BoxedValue);
                    PlayerManager.Instance.messenger.Log("AOE Auto Update: " + (((bool)autoUpdate.BoxedValue) ? "On" : "Off"), 1f);
                }
            }
        }

        // Destroy all wireframe boxes
        private void DestroyWireframes()
        {
            foreach (GameObject go in currentBoxes)
            {
                if (go != null)
                {
                    GameObject.Destroy(go);
                }
            }
            currentBoxes.Clear();
        }

        // Create a wireframe box
        public GameObject CreateWireframeBox(Bounds bounds, Color color, Transform obj)
        {
            GameObject boxObject = new GameObject("WireframeBox");
            LineRenderer lineRenderer = boxObject.AddComponent<LineRenderer>();

            // Set the material and color for the LineRenderer
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = (float)lineWidth.BoxedValue;
            lineRenderer.endWidth = (float)lineWidth.BoxedValue;

            // Set the number of positions (vertices) for the LineRenderer (16 for a box)
            lineRenderer.positionCount = 16;

            // Define the vertices of the wireframe box
            Vector3[] bv = new Vector3[8];

            float halfWidth = bounds.size.x / 2.0f;
            float halfHeight = bounds.size.y / 2.0f;
            float halfDepth = bounds.size.z / 2.0f;

            bv[0] = obj.TransformPoint(bounds.center + new Vector3(-halfWidth, -halfHeight, -halfDepth));
            bv[1] = obj.TransformPoint(bounds.center + new Vector3(halfWidth, -halfHeight, -halfDepth));
            bv[2] = obj.TransformPoint(bounds.center + new Vector3(halfWidth, -halfHeight, halfDepth));
            bv[3] = obj.TransformPoint(bounds.center + new Vector3(-halfWidth, -halfHeight, halfDepth));
            bv[4] = obj.TransformPoint(bounds.center + new Vector3(-halfWidth, halfHeight, -halfDepth));
            bv[5] = obj.TransformPoint(bounds.center + new Vector3(halfWidth, halfHeight, -halfDepth));
            bv[6] = obj.TransformPoint(bounds.center + new Vector3(halfWidth, halfHeight, halfDepth));
            bv[7] = obj.TransformPoint(bounds.center + new Vector3(-halfWidth, halfHeight, halfDepth));

            // Set the positions for the LineRenderer
            lineRenderer.SetPositions(new Vector3[] { bv[0], bv[1], bv[2], bv[3], bv[0], bv[4], bv[5], bv[1], bv[5], bv[6], bv[2], bv[6], bv[7], bv[3], bv[7], bv[4] });

            return boxObject;
        }

        // Get all trigger colliders in the scene
        public List<Collider> GetTriggers()
        {
            List<Collider> triggerColliders = new List<Collider>();

            // Find all colliders in the scene
            Collider[] allColliders = FindObjectsOfType<Collider>();

            foreach (Collider collider in allColliders)
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

        // Get the bounding box for the given collider
        public Bounds GetBoundingBox(Collider collider, Transform obj)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            if (collider is BoxCollider boxCollider)
            {
                bounds = new Bounds(boxCollider.center, boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                float radius = sphereCollider.radius;
                bounds = new Bounds(sphereCollider.center, new Vector3(radius * 2, radius * 2, radius * 2));
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                float radius = capsuleCollider.radius;
                float height = capsuleCollider.height;
                Vector3 directionVector = Vector3.zero;

                switch (capsuleCollider.direction)
                {
                    case 0: // X-axis
                        directionVector = Vector3.right * height;
                        break;
                    case 1: // Y-axis
                        directionVector = Vector3.up * height;
                        break;
                    case 2: // Z-axis
                        directionVector = Vector3.forward * height;
                        break;
                }

                bounds = new Bounds(capsuleCollider.center + directionVector / 2, new Vector3(radius * 2, radius * 2, radius * 2) + directionVector);
            }
            else if (collider is MeshCollider meshCollider)
            {
                bounds = meshCollider.sharedMesh.bounds;
            }

            return bounds;
        }

        // Get color from the given color name
        public Color GetColor(string colorName)
        {
            switch (colorName)
            {
                case "Black":
                    return Color.black;
                case "Blue":
                    return Color.blue;
                case "Cyan":
                    return Color.cyan;
                case "Gray":
                    return Color.gray;
                case "Green":
                    return Color.green;
                case "Magenta":
                    return Color.magenta;
                case "Red":
                    return Color.red;
                default:
                case "White":
                    return Color.white;
                case "Yellow":
                    return Color.yellow;
            }
        }
    }
}
