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

    [BepInDependency(LanguageAPI.PluginGUID)]

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
            Assets.PopulateAssets();
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

        private RoR2.UI.HUD hud;
        private void GetHud(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            orig(self);
            SoundBanks.Init(); // fancy meeting you here
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

        private void BangPlayer(NetworkInstanceId sourceId)
        {
            CharacterMaster source = CharacterMaster.instancesList.Find((CharacterMaster x) => x.networkIdentity.netId == sourceId);
            AkSoundEngine.PostEvent(2315197877, source.GetBody().gameObject);
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

            flashbang.pickupIconSprite = Assets.mainAssetBundle.LoadAsset<Sprite>("flashbang.png");
            flashbang.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/StunChanceOnHit/PickupStunGrenade.prefab").WaitForCompletion();

            flashbang.appearsInMultiPlayer = true;
            flashbang.appearsInSinglePlayer = true;
            flashbang.canBeRandomlyTriggered = true;
            flashbang.enigmaCompatible = true;
            flashbang.canDrop = true;
            flashbang.cooldown = 25;

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
                // Players
                if (x.isPlayerControlled)
                {
                    NetMessageExtensions.Send(new Sink(x.master.networkIdentity.netId, body.master.networkIdentity.netId, distance <= 80), (NetworkDestination)1);
                }
                else if (distance <= 80)
                {
                    // Enemies
                    if (x.teamComponent.teamIndex != body.teamComponent.teamIndex)
                    {
                        DamageInfo boop = new()
                        {
                            damage = Math.Max(x.healthComponent.fullCombinedHealth / 10, body.baseDamage * 1.5f),
                            //inflictor = body.gameObject,
                            attacker = body.gameObject,
                            crit = Util.CheckRoll(body.crit, body.master),
                            procCoefficient = 3f
                        };
                        x.healthComponent.TakeDamage(boop);
                        GlobalEventManager.instance.OnHitEnemy(boop, x.healthComponent.gameObject);
                        SetStateOnHurt.SetStunOnObject(x.gameObject, 5);
                    }
                    // Allies
                    else
                    {
                        SetStateOnHurt.SetStunOnObject(x.gameObject, 5);
                    }
                }
            }
        }

        
        public class Sink : INetMessage, ISerializableObject
        {
            private NetworkInstanceId playerId;
            private NetworkInstanceId sourceId;
            private bool withinRange;

            public Sink() { }

            public Sink(NetworkInstanceId playerId, NetworkInstanceId sourceId, bool withinRange)
            {
                this.playerId = playerId;
                this.sourceId = sourceId;
                this.withinRange = withinRange;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(playerId);
                writer.Write(sourceId);
                writer.Write(withinRange);
            }

            public void Deserialize(NetworkReader reader)
            {
                playerId = reader.ReadNetworkId();
                sourceId = reader.ReadNetworkId();
                withinRange = reader.ReadBoolean();
            }

            public void OnReceived()
            {
                if (playerId == LocalUserManager.GetFirstLocalUser().cachedMaster.networkIdentity.netId)
                {
                    if (withinRange)
                    {
                        Instance.FlashbangPlayer();
                    }
                    else
                    {
                        Instance.BangPlayer(sourceId); // hahaha
                    }
                }
            }
        }
    }
}