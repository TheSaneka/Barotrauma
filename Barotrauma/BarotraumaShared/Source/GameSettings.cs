﻿using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Xml;
#if CLIENT
using Microsoft.Xna.Framework.Graphics;
using Barotrauma.Tutorials;
#endif
using System;

namespace Barotrauma
{
    public enum WindowMode
    {
        Windowed, Fullscreen, BorderlessWindowed
    }

    public enum LosMode
    {
        None,
        Transparent,
        Opaque
    }

    public partial class GameSettings
    {
        const string savePath = "config.xml";
        const string playerSavePath = "config_player.xml";
        const string vanillaContentPackagePath = "Data/ContentPackages/Vanilla";

        public int GraphicsWidth { get; set; }
        public int GraphicsHeight { get; set; }

        public bool VSyncEnabled { get; set; }

        public bool EnableSplashScreen { get; set; }

        public int ParticleLimit { get; set; }

        public float LightMapScale { get; set; }
        public bool SpecularityEnabled { get; set; }
        public bool ChromaticAberrationEnabled { get; set; }

        public bool PauseOnFocusLost { get; set; } = true;
        public bool MuteOnFocusLost { get; set; }
        public bool UseDirectionalVoiceChat { get; set; } = true;

        public enum VoiceMode
        {
            Disabled,
            PushToTalk,
            Activity
        };

        public VoiceMode VoiceSetting { get; set; }
        public string VoiceCaptureDevice { get; set; }

        public float NoiseGateThreshold { get; set; } = -45;

        private KeyOrMouse[] keyMapping;

        private WindowMode windowMode;

        private LosMode losMode;

        public List<string> jobPreferences;

        private bool useSteamMatchmaking;
        private bool requireSteamAuthentication;

        public string QuickStartSubmarineName
        {
            get;
            set;
        }

#if DEBUG
        //steam functionality can be enabled/disabled in debug builds
        public bool RequireSteamAuthentication
        {
            get { return requireSteamAuthentication && Steam.SteamManager.USE_STEAM; }
            set { requireSteamAuthentication = value; }
        }
        public bool UseSteamMatchmaking
        {
            get { return useSteamMatchmaking && Steam.SteamManager.USE_STEAM; }
            set { useSteamMatchmaking = value; }
        }
#else
        public bool RequireSteamAuthentication
        {
            get { return requireSteamAuthentication && Steam.SteamManager.USE_STEAM; }
            set { requireSteamAuthentication = value; }
        }
        public bool UseSteamMatchmaking
        {
            get { return useSteamMatchmaking && Steam.SteamManager.USE_STEAM; }
            set { useSteamMatchmaking = value; }
        }
#endif

        public bool AutoUpdateWorkshopItems;

        public WindowMode WindowMode
        {
            get { return windowMode; }
            set
            {
#if (OSX)
                // Fullscreen doesn't work on macOS, so just force any usage of it to borderless windowed.
                if (value == WindowMode.Fullscreen)
                {
                    windowMode = WindowMode.BorderlessWindowed;
                    return;
                }
#endif
                windowMode = value;
            }
        }

        public List<string> JobPreferences
        {
            get { return jobPreferences; }
            set { jobPreferences = value; }
        }

        public int CharacterHeadIndex { get; set; } = 1;
        public int CharacterHairIndex { get; set; }
        public int CharacterBeardIndex { get; set; }
        public int CharacterMoustacheIndex { get; set; }
        public int CharacterFaceAttachmentIndex { get; set; }

        public Gender CharacterGender { get; set; }
        public Race CharacterRace { get; set; }

        private float aimAssistAmount;
        public float AimAssistAmount
        {
            get { return aimAssistAmount; }
            set { aimAssistAmount = MathHelper.Clamp(value, 0.0f, 5.0f); }
        }

        public bool EnableMouseLook { get; set; } = true;

        public bool CrewMenuOpen { get; set; } = true;
        public bool ChatOpen { get; set; } = true;

        private bool unsavedSettings;
        public bool UnsavedSettings
        {
            get
            {
                return unsavedSettings;
            }
            private set
            {
                unsavedSettings = value;
#if CLIENT
                if (applyButton != null)
                {
                    //applyButton.Selected = unsavedSettings;
                    applyButton.Enabled = unsavedSettings;
                    applyButton.Text = TextManager.Get(unsavedSettings ? "ApplySettingsButtonUnsavedChanges" : "ApplySettingsButton");
                }
#endif
            }
        }

        private float soundVolume = 0.5f, musicVolume = 0.3f, voiceChatVolume = 0.5f, microphoneVolume = 1.0f;

        public float SoundVolume
        {
            get { return soundVolume; }
            set
            {
                soundVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
#if CLIENT
                if (GameMain.SoundManager != null)
                {
                    GameMain.SoundManager.SetCategoryGainMultiplier("default", soundVolume);
                    GameMain.SoundManager.SetCategoryGainMultiplier("ui", soundVolume);
                    GameMain.SoundManager.SetCategoryGainMultiplier("waterambience", soundVolume);
                }
#endif
            }
        }

