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
        public const string pluginVersion = "1.3.0";
        public static bool wireFrameAllowed = false;
        public ConfigEntry<KeyCode> refresh;
        public ConfigEntry<KeyCode> remove;
        public ConfigEntry<KeyCode> autoUpdateKey;

        public ConfigEntry<float> lineWidth;
        public ConfigEntry<string> lineColor;
        public ConfigEntry<bool> autoUpdate;
        public string[] colors = new string[] { "Black", "Blue", "Cyan", "Gray", "Green", "Magenta", "Red", "White", "Yellow" };

        public List<GameObject> currentBoxes = new List<GameObject>();

        public GameObject CreateWireframeBox(Bounds bounds, Color color)
        {
            GameObject boxObject = new GameObject("WireframeBox");
            LineRenderer lineRenderer = boxObject.AddComponent<LineRenderer>();

            // Set the material and color for the LineRenderer
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = (float) lineWidth.BoxedValue;
            lineRenderer.endWidth = (float) lineWidth.BoxedValue;

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
            refresh = Config.Bind("Controls", "Refresh", KeyCode.Keypad7, "Refresh the bounding boxes.");
            remove = Config.Bind("Controls", "Remove", KeyCode.Keypad8, "Remove the bounding boxes.");
            autoUpdateKey = Config.Bind("Controls", "Toggle Auto Update", KeyCode.Keypad9, "Enable or disable the auto wireframe update.");
            lineWidth = Config.Bind("Preferences", "Line Width", 0.2f, "The width of the line in the wireframe.");
            lineColor = Config.Bind("Preferences", "Line Color", "Red", new ConfigDescription("Selected Color", new AcceptableValueList<string>(colors)));
            autoUpdate = Config.Bind("Preferences", "Auto Update", false, "Update the wireframes automatically.");
            
        
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
                if (Input.GetKeyDown((KeyCode)refresh.BoxedValue) || (bool)autoUpdate.BoxedValue)
                {
                    DestroyWireframes();

                    //Get all the current triggers
                    List<Collider> triggers = GetTriggers();

                    //Go over all triggers and create wireframe boxes
                    foreach (Collider t in triggers)
                    {
                        Bounds b = t.bounds;
                        GameObject wireframe = CreateWireframeBox(b, GetColor((string)lineColor.BoxedValue));
                        currentBoxes.Add(wireframe);
                    }
                }

                if (Input.GetKeyDown((KeyCode)remove.BoxedValue))
                {
                    DestroyWireframes();
                }

                if(Input.GetKeyDown((KeyCode)autoUpdateKey.BoxedValue))
                {
                    autoUpdate.Value = !((bool)autoUpdate.BoxedValue);
                    PlayerManager.Instance.messenger.Log("AOE Auto Update: " + ( ((bool)autoUpdate.BoxedValue) ? "On" : "Off"), 1f);
                }
            }
        }

        public List<Collider> GetTriggers()
        {
            List<Collider> triggerColliders = new List<Collider>();
            
            // Find all mesh colliders in the scene
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

        public Color GetColor(string colorName)
        {
            switch(colorName)
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
