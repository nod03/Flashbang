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
using EntityStates;
using Facepunch.Steamworks;
using static System.Net.Mime.MediaTypeNames;

namespace Flashbang
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    [BepInDependency(ItemAPI.PluginGUID)]

    [BepInDependency(NetworkingAPI.PluginGUID)]

    public class Flashbang : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "bouncyshield";
        public const string PluginName = "Flashbang";
        public const string PluginVersion = "1.0.0";

        public static PluginInfo pluginInfo;

        public static Flashbang Instance;

        public void Awake()
        {
            pluginInfo = Info;
            Log.Init(Logger);
            Instance = this;

            On.RoR2.UI.HUD.Awake += GetHud;
            On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentActivate;

            CreateEquipment();

            NetworkingAPI.RegisterMessageType<Sink>();
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

            Dizzyscreen.AddComponent<UnityEngine.UI.Image>();

            Whitescreen = new("Whitescreen");
            Whitescreen.transform.SetParent(hud.mainContainer.transform);
            rectTransform = Whitescreen.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image image = Whitescreen.AddComponent<UnityEngine.UI.Image>();
            image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            FlashAlpha(0);
        }

        private GameObject Whitescreen;
        private GameObject Dizzyscreen;
        private void FlashAlpha(float x)
        {
            Whitescreen.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(1, 1, 1, x);
            Dizzyscreen.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(1, 1, 1, x);
        }

        private void FlashbangPlayer()
        {
            if (Whitescreen != null && Dizzyscreen != null)
            {
                Texture2D buh = ScreenCapture.CaptureScreenshotAsTexture();
                Dizzyscreen.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(buh, new Rect(0, 0, buh.width, buh.height), Vector2.zero);

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
                alpha -= 0.01f * (2 - alpha);
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
            if (def == flashbang && NetworkServer.active)
            {
                FireFlashbang(self.characterBody);
                return true;
            }
            else
            {
                return orig(self, def);
            }
        }

        private void FireFlashbang(CharacterBody body)
        {
            Vector3 origin = body.gameObject.transform.position;
            foreach (CharacterBody x in CharacterBody.instancesList)
            {
                double distance = Vector3.Distance(x.gameObject.transform.position, origin);
                if (distance <= 60)
                {
                    if (x.isPlayerControlled)
                    {
                        NetMessageExtensions.Send(new Sink(x), (NetworkDestination)1);
                    }
                    else
                    {
                        DamageInfo boop = new()
                        {
                            damage = (x.healthComponent.fullCombinedHealth/20) + 10,
                            inflictor = body.gameObject,
                            attacker = body.gameObject,
                            procCoefficient = 3
                        };
                        x.healthComponent.TakeDamage(boop);

                        SetStateOnHurt.SetStunOnObject(x.gameObject, 5);
                    }
                }
            }
        }

        
        public class Sink : INetMessage, ISerializableObject
        {
            private NetworkInstanceId id;

            public Sink() { }

            public Sink(CharacterBody x)
            {
                id = x.master.networkIdentity.netId;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(id);
            }

            public void Deserialize(NetworkReader reader)
            {
                id = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (id == LocalUserManager.GetFirstLocalUser().cachedMaster.networkIdentity.netId)
                {
                    Instance.FlashbangPlayer();
                }
            }
        }
    }
}