        public float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                musicVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
#if CLIENT
                GameMain.SoundManager?.SetCategoryGainMultiplier("music", musicVolume);
#endif
            }
        }

        public float VoiceChatVolume
        {
            get { return voiceChatVolume; }
            set
            {
                voiceChatVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
#if CLIENT
                GameMain.SoundManager?.SetCategoryGainMultiplier("voip", voiceChatVolume * 20.0f);
#endif
            }
        }

        public float MicrophoneVolume
        {
            get { return microphoneVolume; }
            set
            {
                microphoneVolume = MathHelper.Clamp(value, 0.1f, 5.0f);
            }
        }
        public string Language
        {
            get { return TextManager.Language; }
            set { TextManager.Language = value; }
        }

        public HashSet<ContentPackage> SelectedContentPackages { get; set; }

        private HashSet<string> selectedContentPackagePaths = new HashSet<string>();

        public string   MasterServerUrl { get; set; }
        public bool     AutoCheckUpdates { get; set; }
        public bool     WasGameUpdated { get; set; }

        private string defaultPlayerName;
        public string DefaultPlayerName
        {
            get
            {
                return defaultPlayerName ?? "";
            }
            set
            {
                if (defaultPlayerName != value)
                {
                    defaultPlayerName = value;
                }
            }
        }

        public LosMode LosMode
        {
            get { return losMode; }
            set { losMode = value; }
        }

        private const float MinHUDScale = 0.75f, MaxHUDScale = 1.25f;
        public static float HUDScale { get; set; } = 1.0f;
        private const float MinInventoryScale = 0.75f, MaxInventoryScale = 1.25f;
        public static float InventoryScale { get; set; } = 1.0f;

        public List<string> CompletedTutorialNames { get; private set; }

        public static bool VerboseLogging { get; set; }
        public static bool SaveDebugConsoleLogs { get; set; }

        public bool CampaignDisclaimerShown, EditorDisclaimerShown;

        private static bool sendUserStatistics;
        public static bool SendUserStatistics
        {
            get { return sendUserStatistics; }
            set
            {
                sendUserStatistics = value;
                GameMain.Config.SaveNewPlayerConfig();
            }
        }
        public static bool ShowUserStatisticsPrompt { get; set; }

        public bool ShowLanguageSelectionPrompt { get; set; }

        public GameSettings()
        {
            SelectedContentPackages = new HashSet<ContentPackage>();
            ContentPackage.LoadAll(ContentPackage.Folder);
            CompletedTutorialNames = new List<string>();

            LoadDefaultConfig();

            if (WasGameUpdated)
            {
                UpdaterUtil.CleanOldFiles();
                WasGameUpdated = false;
                SaveNewDefaultConfig();
            }

            LoadPlayerConfig();
        }

        public void SetDefaultBindings(XDocument doc = null, bool legacy = false)
        {
            keyMapping = new KeyOrMouse[Enum.GetNames(typeof(InputType)).Length];
            keyMapping[(int)InputType.Run] = new KeyOrMouse(Keys.LeftShift);
            keyMapping[(int)InputType.Attack] = new KeyOrMouse(2);
            keyMapping[(int)InputType.Crouch] = new KeyOrMouse(Keys.LeftControl);
            keyMapping[(int)InputType.Grab] = new KeyOrMouse(Keys.G);
            keyMapping[(int)InputType.Health] = new KeyOrMouse(Keys.H);
            keyMapping[(int)InputType.Ragdoll] = new KeyOrMouse(Keys.Space);
            keyMapping[(int)InputType.Aim] = new KeyOrMouse(1);

            keyMapping[(int)InputType.InfoTab] = new KeyOrMouse(Keys.Tab);
            keyMapping[(int)InputType.Chat] = new KeyOrMouse(Keys.T);
            keyMapping[(int)InputType.RadioChat] = new KeyOrMouse(Keys.R);
            keyMapping[(int)InputType.CrewOrders] = new KeyOrMouse(Keys.C);

            keyMapping[(int)InputType.Voice] = new KeyOrMouse(Keys.V);

            if (Language == "French")
            {
                keyMapping[(int)InputType.Up] = new KeyOrMouse(Keys.Z);
                keyMapping[(int)InputType.Down] = new KeyOrMouse(Keys.S);
                keyMapping[(int)InputType.Left] = new KeyOrMouse(Keys.Q);
                keyMapping[(int)InputType.Right] = new KeyOrMouse(Keys.D);

                keyMapping[(int)InputType.SelectNextCharacter] = new KeyOrMouse(Keys.X);
                keyMapping[(int)InputType.SelectPreviousCharacter] = new KeyOrMouse(Keys.W);
            }
            else
            {
                keyMapping[(int)InputType.Up] = new KeyOrMouse(Keys.W);
                keyMapping[(int)InputType.Down] = new KeyOrMouse(Keys.S);
                keyMapping[(int)InputType.Left] = new KeyOrMouse(Keys.A);
                keyMapping[(int)InputType.Right] = new KeyOrMouse(Keys.D);

                keyMapping[(int)InputType.SelectNextCharacter] = new KeyOrMouse(Keys.Z);
                keyMapping[(int)InputType.SelectPreviousCharacter] = new KeyOrMouse(Keys.X);
            }

            if (legacy)
            {
                keyMapping[(int)InputType.Use] = new KeyOrMouse(0);
                keyMapping[(int)InputType.Shoot] = new KeyOrMouse(0);
                keyMapping[(int)InputType.Select] = new KeyOrMouse(Keys.E);
                keyMapping[(int)InputType.Deselect] = new KeyOrMouse(Keys.E);
            }
            else
            {
                keyMapping[(int)InputType.Use] = new KeyOrMouse(Keys.E);
                keyMapping[(int)InputType.Select] = new KeyOrMouse(0);
                // shoot and deselect are handled in CheckBindings() so that we don't override the legacy settings.
            }
            if (doc != null)
            {
                foreach (XElement subElement in doc.Root.Elements())
                {
                    if (subElement.Name.ToString().ToLowerInvariant() == "keymapping")
                    {
                        LoadKeyBinds(subElement);
                    }
                }
            }
        }

        public void CheckBindings(bool useDefaults)
        {
            foreach (InputType inputType in Enum.GetValues(typeof(InputType)))
            {
                var binding = keyMapping[(int)inputType];
                if (binding == null)
                {
                    switch (inputType)
                    {
                        case InputType.Deselect:
                            if (useDefaults)
                            {
                                binding = new KeyOrMouse(1);
                            }
                            else
                            {
                                // Legacy support
                                var selectKey = keyMapping[(int)InputType.Select];
                                if (selectKey != null && selectKey.Key != Keys.None)
                                {
                                    binding = new KeyOrMouse(selectKey.Key);
                                }
                            }
                            break;
                        case InputType.Shoot:
                            if (useDefaults)
                            {
                                binding = new KeyOrMouse(0);
                            }
                            else
                            {
                                // Legacy support
                                var useKey = keyMapping[(int)InputType.Use];
                                if (useKey != null && useKey.MouseButton.HasValue)
                                {
                                    binding = new KeyOrMouse(useKey.MouseButton.Value);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    if (binding == null)
                    {
                        DebugConsole.ThrowError("Key binding for the input type \"" + inputType + " not set!");
                        binding = new KeyOrMouse(Keys.D1);
                    }
                    keyMapping[(int)inputType] = binding;
                }
            }
        }

        #region Load DefaultConfig
        private void LoadDefaultConfig(bool setLanguage = true)
        {
            XDocument doc = XMLExtensions.TryLoadXml(savePath);

            if (setLanguage || string.IsNullOrEmpty(Language))
            {
                Language = doc.Root.GetAttributeString("language", "English");
            }

            MasterServerUrl = doc.Root.GetAttributeString("masterserverurl", "");

            AutoCheckUpdates = doc.Root.GetAttributeBool("autocheckupdates", true);
            WasGameUpdated = doc.Root.GetAttributeBool("wasgameupdated", false);

            VerboseLogging = doc.Root.GetAttributeBool("verboselogging", false);
            SaveDebugConsoleLogs = doc.Root.GetAttributeBool("savedebugconsolelogs", false);

            QuickStartSubmarineName = doc.Root.GetAttributeString("quickstartsub", "");

            if (doc == null)
            {
                GraphicsWidth = 1024;
                GraphicsHeight = 678;

                MasterServerUrl = "";

                SelectedContentPackages.Add(ContentPackage.List.Any() ? ContentPackage.List[0] : new ContentPackage(""));

                jobPreferences = new List<string>();
                foreach (JobPrefab job in JobPrefab.List)
                {
                    jobPreferences.Add(job.Identifier);
                }
                return;
            }

            XElement graphicsMode = doc.Root.Element("graphicsmode");
            GraphicsWidth   = 0;
            GraphicsHeight  = 0;
            VSyncEnabled    = graphicsMode.GetAttributeBool("vsync", true);

            XElement graphicsSettings = doc.Root.Element("graphicssettings");
            ParticleLimit               = graphicsSettings.GetAttributeInt("particlelimit", 1500);
            LightMapScale               = MathHelper.Clamp(graphicsSettings.GetAttributeFloat("lightmapscale", 0.5f), 0.1f, 1.0f);
            SpecularityEnabled          = graphicsSettings.GetAttributeBool("specularity", true);
            ChromaticAberrationEnabled  = graphicsSettings.GetAttributeBool("chromaticaberration", true);
            HUDScale                    = graphicsSettings.GetAttributeFloat("hudscale", 1.0f);
            InventoryScale              = graphicsSettings.GetAttributeFloat("inventoryscale", 1.0f);
            var losModeStr              = graphicsSettings.GetAttributeString("losmode", "Transparent");
            if (!Enum.TryParse(losModeStr, out losMode))
            {
                losMode = LosMode.Transparent;
            }

#if CLIENT
            if (GraphicsWidth == 0 || GraphicsHeight == 0)
            {
                GraphicsWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                GraphicsHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
#endif

            var windowModeStr = graphicsMode.GetAttributeString("displaymode", "Fullscreen");
            if (!Enum.TryParse(windowModeStr, out WindowMode wm))
            {
                wm = WindowMode.Fullscreen;
            }
            WindowMode = wm;
            
            useSteamMatchmaking = doc.Root.GetAttributeBool("usesteammatchmaking", true);
            requireSteamAuthentication = doc.Root.GetAttributeBool("requiresteamauthentication", true);
            AutoUpdateWorkshopItems = doc.Root.GetAttributeBool("autoupdateworkshopitems", true);

#if DEBUG
            EnableSplashScreen = false;
#else
            EnableSplashScreen = doc.Root.GetAttributeBool("enablesplashscreen", true);
#endif

            AimAssistAmount = doc.Root.GetAttributeFloat("aimassistamount", 0.5f);

            SetDefaultBindings(doc, legacy: false);

            foreach (XElement subElement in doc.Root.Elements())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "keymapping":
                        LoadKeyBinds(subElement);
                        break;
                    case "gameplay":
                        jobPreferences = new List<string>();
                        foreach (XElement ele in subElement.Element("jobpreferences").Elements("job"))
                        {
                            string jobIdentifier = ele.GetAttributeString("identifier", "");
                            if (string.IsNullOrEmpty(jobIdentifier)) continue;
                            jobPreferences.Add(jobIdentifier);
                        }
                        break;
                    case "player":
                        defaultPlayerName = subElement.GetAttributeString("name", "");
                        CharacterHeadIndex = subElement.GetAttributeInt("headindex", CharacterHeadIndex);
                        if (Enum.TryParse(subElement.GetAttributeString("gender", "none"), true, out Gender g))
                        {
                            CharacterGender = g;
                        }
                        if (Enum.TryParse(subElement.GetAttributeString("race", "white"), true, out Race r))
                        {
                            CharacterRace = r;
                        }
                        else
                        {
                            CharacterRace = Race.White;
                        }
                        CharacterHairIndex = subElement.GetAttributeInt("hairindex", -1);
                        CharacterBeardIndex = subElement.GetAttributeInt("beardindex", -1);
                        CharacterMoustacheIndex = subElement.GetAttributeInt("moustacheindex", -1);
                        CharacterFaceAttachmentIndex = subElement.GetAttributeInt("faceattachmentindex", -1);
                        break;
                }
            }

            List<string> missingPackagePaths = new List<string>();
            List<ContentPackage> incompatiblePackages = new List<ContentPackage>();
            foreach (XElement subElement in doc.Root.Elements())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "contentpackage":
                        string path = System.IO.Path.GetFullPath(subElement.GetAttributeString("path", ""));
                        var matchingContentPackage = ContentPackage.List.Find(cp => System.IO.Path.GetFullPath(cp.Path) == path);
                        if (matchingContentPackage == null)
                        {
                            missingPackagePaths.Add(path);
                        }
                        else if (!matchingContentPackage.IsCompatible())
                        {
                            incompatiblePackages.Add(matchingContentPackage);
                        }
                        else
                        {
                            SelectedContentPackages.Add(matchingContentPackage);
                        }
                        break;
                }
            }

            TextManager.LoadTextPacks(SelectedContentPackages);

            //display error messages after all content packages have been loaded
            //to make sure the package that contains text files has been loaded before we attempt to use TextManager
            foreach (string missingPackagePath in missingPackagePaths)
            {
                DebugConsole.ThrowError(TextManager.GetWithVariable("ContentPackageNotFound", "[packagepath]", missingPackagePath));
            }
            foreach (ContentPackage incompatiblePackage in incompatiblePackages)
            {
                DebugConsole.ThrowError(TextManager.GetWithVariables(incompatiblePackage.GameVersion <= new Version(0, 0, 0, 0) ? "IncompatibleContentPackageUnknownVersion" : "IncompatibleContentPackage",
                    new string[3] { "[packagename]", "[packageversion]", "[gameversion]" }, new string[3] { incompatiblePackage.Name, incompatiblePackage.GameVersion.ToString(), GameMain.Version.ToString() }));
            }
            foreach (ContentPackage contentPackage in SelectedContentPackages)
            {
                bool packageOk = contentPackage.VerifyFiles(out List<string> errorMessages);
                if (!packageOk)
                {
                    DebugConsole.ThrowError("Error in content package \"" + contentPackage.Name + "\":\n" + string.Join("\n", errorMessages));
                    continue;
                }
                foreach (ContentFile file in contentPackage.Files)
                {
                    ToolBox.IsProperFilenameCase(file.Path);
                }
            }
            if (!SelectedContentPackages.Any())
            {
                var availablePackage = ContentPackage.List.FirstOrDefault(cp => cp.IsCompatible() && cp.CorePackage);
                if (availablePackage != null)
                {
                    SelectedContentPackages.Add(availablePackage);
                }
            }

            //save to get rid of the invalid selected packages in the config file
            if (missingPackagePaths.Count > 0 || incompatiblePackages.Count > 0) { SaveNewPlayerConfig(); }
        }
        #endregion

        #region Save DefaultConfig
        private void SaveNewDefaultConfig()
        {
            XDocument doc = new XDocument();

            if (doc.Root == null)
            {
                doc.Add(new XElement("config"));
            }

            doc.Root.Add(
                new XAttribute("language", TextManager.Language),
                new XAttribute("masterserverurl", MasterServerUrl),
                new XAttribute("autocheckupdates", AutoCheckUpdates),
                new XAttribute("musicvolume", musicVolume),
                new XAttribute("soundvolume", soundVolume),
                new XAttribute("voicechatvolume", voiceChatVolume),
                new XAttribute("verboselogging", VerboseLogging),
                new XAttribute("savedebugconsolelogs", SaveDebugConsoleLogs),
                new XAttribute("enablesplashscreen", EnableSplashScreen),
                new XAttribute("usesteammatchmaking", useSteamMatchmaking),
                new XAttribute("quickstartsub", QuickStartSubmarineName),
                new XAttribute("requiresteamauthentication", requireSteamAuthentication),
                new XAttribute("aimassistamount", aimAssistAmount));

            if (!ShowUserStatisticsPrompt)
            {
                doc.Root.Add(new XAttribute("senduserstatistics", sendUserStatistics));
            }

            if (WasGameUpdated)
            {
                doc.Root.Add(new XAttribute("wasgameupdated", true));
            }
            
            XElement gMode = doc.Root.Element("graphicsmode");
            if (gMode == null)
            {
                gMode = new XElement("graphicsmode");
                doc.Root.Add(gMode);
            }
            if (GraphicsWidth == 0 || GraphicsHeight == 0)
            {
                gMode.ReplaceAttributes(new XAttribute("displaymode", windowMode));
            }
            else
            {
                gMode.ReplaceAttributes(
                    new XAttribute("width", GraphicsWidth),
                    new XAttribute("height", GraphicsHeight),
                    new XAttribute("vsync", VSyncEnabled),
                    new XAttribute("displaymode", windowMode));
            }

            XElement gSettings = doc.Root.Element("graphicssettings");
            if (gSettings == null)
            {
                gSettings = new XElement("graphicssettings");
                doc.Root.Add(gSettings);
            }

            gSettings.ReplaceAttributes(
                new XAttribute("particlelimit", ParticleLimit),
                new XAttribute("lightmapscale", LightMapScale),
                new XAttribute("specularity", SpecularityEnabled),
                new XAttribute("chromaticaberration", ChromaticAberrationEnabled),
                new XAttribute("losmode", LosMode),
                new XAttribute("hudscale", HUDScale),
                new XAttribute("inventoryscale", InventoryScale));

            foreach (ContentPackage contentPackage in SelectedContentPackages)
            {
                if (contentPackage.Path.Contains(vanillaContentPackagePath))
                {
                    doc.Root.Add(new XElement("contentpackage", new XAttribute("path", contentPackage.Path)));
                    break;
                }
            }

            var keyMappingElement = new XElement("keymapping");
            doc.Root.Add(keyMappingElement);
            for (int i = 0; i < keyMapping.Length; i++)
            {
                if (keyMapping[i].MouseButton == null)
                {
                    keyMappingElement.Add(new XAttribute(((InputType)i).ToString(), keyMapping[i].Key));
                }
                else
                {
                    keyMappingElement.Add(new XAttribute(((InputType)i).ToString(), keyMapping[i].MouseButton));
                }
            }

            var gameplay = new XElement("gameplay");
            var jobPreferences = new XElement("jobpreferences");
            foreach (string jobName in JobPreferences)
            {
                jobPreferences.Add(new XElement("job", new XAttribute("identifier", jobName)));
            }
            gameplay.Add(jobPreferences);
            doc.Root.Add(gameplay);

            var playerElement = new XElement("player",
                new XAttribute("name", defaultPlayerName ?? ""),
                new XAttribute("headindex", CharacterHeadIndex),
                new XAttribute("gender", CharacterGender),
                new XAttribute("race", CharacterRace),
                new XAttribute("hairindex", CharacterHairIndex),
                new XAttribute("beardindex", CharacterBeardIndex),
                new XAttribute("moustacheindex", CharacterMoustacheIndex),
                new XAttribute("faceattachmentindex", CharacterFaceAttachmentIndex));
            doc.Root.Add(playerElement);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                NewLineOnAttributes = true
            };

            try
            {
                using (var writer = XmlWriter.Create(savePath, settings))
                {
                    doc.WriteTo(writer);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                DebugConsole.ThrowError("Saving game settings failed.", e);
                GameAnalyticsManager.AddErrorEventOnce("GameSettings.Save:SaveFailed", GameAnalyticsSDK.Net.EGAErrorSeverity.Error,
                    "Saving game settings failed.\n" + e.Message + "\n" + e.StackTrace);
            }
        }
        #endregion

        #region Load PlayerConfig
        public void LoadPlayerConfig()
        {
            bool fileFound = LoadPlayerConfigInternal();
            CheckBindings(!fileFound);
            if (!fileFound)
            {
                ShowLanguageSelectionPrompt = true;
                ShowUserStatisticsPrompt = true;
                SaveNewPlayerConfig();
            }
        }

        // TODO: DRY
        /// <summary>
        /// Returns false if no player config file was found, in which case a new file is created.
        /// </summary>
        private bool LoadPlayerConfigInternal()
        {
            XDocument doc = XMLExtensions.LoadXml(playerSavePath);

            if (doc == null || doc.Root == null)
            {
                ShowUserStatisticsPrompt = true;
                return false;
            }

            Language = doc.Root.GetAttributeString("language", Language);
            AutoCheckUpdates = doc.Root.GetAttributeBool("autocheckupdates", AutoCheckUpdates);
            sendUserStatistics = doc.Root.GetAttributeBool("senduserstatistics", true);

            XElement graphicsMode = doc.Root.Element("graphicsmode");
            GraphicsWidth = graphicsMode.GetAttributeInt("width", GraphicsWidth);
            GraphicsHeight = graphicsMode.GetAttributeInt("height", GraphicsHeight);
            VSyncEnabled = graphicsMode.GetAttributeBool("vsync", VSyncEnabled);

            XElement graphicsSettings = doc.Root.Element("graphicssettings");
            ParticleLimit = graphicsSettings.GetAttributeInt("particlelimit", ParticleLimit);
            LightMapScale = MathHelper.Clamp(graphicsSettings.GetAttributeFloat("lightmapscale", LightMapScale), 0.1f, 1.0f);
            SpecularityEnabled = graphicsSettings.GetAttributeBool("specularity", SpecularityEnabled);
            ChromaticAberrationEnabled = graphicsSettings.GetAttributeBool("chromaticaberration", ChromaticAberrationEnabled);
            HUDScale = graphicsSettings.GetAttributeFloat("hudscale", HUDScale);
            InventoryScale = graphicsSettings.GetAttributeFloat("inventoryscale", InventoryScale);
            var losModeStr = graphicsSettings.GetAttributeString("losmode", "Transparent");
            if (!Enum.TryParse(losModeStr, out losMode))
            {
                losMode = LosMode.Transparent;
            }

#if CLIENT
            if (GraphicsWidth == 0 || GraphicsHeight == 0)
            {
                GraphicsWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                GraphicsHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
#endif

            var windowModeStr = graphicsMode.GetAttributeString("displaymode", "Fullscreen");
            if (!Enum.TryParse(windowModeStr, out windowMode))
            {
                windowMode = WindowMode.Fullscreen;
            }

            XElement audioSettings = doc.Root.Element("audio");
            if (audioSettings != null)
            {
                SoundVolume = audioSettings.GetAttributeFloat("soundvolume", SoundVolume);
                MusicVolume = audioSettings.GetAttributeFloat("musicvolume", MusicVolume);
                VoiceChatVolume = audioSettings.GetAttributeFloat("voicechatvolume", VoiceChatVolume);
                MuteOnFocusLost = audioSettings.GetAttributeBool("muteonfocuslost", false);
                UseDirectionalVoiceChat = audioSettings.GetAttributeBool("usedirectionalvoicechat", true);
                string voiceSettingStr = audioSettings.GetAttributeString("voicesetting", "Disabled");
                VoiceCaptureDevice = audioSettings.GetAttributeString("voicecapturedevice", "");
                NoiseGateThreshold = audioSettings.GetAttributeFloat("noisegatethreshold", -45);
                var voiceSetting = VoiceMode.Disabled;
                if (Enum.TryParse(voiceSettingStr, out voiceSetting))
                {
                    VoiceSetting = voiceSetting;
                }
            }
            
            useSteamMatchmaking = doc.Root.GetAttributeBool("usesteammatchmaking", useSteamMatchmaking);
            requireSteamAuthentication = doc.Root.GetAttributeBool("requiresteamauthentication", requireSteamAuthentication);

            EnableSplashScreen = doc.Root.GetAttributeBool("enablesplashscreen", EnableSplashScreen);

            PauseOnFocusLost = doc.Root.GetAttributeBool("pauseonfocuslost", PauseOnFocusLost);
            AimAssistAmount = doc.Root.GetAttributeFloat("aimassistamount", AimAssistAmount);
            EnableMouseLook = doc.Root.GetAttributeBool("enablemouselook", EnableMouseLook);

            CrewMenuOpen = doc.Root.GetAttributeBool("crewmenuopen", CrewMenuOpen);
            ChatOpen = doc.Root.GetAttributeBool("chatopen", ChatOpen);

            CampaignDisclaimerShown = doc.Root.GetAttributeBool("campaigndisclaimershown", false);
            EditorDisclaimerShown = doc.Root.GetAttributeBool("editordisclaimershown", false);

            foreach (XElement subElement in doc.Root.Elements())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "keymapping":
                        LoadKeyBinds(subElement);
                        break;
                    case "gameplay":
                        jobPreferences = new List<string>();
                        foreach (XElement ele in subElement.Element("jobpreferences").Elements("job"))
                        {
                            string jobIdentifier = ele.GetAttributeString("identifier", "");
                            if (string.IsNullOrEmpty(jobIdentifier)) continue;
                            jobPreferences.Add(jobIdentifier);
                        }
                        break;
                    case "player":
                        defaultPlayerName = subElement.GetAttributeString("name", defaultPlayerName);
                        CharacterHeadIndex = subElement.GetAttributeInt("headindex", CharacterHeadIndex);
                        if (Enum.TryParse(subElement.GetAttributeString("gender", "none"), true, out Gender g))
                        {
                            CharacterGender = g;
                        }
                        if (Enum.TryParse(subElement.GetAttributeString("race", "white"), true, out Race r))
                        {
                            CharacterRace = r;
                        }
                        else
                        {
                            CharacterRace = Race.White;
                        }
                        CharacterHairIndex = subElement.GetAttributeInt("hairindex", CharacterHairIndex);
                        CharacterBeardIndex = subElement.GetAttributeInt("beardindex", CharacterBeardIndex);
                        CharacterMoustacheIndex = subElement.GetAttributeInt("moustacheindex", CharacterMoustacheIndex);
                        CharacterFaceAttachmentIndex = subElement.GetAttributeInt("faceattachmentindex", CharacterFaceAttachmentIndex);
                        break;
                    case "tutorials":
                        foreach (XElement tutorialElement in subElement.Elements())
                        {
                            CompletedTutorialNames.Add(tutorialElement.GetAttributeString("name", ""));
                        }
                        break;
                }
            }

            UnsavedSettings = false;

            selectedContentPackagePaths = new HashSet<string>();

            foreach (XElement subElement in doc.Root.Elements())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "contentpackage":
                        string path = System.IO.Path.GetFullPath(subElement.GetAttributeString("path", ""));
                        selectedContentPackagePaths.Add(path);
                        break;
                }
            }

            LoadContentPackages(selectedContentPackagePaths);
            return true;
        }

        public void ReloadContentPackages()
        {
            LoadContentPackages(selectedContentPackagePaths);
        }

        private void LoadContentPackages(IEnumerable<string> contentPackagePaths)
        {
            var missingPackagePaths = new List<string>();
            var incompatiblePackages = new List<ContentPackage>();
            SelectedContentPackages.Clear();
            foreach (string path in contentPackagePaths)
            {
                var matchingContentPackage = ContentPackage.List.Find(cp => System.IO.Path.GetFullPath(cp.Path) == path);

                if (matchingContentPackage == null)
                {
                    missingPackagePaths.Add(path);
                }
                else if (!matchingContentPackage.IsCompatible())
                {
                    incompatiblePackages.Add(matchingContentPackage);
                }
                else
                {
                    SelectedContentPackages.Add(matchingContentPackage);
                }
            }

            TextManager.LoadTextPacks(SelectedContentPackages);

            foreach (ContentPackage contentPackage in SelectedContentPackages)
            {
                bool packageOk = contentPackage.VerifyFiles(out List<string> errorMessages);
                if (!packageOk)
                {
                    DebugConsole.ThrowError("Error in content package \"" + contentPackage.Name + "\":\n" + string.Join("\n", errorMessages));
                    continue;
                }
                foreach (ContentFile file in contentPackage.Files)
                {
                    ToolBox.IsProperFilenameCase(file.Path);
                }
            }

            EnsureCoreContentPackageSelected();

            //save to get rid of the invalid selected packages in the config file
            if (missingPackagePaths.Count > 0 || incompatiblePackages.Count > 0) { SaveNewPlayerConfig(); }

            //display error messages after all content packages have been loaded
            //to make sure the package that contains text files has been loaded before we attempt to use TextManager
            foreach (string missingPackagePath in missingPackagePaths)
            {
                DebugConsole.ThrowError(TextManager.GetWithVariable("ContentPackageNotFound", "[packagepath]", missingPackagePath));
            }
            foreach (ContentPackage incompatiblePackage in incompatiblePackages)
            {
                DebugConsole.ThrowError(TextManager.GetWithVariables(incompatiblePackage.GameVersion <= new Version(0, 0, 0, 0) ? "IncompatibleContentPackageUnknownVersion" : "IncompatibleContentPackage", 
                    new string[3] { "[packagename]", "[packageversion]", "[gameversion]" }, new string[3] { incompatiblePackage.Name, incompatiblePackage.GameVersion.ToString(), GameMain.Version.ToString() }));
            }
        }

        public void EnsureCoreContentPackageSelected()
        {
            if (SelectedContentPackages.Any(cp => cp.CorePackage)) { return; }

            if (GameMain.VanillaContent != null)
            {
                SelectedContentPackages.Add(GameMain.VanillaContent);
            }
            else
            {
                var availablePackage = ContentPackage.List.FirstOrDefault(cp => cp.IsCompatible() && cp.CorePackage);
                if (availablePackage != null)
                {
                    SelectedContentPackages.Add(availablePackage);
                }
            }
        }

        #endregion

        #region Save PlayerConfig
        public void SaveNewPlayerConfig()
        {
            XDocument doc = new XDocument();
            UnsavedSettings = false;

            if (doc.Root == null)
            {
                doc.Add(new XElement("config"));
            }

            doc.Root.Add(
                new XAttribute("language", TextManager.Language),
                new XAttribute("masterserverurl", MasterServerUrl),
                new XAttribute("autocheckupdates", AutoCheckUpdates),
                new XAttribute("musicvolume", musicVolume),
                new XAttribute("soundvolume", soundVolume),
                new XAttribute("verboselogging", VerboseLogging),
                new XAttribute("savedebugconsolelogs", SaveDebugConsoleLogs),
                new XAttribute("enablesplashscreen", EnableSplashScreen),
                new XAttribute("usesteammatchmaking", useSteamMatchmaking),
                new XAttribute("quickstartsub", QuickStartSubmarineName),
                new XAttribute("requiresteamauthentication", requireSteamAuthentication),
                new XAttribute("autoupdateworkshopitems", AutoUpdateWorkshopItems),
                new XAttribute("pauseonfocuslost", PauseOnFocusLost),
                new XAttribute("aimassistamount", aimAssistAmount),
                new XAttribute("enablemouselook", EnableMouseLook),
                new XAttribute("chatopen", ChatOpen),
                new XAttribute("crewmenuopen", CrewMenuOpen),
                new XAttribute("campaigndisclaimershown", CampaignDisclaimerShown),
                new XAttribute("editordisclaimershown", EditorDisclaimerShown));

            if (!ShowUserStatisticsPrompt)
            {
                doc.Root.Add(new XAttribute("senduserstatistics", sendUserStatistics));
            }

            XElement gMode = doc.Root.Element("graphicsmode");
            if (gMode == null)
            {
                gMode = new XElement("graphicsmode");
                doc.Root.Add(gMode);
            }

            if (GraphicsWidth == 0 || GraphicsHeight == 0)
            {
                gMode.ReplaceAttributes(new XAttribute("displaymode", windowMode));
            }
            else
            {
                gMode.ReplaceAttributes(
                    new XAttribute("width", GraphicsWidth),
                    new XAttribute("height", GraphicsHeight),
                    new XAttribute("vsync", VSyncEnabled),
                    new XAttribute("displaymode", windowMode));
            }
            
            XElement audio = doc.Root.Element("audio");
            if (audio == null)
            {
                audio = new XElement("audio");
                doc.Root.Add(audio);
            }
            audio.ReplaceAttributes(
                new XAttribute("musicvolume", musicVolume),
                new XAttribute("soundvolume", soundVolume),
                new XAttribute("muteonfocuslost", MuteOnFocusLost),
                new XAttribute("usedirectionalvoicechat", UseDirectionalVoiceChat),
                new XAttribute("voicesetting", VoiceSetting),
                new XAttribute("voicecapturedevice", VoiceCaptureDevice ?? ""),
                new XAttribute("noisegatethreshold", NoiseGateThreshold));

            XElement gSettings = doc.Root.Element("graphicssettings");
            if (gSettings == null)
            {
                gSettings = new XElement("graphicssettings");
                doc.Root.Add(gSettings);
            }

            gSettings.ReplaceAttributes(
                new XAttribute("particlelimit", ParticleLimit),
                new XAttribute("lightmapscale", LightMapScale),
                new XAttribute("specularity", SpecularityEnabled),
                new XAttribute("chromaticaberration", ChromaticAberrationEnabled),
                new XAttribute("losmode", LosMode),
                new XAttribute("hudscale", HUDScale),
                new XAttribute("inventoryscale", InventoryScale));

            foreach (ContentPackage contentPackage in SelectedContentPackages)
            {
                doc.Root.Add(new XElement("contentpackage",
                    new XAttribute("path", contentPackage.Path)));
            }

            var keyMappingElement = new XElement("keymapping");
            doc.Root.Add(keyMappingElement);
            for (int i = 0; i < keyMapping.Length; i++)
            {
                if (keyMapping[i].MouseButton == null)
                {
                    keyMappingElement.Add(new XAttribute(((InputType)i).ToString(), keyMapping[i].Key));
                }
                else
                {
                    keyMappingElement.Add(new XAttribute(((InputType)i).ToString(), keyMapping[i].MouseButton));
                }
            }

            var gameplay = new XElement("gameplay");
            var jobPreferences = new XElement("jobpreferences");
            foreach (string jobName in JobPreferences)
            {
                jobPreferences.Add(new XElement("job", new XAttribute("identifier", jobName)));
            }
            gameplay.Add(jobPreferences);
            doc.Root.Add(gameplay);

            var playerElement = new XElement("player",
                new XAttribute("name", defaultPlayerName ?? ""),
                new XAttribute("headindex", CharacterHeadIndex),
                new XAttribute("gender", CharacterGender),
                new XAttribute("race", CharacterRace),
                new XAttribute("hairindex", CharacterHairIndex),
                new XAttribute("beardindex", CharacterBeardIndex),
                new XAttribute("moustacheindex", CharacterMoustacheIndex),
                new XAttribute("faceattachmentindex", CharacterFaceAttachmentIndex));
            doc.Root.Add(playerElement);

#if CLIENT
            if (Tutorial.Tutorials != null)
            {
                foreach (Tutorial tutorial in Tutorial.Tutorials)
                {
                    if (tutorial.Completed && !CompletedTutorialNames.Contains(tutorial.Identifier))
                    {
                        CompletedTutorialNames.Add(tutorial.Identifier);
                    }
                }
            }
#endif
            var tutorialElement = new XElement("tutorials");
            foreach (string tutorialName in CompletedTutorialNames)
            {
                tutorialElement.Add(new XElement("Tutorial", new XAttribute("name", tutorialName)));
            }
            doc.Root.Add(tutorialElement);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                NewLineOnAttributes = true
            };

            try
            {
                using (var writer = XmlWriter.Create(playerSavePath, settings))
                {
                    doc.WriteTo(writer);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                DebugConsole.ThrowError("Saving game settings failed.", e);
                GameAnalyticsManager.AddErrorEventOnce("GameSettings.Save:SaveFailed", GameAnalyticsSDK.Net.EGAErrorSeverity.Error,
                    "Saving game settings failed.\n" + e.Message + "\n" + e.StackTrace);
            }
        }
        #endregion

        private void LoadKeyBinds(XElement element)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (!Enum.TryParse(attribute.Name.ToString(), true, out InputType inputType)) { continue; }
                
                if (int.TryParse(attribute.Value.ToString(), out int mouseButton))
                {
                    keyMapping[(int)inputType] = new KeyOrMouse(mouseButton);
                }
                else
                {
                    if (Enum.TryParse(attribute.Value.ToString(), true, out Keys key))
                    {
                        keyMapping[(int)inputType] = new KeyOrMouse(key);
                    }
                }                
            }
        }

        public void ResetToDefault()
        {
            LoadDefaultConfig();
            CheckBindings(true);
            SaveNewPlayerConfig();
        }

        public KeyOrMouse KeyBind(InputType inputType)
        {
            return keyMapping[(int)inputType];
        }
    }
}
