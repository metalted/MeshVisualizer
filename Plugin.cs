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
        public const string pluginVersion = "1.5.0";
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
                        
                        BlockProperties bp = trigger.GetComponentInParent<BlockProperties>();
                        if(bp != null)
                        {
                            //Omnibooster
                            if(bp.blockID == 1545)
                            {
                                GameObject dodec = CreateWireframeDodecagon(bounds, GetColor((string)lineColor.BoxedValue), trigger.transform);
                                currentBoxes.Add(dodec);
                                continue;
                            }
                        }

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

        public GameObject CreateWireframeDodecagon(Bounds bounds, Color color, Transform obj)
        {
            GameObject dodecagonObject = new GameObject("WireframeDodecagon");
            LineRenderer lineRenderer = dodecagonObject.AddComponent<LineRenderer>();

            // Set the material and color for the LineRenderer
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = (float)lineWidth.BoxedValue;
            lineRenderer.endWidth = (float)lineWidth.BoxedValue;

            // Define the vertices of the wireframe dodecagon
            Vector3[] top = new Vector3[12];
            Vector3[] bot = new Vector3[12];

            float radius = bounds.size.x / 2.0f; // Assuming bounds.size.x represents the diameter of the dodecagon

            for (int i = 0; i < 12; i++)
            {
                float angle = 2 * Mathf.PI / 12 * i;
                float x = bounds.center.x + Mathf.Cos(angle) * radius;
                float z = bounds.center.z + Mathf.Sin(angle) * radius;
                float yTop = bounds.center.y + bounds.size.y / 2.0f; // Adjusting y to be at the top of the dodecagon
                float yBottom = bounds.center.y - bounds.size.y / 2.0f; // Adjusting y to be at the bottom of the dodecagon
                top[i] = obj.TransformPoint(new Vector3(x, yTop, z));
                bot[i] = obj.TransformPoint(new Vector3(x, yBottom, z));
            }

            Vector3[] dodecLine = new Vector3[]
            {
                // First zigzag run
                top[0], top[1], bot[1], bot[2], top[2], top[3], bot[3], bot[4],
                top[4], top[5], bot[5], bot[6], top[6], top[7], bot[7], bot[8],
                top[8], top[9], bot[9], bot[10], top[10], top[11], bot[11], bot[0],
                top[0], bot[0], bot[1], top[1],

                // Second zigzag run
                top[2], bot[2], bot[3], top[3], top[4], bot[4],
                bot[5], top[5], top[6], bot[6], bot[7], top[7], top[8], bot[8],
                bot[9], top[9], top[10], bot[10], bot[11], top[11], top[0]
            };

            lineRenderer.positionCount = dodecLine.Length;
            lineRenderer.SetPositions(dodecLine);

            return dodecagonObject;
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
