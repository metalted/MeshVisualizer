using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;

namespace MeshVisualizer
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginName = "Area of Effect";
        public const string pluginGuid = "com.metalted.zeepkist.areaofeffect";
        public const string pluginVersion = "1.6.2";

        public static Plugin Instance;
        public LEV_LevelEditorCentral central;
        public Material semiTransparentOrange;

        public ConfigEntry<bool> areaEnabled;
        public ConfigEntry<KeyCode> toggleAreas;
        public ConfigEntry<string> appliedIDs;
        public List<int> idsToApplyTo = new List<int>(new int[] { 2256, 69, 1545, 290, 2280, 1280, 1281, 1282, 1666 });

        public Mesh unitDodecagonMesh;

        private void Awake()
        {
            Logger.LogInfo($"Plugin {pluginGuid} is loaded!");

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll();

            Instance = this;

            // Create a semi-transparent orange material
            semiTransparentOrange = new Material(Shader.Find("Standard"));
            semiTransparentOrange.color = new Color(1f, 0.5f, 0f, 0.55f);

            unitDodecagonMesh = CreateDodecagonMesh();

            // Set the material to be transparent
            semiTransparentOrange.SetFloat("_Mode", 3);
            semiTransparentOrange.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            semiTransparentOrange.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            semiTransparentOrange.SetInt("_ZWrite", 0);
            semiTransparentOrange.DisableKeyword("_ALPHATEST_ON");
            semiTransparentOrange.EnableKeyword("_ALPHABLEND_ON");
            semiTransparentOrange.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            semiTransparentOrange.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            areaEnabled = Config.Bind("Settings", "Areas Enabled", true, "");
            toggleAreas = Config.Bind("Settings", "Toggle Areas", KeyCode.Keypad9, "");
            Config.SettingChanged += Config_SettingChanged;
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            //Debug.LogWarning("Config Changed");
            AreaEffect[] areaCubes = GameObject.FindObjectsOfType<AreaEffect>();
            foreach (AreaEffect ae in areaCubes)
            {
                ae.SetState(areaEnabled.Value);
            }
        }

        private void Update()
        {
            if (central != null)
            {
                if (Input.GetKeyDown(toggleAreas.Value))
                {
                    areaEnabled.Value = !areaEnabled.Value;
                    Config.Save();
                }
            }
        }

        private Mesh CreateDodecagonMesh()
        {
            Mesh mesh = new Mesh();

            // Define the vertices for a unit dodecagon (12-sided cylinder)
            int sides = 12;
            float radius = 0.5f;
            float height = 1f;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            // Create vertices
            for (int i = 0; i < sides; i++)
            {
                float angle = 2 * Mathf.PI * i / sides;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                // Top vertices
                vertices.Add(new Vector3(x, height / 2, z));
                // Bottom vertices
                vertices.Add(new Vector3(x, -height / 2, z));
            }

            // Center points
            vertices.Add(new Vector3(0, height / 2, 0)); // Top center
            vertices.Add(new Vector3(0, -height / 2, 0)); // Bottom center

            int topCenterIndex = vertices.Count - 2;
            int bottomCenterIndex = vertices.Count - 1;

            // Create triangles with correct winding order
            for (int i = 0; i < sides; i++)
            {
                int nextIndex = (i + 1) % sides;

                // Top cap (CCW)
                triangles.Add(topCenterIndex);
                triangles.Add(nextIndex * 2);
                triangles.Add(i * 2);

                // Bottom cap (CW)
                triangles.Add(bottomCenterIndex);
                triangles.Add(i * 2 + 1);
                triangles.Add(nextIndex * 2 + 1);

                // Side faces (CCW)
                triangles.Add(i * 2);
                triangles.Add(nextIndex * 2 + 1);
                triangles.Add(i * 2 + 1);

                triangles.Add(i * 2);
                triangles.Add(nextIndex * 2);
                triangles.Add(nextIndex * 2 + 1);
            }

            // Assign vertices and triangles to the mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            // Recalculate normals for proper lighting
            mesh.RecalculateNormals();

            return mesh;
        }
    }

    public class AreaEffect : MonoBehaviour
    {
        private MeshRenderer renderer;
        public void Start()
        {
            renderer = GetComponent<MeshRenderer>();

            SetState(Plugin.Instance.areaEnabled.Value);
        }

        public void SetState(bool state)
        {
            renderer.enabled = state;
        }
    }

    [HarmonyPatch(typeof(LEV_LevelEditorCentral), "Awake")]
    public class LEVCentralAwakePostfixPatch
    {
        public static void Postfix(LEV_LevelEditorCentral __instance)
        {
            Plugin.Instance.central = __instance;
        }
    }

    [HarmonyPatch(typeof(Fan), "ChangeTriggerSize")]
    public class FanChangeTriggerSizePostfixPatch
    {
        public static void Postfix(Fan __instance)
        {
            AreaEffect ae = __instance.GetComponentInChildren<AreaEffect>();
            if (ae != null)
            {
                // Get the size and center of the trigger collider
                Vector3 colliderSize = __instance.triggerCollider.size;
                Vector3 colliderCenter = __instance.triggerCollider.center;

                // Set the AreaEffect's local scale to match the collider size (with a small adjustment)
                ae.transform.localScale = colliderSize * 0.9999f;

                // Set the AreaEffect's local position to match the collider center
                ae.transform.localPosition = colliderCenter;
            }
        }
    }

    [HarmonyPatch(typeof(BlockProperties), "Awake")]
    public class BlockPropertiesAwakePostfixPatch
    {
        public static void Postfix(BlockProperties __instance)
        {
            if (Plugin.Instance.central != null)
            {
                if (Plugin.Instance.idsToApplyTo.Contains(__instance.blockID))
                {
                    // Find all Collider components in the current GameObject and its children
                    Collider[] colliders = __instance.GetComponentsInChildren<Collider>();

                    foreach (Collider collider in colliders)
                    {
                        //If this collider already has a child with a area of effect, skip it.
                        AreaEffect childScripts = collider.transform.GetComponentInChildren<AreaEffect>();
                        if(childScripts != null)
                        {
                            continue;
                        }

                        // Check if the collider has isTrigger enabled
                        if (collider.isTrigger)
                        {
                            // Get the bounds of the collider
                            Bounds bounds = collider.bounds;

                            // Create a cube primitive
                            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                            // Remove the collider from the cube
                            Collider cubeCollider = cube.GetComponent<Collider>();
                            if (cubeCollider != null)
                            {
                                Object.Destroy(cubeCollider);
                            }

                            // Set the cube size to match the bounds size
                            cube.transform.localScale = bounds.size * 0.9999f;

                            // Set the cube position to match the collider bounds center
                            cube.transform.position = bounds.center;

                            // Make the cube a child of the collider's transform
                            cube.transform.SetParent(collider.transform);

                            // Apply the material to the cube
                            Renderer cubeRenderer = cube.GetComponent<Renderer>();
                            cubeRenderer.material = Plugin.Instance.semiTransparentOrange;

                            // If the blockID is 1545, replace the cube's mesh with the dodecagon mesh
                            if (__instance.blockID == 1545)
                            {
                                MeshFilter meshFilter = cube.GetComponent<MeshFilter>();
                                if (meshFilter != null)
                                {
                                    meshFilter.mesh = Plugin.Instance.unitDodecagonMesh;
                                }
                            }

                            // Add the AreaEffect component to the cube
                            cube.AddComponent<AreaEffect>();
                        }
                    }
                }
            }
        }
    }
}
