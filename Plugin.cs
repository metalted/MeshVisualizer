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
        public const string pluginName = "Area of Effect2";
        public const string pluginGuid = "com.metalted.zeepkist.areaofeffect";
        public const string pluginVersion = "1.6";

        public static Plugin Instance;
        public LEV_LevelEditorCentral central;
        public Material semiTransparentOrange;

        public ConfigEntry<bool> areaEnabled;
        public ConfigEntry<KeyCode> toggleAreas;
        public ConfigEntry<string> appliedIDs;
        public List<int> idsToApplyTo = new List<int>(new int[] { 2256, 69, 1545, 290, 2280, 1280, 1281, 1282, 1666 });

        private void Awake()
        {
            Logger.LogInfo($"Plugin {pluginGuid} is loaded!");

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll();

            Instance = this;

            // Create a semi-transparent orange material
            semiTransparentOrange = new Material(Shader.Find("Standard"));
            semiTransparentOrange.color = new Color(1f, 0.5f, 0f, 0.55f);

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
            if(central != null)
            {
                if(Input.GetKeyDown(KeyCode.Keypad9))
                {
                    areaEnabled.Value = !areaEnabled.Value;
                    //Debug.Log("Area enabled: " + areaEnabled.Value);
                    Config.Save();                   
                }
            }
        }
    }

    public class AreaEffect : MonoBehaviour
    {
        private MeshRenderer renderer;
        public void Start()
        {
            renderer = GetComponent<MeshRenderer>();
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

                            cube.AddComponent<AreaEffect>();
                        }
                    }
                }
            }
        }
    }
}

