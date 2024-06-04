using BepInEx;
using R2API;
using RoR2;
using RoR2.Stats;
using System.IO;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;
using System.Runtime.CompilerServices;
using IL.RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using R2API.Networking;
using RoR2.Networking;
using UnityEngine.UI;

namespace Flashbang
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class Flashbang : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "bouncyshield";
        public const string PluginName = "Flashbang";
        public const string PluginVersion = "1.0.0";

        public static PluginInfo pluginInfo;

        public RoR2.UI.HUD hud;

        public void Awake()
        {
            pluginInfo = Info;
            Log.Init(Logger);

            On.RoR2.UI.HUD.Awake += GetHud;
        }

        public void Start()
        {
            SoundBanks.Init();
        }

        public void GetHud(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            orig(self);
            hud = self;

            Dizzyscreen = new("Dizzyscreen");
            Dizzyscreen.transform.SetParent(hud.mainContainer.transform);
            RectTransform rectTransform = Dizzyscreen.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            Dizzyscreen.AddComponent<Image>();

            Whitescreen = new("Whitescreen");
            Whitescreen.transform.SetParent(hud.mainContainer.transform);
            rectTransform = Whitescreen.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            Image image = Whitescreen.AddComponent<Image>();
            image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            FlashAlpha(0);
        }

        GameObject Whitescreen;
        GameObject Dizzyscreen;
        public void FlashAlpha(float x)
        {
            Whitescreen.GetComponent<Image>().color = new Color(1, 1, 1, x);
            Dizzyscreen.GetComponent<Image>().color = new Color(1, 1, 1, x);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Bang();
            }
        }

        public void Bang()
        {
            if (Whitescreen != null && Dizzyscreen != null)
            {
                Texture2D buh = ScreenCapture.CaptureScreenshotAsTexture();
                Dizzyscreen.GetComponent<Image>().sprite = Sprite.Create(buh, new Rect(0, 0, buh.width, buh.height), Vector2.zero);

                StartCoroutine(Fade());
                
                AkSoundEngine.PostEvent(1322173159, PlayerCharacterMasterController.instances[0].body.gameObject);

                Log.Info("Flashbang!");
            }
            else
            {
                Log.Warning("Bad flashbang...");
            }
        }

        float alpha;
        private IEnumerator Fade()
        {
            alpha = 1;
            FlashAlpha(alpha);
            while (alpha >= 0)
            {
                yield return new WaitForSeconds(0.04f);
                alpha -= 0.01f * (2-alpha);
                FlashAlpha(alpha);
            }
            
        }
    }
}