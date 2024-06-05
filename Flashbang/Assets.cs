using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

namespace Flashbang
{
    public static class Assets
    {

        public static AssetBundle mainAssetBundle = null;
        internal static string assetBundleName = "flashbang";
        internal static string assemblyDir
        {
            get
            {
                return Path.GetDirectoryName(Flashbang.pluginInfo.Location);
            }
        }

        public static void PopulateAssets()
        {
            mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(assemblyDir, assetBundleName));
        }
    }

    internal static class SoundBanks
    {
        private static bool initialized = false;

        public static string SoundBankDirectory
        {
            get
            {
                return Path.Combine(Assets.assemblyDir);
            }
        }

        public static void Init()
        {
            if (initialized) return;
            initialized = true;
            AKRESULT akResult = AkSoundEngine.AddBasePath(SoundBankDirectory);
            if (akResult == AKRESULT.AK_Success)
            {
                Log.Info($"Added bank base path : {SoundBankDirectory}");
            }
            else
            {
                Log.Error(
                    $"Error adding base path : {SoundBankDirectory} " +
                    $"Error code : {akResult}");
            }

            AkSoundEngine.LoadBank("flashbang.bnk", out _);
            if (akResult == AKRESULT.AK_Success)
            {
                Log.Info($"Added bank : {"flashbang.bnk"}");
            }
            else
            {
                Log.Error(
                    $"Error loading bank : {"flashbang.bnk"} " +
                    $"Error code : {akResult}");
            }
        }
    }
}
