using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyCasualtiesRussifier
{
    [BepInPlugin("com.myname.casualtiesunknown.russifier", "MyCasualtiesRussifier", "1.0.0")]
    [BepInProcess("CasualtiesUnknown.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly string PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string AssetsPath => Path.Combine(PluginPath, "Assets");
        public static Plugin Instance { get; private set; }

        internal new ManualLogSource Logger;
        public Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            try
            {
                LoadAssets(AssetsPath);
                var harmony = new Harmony("com.myname.casualtiesunknown.russifier");
                harmony.PatchAll();
                Logger.LogInfo("MyCasualtiesRussifier успешно загружен!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при загрузке русификатора: {ex}");
            }
        }

        private void LoadAssets(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.LogWarning($"Папка Assets не найдена по пути: {path}");
                return;
            }

            string[] files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string relativePath = file.Substring(path.Length + 1);
                string key = Path.ChangeExtension(relativePath, null).Replace('/', '\\');

                Texture2D tex = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(tex, File.ReadAllBytes(file)))
                {
                    tex.filterMode = FilterMode.Point;
                    Textures[key] = tex;
                }
            }
        }

        public static void SafeReplaceTexture(string key, Texture2D targetTexture)
        {
            if (targetTexture == null || !Instance.Textures.TryGetValue(key, out Texture2D sourceTexture))
                return;

            if (sourceTexture.width != targetTexture.width || sourceTexture.height != targetTexture.height)
            {
                Instance.Logger.LogWarning($"Размер картинки {key} ({sourceTexture.width}x{sourceTexture.height}) не совпадает с оригиналом ({targetTexture.width}x{targetTexture.height})!");
                return;
            }

            targetTexture.SetPixels(0, 0, sourceTexture.width, sourceTexture.height, sourceTexture.GetPixels());
            targetTexture.Apply();
        }
    }

    [HarmonyPatch(typeof(ManualDefibMinigame), "Start")]
    public static class ManualDefibMinigame_Start
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            var screen = Minigame.game.spawnedMiniGame.transform.Find("Screen");
            if (screen != null)
            {
                Plugin.SafeReplaceTexture("ManualDefibrillator\\ManualDefibrillator", screen.GetComponent<Image>().sprite.texture);
                var charging = screen.Find("Charging");
                if (charging != null)
                {
                    Plugin.SafeReplaceTexture("ManualDefibrillator\\ManualDefibrillatorChargingBar", charging.GetComponent<Image>().sprite.texture);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "Start")]
    public static class PlayerCamera_Start
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance)
        {
            for (int i = 0; i < __instance.lastStandImages.Length; i++)
            {
                Plugin.SafeReplaceTexture($"LastStand\\LastStand{i}", __instance.lastStandImages[i].texture);
            }
        }
    }

    [HarmonyPatch(typeof(PreRunScript), "Start")]
    public static class PreRunScript_Start
    {
        [HarmonyPostfix]
        private static void Postfix(PreRunScript __instance)
        {
            var logo = __instance.transform.Find("Logo");
            if (logo != null)
                Plugin.SafeReplaceTexture("MainMenu\\GameTitle", logo.GetComponent<Image>().sprite.texture);

            var credits = __instance.transform.Find("Credits");
            if (credits != null)
            {
                var creditsLogo = credits.Find("Logo");
                if (creditsLogo != null)
                    Plugin.SafeReplaceTexture("MainMenu\\GameTitle", creditsLogo.GetComponent<Image>().sprite.texture);

                SetText(credits, "Orsoniks", "creditsorsoniks");
                SetText(credits, "Wick", "creditswick");
                SetText(credits, "Taco", "creditstaco");
                SetText(credits, "Oro", "creditsoro");
            }

            SetText(__instance.transform, "Version", "version");
        }

        private static void SetText(Transform parent, string childName, string localeKey)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = Locale.GetOther(localeKey);
            }
        }
    }

    [HarmonyPatch(typeof(WoundView), "Start")]
    public static class WoundView_Start
    {
        [HarmonyPostfix]
        private static void Postfix(WoundView __instance)
        {
            var skillsBack = __instance.transform.Find("SkillsBack");
            if (skillsBack != null)
            {
                var img = skillsBack.Find("Image");
                if (img != null)
                    Plugin.SafeReplaceTexture("HealthPanel\\SkillsScreen", img.GetComponent<Image>().sprite.texture);
            }
        }
    }
}
