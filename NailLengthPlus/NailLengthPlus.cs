using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Modding;
using UnityEngine;
using System.Security.Cryptography;
using SFCore;

namespace NailLengthPlus
{
    public class SaveSettings : ModSettings
    {
        public List<bool> gotCharms = new List<bool>() { true};
        public List<bool> newCharms = new List<bool>() { true};
        public List<bool> equippedCharms = new List<bool>() { false};
        public List<int> charmCosts = new List<int>() { 1};
    }

    public class GlobalSettings : ModSettings
    {
        public bool ULTRANAIL = false;
    }

    public class NailLengthPlus : Mod<SaveSettings, GlobalSettings>
    {
        internal static NailLengthPlus Instance;

        public CharmHelper charmHelper { get; private set; }

        // Thx to 56
        public override string GetVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            string ver = asm.GetName().Version.ToString();

            /*SHA1 sha1 = SHA1.Create();
            FileStream stream = File.OpenRead(asm.Location);

            byte[] hashBytes = sha1.ComputeHash(stream);

            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            stream.Close();
            sha1.Clear();

            string ret = $"{ver}-{hash.Substring(0, 6)}";

            return ret;*/
            return ver;
        }

        public override void Initialize()
        {
            Log("Initializing");
            Instance = this;

            initGlobalSettings();
            charmHelper = new CharmHelper();
            charmHelper.customCharms = 1;
            charmHelper.customSprites = new Sprite[] { new Sprite() };
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (string res in asm.GetManifestResourceNames())
                {
                    // if (!res.EndsWith(".png") && !res.EndsWith(".tex"))
                    // {
                    //     Log("Unknown resource: " + res);
                    //     continue;
                    // }

                    using (Stream s = asm.GetManifestResourceStream(res))
                    {
                        if (s == null) continue;

                        byte[] buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        s.Dispose();

                        //Create texture from bytes 
                        var tex = new Texture2D(2, 2);

                        tex.LoadImage(buffer, true);

                        // Create sprite from texture 
                        // Substring is to cut off the Lightbringer. and the .png 
                        charmHelper.customSprites = new Sprite[] { Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) };

                        Log("Created sprite from embedded image: " + res);
                    }
                }
            }
            catch
            {
                charmHelper.customSprites = new Sprite[] { new Sprite() };
            }
            initCallbacks();

            Log("Initialized");
        }

        private void initGlobalSettings()
        {
            GlobalSettings.ULTRANAIL = GlobalSettings.ULTRANAIL;
        }

        private void initSaveSettings(SaveGameData data)
        {
            Settings.gotCharms = Settings.gotCharms;
            Settings.newCharms = Settings.newCharms;
            Settings.equippedCharms = Settings.equippedCharms;
            Settings.charmCosts = Settings.charmCosts;
        }

        private void initCallbacks()
        {
            ModHooks.Instance.GetPlayerBoolHook += OnGetPlayerBoolHook;
            ModHooks.Instance.SetPlayerBoolHook += OnSetPlayerBoolHook;
            ModHooks.Instance.GetPlayerIntHook += OnGetPlayerIntHook;
            ModHooks.Instance.SetPlayerIntHook += OnSetPlayerIntHook;
            ModHooks.Instance.AfterSavegameLoadHook += initSaveSettings;
            ModHooks.Instance.ApplicationQuitHook += SaveCHEGlobalSettings;
            ModHooks.Instance.LanguageGetHook += OnLanguageGetHook;
            ModHooks.Instance.CharmUpdateHook += Instance_CharmUpdateHook;
            On.NailSlash.StartSlash += NailSlash_StartSlash;
        }

        private void NailSlash_StartSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            orig(self);
            int i = Settings.charmCosts[0] - 2;
            if (PlayerData.instance.hasAllNailArts)
                i++;
            if (GlobalSettings.ULTRANAIL)
                i = 6;
            if (Settings.equippedCharms[0])
                self.gameObject.transform.localScale += new Vector3(self.scale.x * i / 2, self.scale.y * i / 2);
        }

        private void Instance_CharmUpdateHook(PlayerData data, HeroController controller)
        {
            int i = 3;
            if (data.hasCyclone)
                i++;
            if (data.hasDashSlash)
                i++;
            if (data.hasUpwardSlash)
                i++;
            //if (data.hasAllNailArts)
            //    i++;
            if (GlobalSettings.ULTRANAIL)
                i = 0;
            Settings.charmCosts[0] = i;
        }

        private void SaveCHEGlobalSettings()
        {
            SaveGlobalSettings();
        }

        #region Get/Set Hooks
        private string OnLanguageGetHook(string key, string sheet)
        {
            //Log($"Sheet: {sheet}; Key: {key}");
            // There probably is a better way to do this, but for now take this
            #region Custom Charms
            if (key.StartsWith("CHARM_NAME_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return "Nailmaster's Passion";
                }
            }
            if (key.StartsWith("CHARM_DESC_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return "Nailmasters experiance a great passion when weilding their nail, causing them to weild it with a longer length. Anyone can feel this, but the more nail arts that are collected, the longer your nail will become";
                }
            }
            #endregion
            return Language.Language.GetInternal(key, sheet);
        }

        private bool OnGetPlayerBoolHook(string target)
        {
            if (Settings.BoolValues.ContainsKey(target))
            {
                return Settings.BoolValues[target];
            }
            #region Custom Charms
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.gotCharms[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.newCharms[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.equippedCharms[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
            #endregion
            return PlayerData.instance.GetBoolInternal(target);
        }
        private void OnSetPlayerBoolHook(string target, bool val)
        {
            if (Settings.BoolValues.ContainsKey(target))
            {
                Settings.BoolValues[target] = val;
                return;
            }
            #region Custom Charms
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    Settings.gotCharms[charmHelper.charmIDs.IndexOf(charmNum)] = val;
                    return;
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    Settings.newCharms[charmHelper.charmIDs.IndexOf(charmNum)] = val;
                    return;
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    Settings.equippedCharms[charmHelper.charmIDs.IndexOf(charmNum)] = val;
                    return;
                }
            }
            #endregion
            PlayerData.instance.SetBoolInternal(target, val);
        }

        private int OnGetPlayerIntHook(string target)
        {
            if (Settings.IntValues.ContainsKey(target))
            {
                return Settings.IntValues[target];
            }
            #region Custom Charms
            if (target.StartsWith("charmCost_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.charmCosts[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
            #endregion
            return PlayerData.instance.GetIntInternal(target);
        }
        private void OnSetPlayerIntHook(string target, int val)
        {
            if (Settings.IntValues.ContainsKey(target))
            {
                Settings.IntValues[target] = val;
            }
            else
            {
                PlayerData.instance.SetIntInternal(target, val);
            }
            //Log("Int  set: " + target + "=" + val.ToString());
        }
        #endregion
    }
}