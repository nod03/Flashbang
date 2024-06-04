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

            Whitescreen = new("Flashbang");
            Whitescreen.transform.SetParent(hud.mainContainer.transform);
            RectTransform rectTransform = Whitescreen.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            Image image = Whitescreen.AddComponent<Image>();
            image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 500);
            
            SetWhitescreenAlpha(0);
        }

        GameObject Whitescreen;
        public void SetWhitescreenAlpha(float x)
        {
            Whitescreen.GetComponent<Image>().color = new Color(1, 1, 1, x);
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
            if (Whitescreen != null)
            {
                opacity = 1;
                StartCoroutine(Fade());
                Log.Info("Flashbang!");
                AkSoundEngine.PostEvent(1322173159, PlayerCharacterMasterController.instances[0].body.gameObject);
            }
            else
            {
                Log.Warning("Tried to flashbang without whitescreen");
            }
        }

        float opacity;
        private IEnumerator Fade()
        {
            SetWhitescreenAlpha(opacity);
            yield return new WaitForSeconds(1);
            while (opacity > 0)
            {
                yield return new WaitForSeconds(0.01f);
                opacity -= 0.01f;
                SetWhitescreenAlpha(opacity);
            }
            
        }
    }
}