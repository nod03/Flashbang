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

    [BepInDependency(ItemAPI.PluginGUID)]

    public class Flashbang : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "bouncyshield";
        public const string PluginName = "Flashbang";
        public const string PluginVersion = "1.0.0";

        public static PluginInfo pluginInfo;

        public void Awake()
        {
            pluginInfo = Info;
            Log.Init(Logger);

            On.RoR2.UI.HUD.Awake += GetHud;
            On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentActivate;

            CreateEquipment();
        }

        public void OnDestroy()
        {
            On.RoR2.UI.HUD.Awake -= GetHud;
            On.RoR2.EquipmentSlot.PerformEquipmentAction -= EquipmentActivate;
        }

        public void Start()
        {
            SoundBanks.Init();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Bang();
            }
        }

        private RoR2.UI.HUD hud;
        private void GetHud(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
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

        private GameObject Whitescreen;
        private GameObject Dizzyscreen;
        private void FlashAlpha(float x)
        {
            Whitescreen.GetComponent<Image>().color = new Color(1, 1, 1, x);
            Dizzyscreen.GetComponent<Image>().color = new Color(1, 1, 1, x);
        } 

        private void Bang()
        {
            if (Whitescreen != null && Dizzyscreen != null)
            {
                Texture2D buh = ScreenCapture.CaptureScreenshotAsTexture();
                Dizzyscreen.GetComponent<Image>().sprite = Sprite.Create(buh, new Rect(0, 0, buh.width, buh.height), Vector2.zero);

                StartCoroutine(Fade());
                
                AkSoundEngine.PostEvent(2753768932, Run.instance.gameObject);

                Log.Info("Flashbang!");
            }
            else
            {
                Log.Warning("Bad flashbang...");
            }
        }

        private float alpha;
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

        private EquipmentDef flashbang;
        private void CreateEquipment()
        {
            flashbang = ScriptableObject.CreateInstance<EquipmentDef>();

            flashbang.name = "FLASHBANG_NAME";
            flashbang.nameToken = "FLASHBANG_NAME";
            flashbang.pickupToken = "FLASHBANG_PICKUP";
            flashbang.descriptionToken = "FLASHBANG_DESC";
            flashbang.loreToken = "FLASHBANG_LORE";

            flashbang.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            flashbang.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            flashbang.cooldown = 30;

            ItemAPI.Add(new CustomEquipment(flashbang, new ItemDisplayRuleDict(null)));
        }

        private bool EquipmentActivate(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef def)
        {
            if (def == flashbang)
            {
                try
                {
                    Bang();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return orig(self, def);
            }
        }
    }
}