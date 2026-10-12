using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PartAdjustment
{
    [BepInPlugin(Guid, "Ultimate Tools", "2.3.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.denis.apocalypter.partadjustment";
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled, VisibleGizmosWithoutTool;
        internal const float WidthStep = .025f, AttachmentReach = 5f;
        internal const int ReferenceFrame = 0;
        internal const KeyCode ModifierAlternative = KeyCode.None;
        internal static ConfigEntry<float> MovementRotationSpeed, FastMovementRotationSpeed;
        internal static ConfigEntry<KeyCode> ModifierKey;
        internal static ConfigEntry<bool> AttachAnywhere, PutAwayWithKey;
        internal static ConfigEntry<int> TailLightChance;
        internal static ConfigEntry<KeyCode> PutAwayKey;
        private static GameObject runner;
        private void Awake()
        {
            Log = Logger;
            if (File.Exists(Config.ConfigFilePath))
            {
                string old = File.ReadAllText(Config.ConfigFilePath);
                if (SettingsMigration.NeedsUpgrade(old))
                {
                    File.WriteAllText(Config.ConfigFilePath, SettingsMigration.Upgrade(old));
                    Config.Reload();
                }
            }
            Enabled = Config.Bind("General", "Enabled", true, "Enable Ultimate Tools for both utility and Part Adjustment toolboxes. Existing saved poses remain on vehicles.");
            VisibleGizmosWithoutTool = Config.Bind("General", "VisibleGizmosWithoutTool", true, "While carrying an attachable car part near a car, show compatible slots even without the tool. Turn off to require the tool. The existing scan runs at most once per second while a part is held.");
            PutAwayWithKey = Config.Bind("General", "PutAwayWithKey", true, "Enable the key that puts away the utility, repair or Part Adjustment tool.");
            PutAwayKey = Config.Bind("General", "PutAwayKey", KeyCode.X, "Key that puts away the tool in your hands.");
            MovementRotationSpeed = Config.Bind("General", "MovementRotationSpeed", 100f, new ConfigDescription("Normal item movement/rotation speed in percent. 100% = 1 cm / 1 degree per press.", new AcceptableValueRange<float>(10f, 10000f)));
            FastMovementRotationSpeed = Config.Bind("General", "FastMovementRotationSpeed", 1000f, new ConfigDescription("Item movement/rotation speed with FastModifierKey held, in percent. 1000% = 10 cm / 10 degrees per press.", new AcceptableValueRange<float>(10f, 10000f)));
            ModifierKey = Config.Bind("General", "FastModifierKey", KeyCode.LeftAlt, "Hold for the fast movement/rotation speed. None disables the modifier.");
            AttachAnywhere = Config.Bind("Headlights", "AttachAnywhere", true, "With the Part Adjustment tool selected, a held headlight or tail light attaches anywhere on a car body (Use, normally F) and Use removes attached ones. Without the tool they only fit headlight slots; the utility tool removes them as usual.");
            TailLightChance = Config.Bind("Headlights", "TailLightChance", 8, new ConfigDescription("Chance in percent that a headlight spawned in the world (wrecks, caves, merchants) is a red tail light instead.", new AcceptableValueRange<int>(0, 100)));
            // Apocasetter hides this registration flag from the settings list.
            Config.Bind("General", "Apocasetter", true, "Show this mod in the Apocasetter Mods menu.");
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            SceneManager.sceneLoaded += (scene, mode) => { EnsureRunner(); ToolRunner.SceneChanged(); AdjustmentRunner.SceneChanged(); TailLights.SceneChanged(); };
            EnsureRunner();
            Logger.LogInfo("Ultimate Tools 2.3.0 loaded. Nearby slot highlights default to always visible; settings use the compact General/Headlights layout.");
        }
        private static void EnsureRunner()
        {
            if (runner != null && runner.activeInHierarchy) return;
            if (runner != null) Destroy(runner);
            runner = new GameObject("PartAdjustment.Runner") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(runner);
            runner.AddComponent<ToolRunner>();
            runner.AddComponent<AdjustmentRunner>();
            runner.AddComponent<AdjustmentLateRunner>();
            runner.AddComponent<ToolPutAway>();
            runner.AddComponent<TailLightRunner>();
            runner.AddComponent<ToolGuide>();
        }

    }
}
