﻿// FFXIVAPP.Client
// Initializer.cs
// 
// © 2013 Ryan Wilson

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using FFXIVAPP.Client.Helpers;
using FFXIVAPP.Client.Memory;
using FFXIVAPP.Client.Models;
using FFXIVAPP.Client.Properties;
using FFXIVAPP.Client.ViewModels;
using FFXIVAPP.Client.Views;
using FFXIVAPP.Common.Helpers;
using FFXIVAPP.Common.Utilities;
using Newtonsoft.Json.Linq;
using NLog;
using SmartAssembly.Attributes;

#endregion

namespace FFXIVAPP.Client
{
    [DoNotObfuscate]
    internal static class Initializer
    {
        #region Declarations

        private static ChatLogWorker _chatLogWorker;
        private static MonsterWorker _monsterWorker;
        private static NPCWorker _npcWorker;
        private static PlayerInfoWorker _playerInfoWorker;

        #endregion

        /// <summary>
        /// </summary>
        public static void SetupCurrentUICulture()
        {
            var cultureInfo = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var currentCulture = new CultureInfo(cultureInfo);
            Constants.CultureInfo = Settings.Default.CultureSet ? Settings.Default.Culture : currentCulture;
            Settings.Default.CultureSet = true;
        }

        /// <summary>
        /// </summary>
        public static void LoadChatCodes()
        {
            if (Constants.XChatCodes != null)
            {
                foreach (var xElement in Constants.XChatCodes.Descendants()
                                                  .Elements("Code"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xDescription = (string) xElement.Element("Description");
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xDescription))
                    {
                        continue;
                    }
                    Constants.ChatCodes.Add(xKey, xDescription);
                }
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("LoadedChatCodes : {0} KeyValuePairs", Constants.ChatCodes.Count));
            }
        }

        /// <summary>
        /// </summary>
        public static void LoadAutoTranslate()
        {
            if (Constants.XAutoTranslate != null)
            {
                foreach (var xElement in Constants.XAutoTranslate.Descendants()
                                                  .Elements("Code"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xValue = (string) xElement.Element(Settings.Default.GameLanguage);
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xValue))
                    {
                        continue;
                    }
                    Constants.AutoTranslate.Add(xKey, xValue);
                }
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("LoadedAutoTranslate : {0} KeyValuePairs", Constants.AutoTranslate.Count));
            }
        }

        /// <summary>
        /// </summary>
        public static void LoadColors()
        {
            if (Constants.XColors != null)
            {
                foreach (var xElement in Constants.XColors.Descendants()
                                                  .Elements("Color"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xValue = (string) xElement.Element("Value");
                    var xDescription = (string) xElement.Element("Description");
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xValue))
                    {
                        continue;
                    }
                    if (Constants.ChatCodes.ContainsKey(xKey))
                    {
                        if (xDescription.ToLower()
                                        .Contains("unknown") || String.IsNullOrWhiteSpace(xDescription))
                        {
                            xDescription = Constants.ChatCodes[xKey];
                        }
                    }
                    Constants.Colors.Add(xKey, new[]
                    {
                        xValue, xDescription
                    });
                }
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("LoadedColors : {0} KeyValuePairs", Constants.Colors.Count));
            }
        }

        /// <summary>
        /// </summary>
        public static void LoadPlugins()
        {
            App.Plugins.LoadPlugins(Directory.GetCurrentDirectory() + @"\Plugins");
            foreach (PluginInstance pluginInstance in App.Plugins.Loaded)
            {
                try
                {
                    var tabItem = pluginInstance.Instance.CreateTab();
                    var iconfile = String.Format("{0}\\{1}", Path.GetDirectoryName(pluginInstance.AssemblyPath), pluginInstance.Instance.Icon);
                    var icon = new BitmapImage(new Uri(Common.Constants.DefaultIcon));
                    icon = File.Exists(iconfile) ? new BitmapImage(new Uri(iconfile)) : icon;
                    tabItem.HeaderTemplate = TabItemHelper.ImageHeader(icon, pluginInstance.Instance.FriendlyName);
                    var info = new Dictionary<string, string>();
                    info.Add("Icon", pluginInstance.Instance.Icon);
                    info.Add("Description", pluginInstance.Instance.Description);
                    info.Add("Copyright", pluginInstance.Instance.Copyright);
                    info.Add("Version", pluginInstance.Instance.Version);
                    AppViewModel.Instance.PluginTabItems.Add(tabItem);
                }
                catch (AppException ex)
                {
                    Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
                }
            }
            AppViewModel.Instance.HasPlugins = App.Plugins.Loaded.Count > 0;
        }

        /// <summary>
        /// </summary>
        public static void SetGlobals()
        {
            Constants.CharacterName = Settings.Default.CharacterName;
            Constants.ServerName = Settings.Default.ServerName;
            Constants.GameLanguage = Settings.Default.GameLanguage;
        }

        /// <summary>
        /// </summary>
        public static void GetHomePlugin()
        {
            switch (Settings.Default.HomePlugin)
            {
                case "Parse":
                    SetHomePlugin(0);
                    break;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="pluginIndex"></param>
        private static void SetHomePlugin(int pluginIndex)
        {
            ShellView.View.ShellViewTC.SelectedIndex = 1;
            ShellView.View.PluginsTC.SelectedIndex = pluginIndex;
        }

        /// <summary>
        /// </summary>
        public static void SetCharacter()
        {
            var name = String.Format("{0} {1}", Settings.Default.FirstName, Settings.Default.LastName);
            Settings.Default.CharacterName = StringHelper.TrimAndCleanSpaces(name);
        }

        /// <summary>
        /// </summary>
        public static void CheckUpdates()
        {
            Func<bool> updateCheck = delegate
            {
                try
                {
                    File.Delete("FFXIVAPP.Updater.Backup.exe");
                }
                catch (Exception ex)
                {
                }
                var current = Assembly.GetExecutingAssembly()
                                      .GetName()
                                      .Version.ToString();
                AppViewModel.Instance.CurrentVersion = current;
                var request = (HttpWebRequest) WebRequest.Create(String.Format("http://ffxiv-app.com/Json/CurrentVersion/"));
                request.UserAgent = "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_3; en-US) AppleWebKit/533.4 (KHTML, like Gecko) Chrome/5.0.375.70 Safari/533.4";
                request.Headers.Add("Accept-Language", "en;q=0.8");
                request.ContentType = "application/json; charset=utf-8";
                var response = (HttpWebResponse) request.GetResponse();
                var responseText = "";
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    try
                    {
                        responseText = streamReader.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                    }
                }
                if (response.StatusCode != HttpStatusCode.OK || String.IsNullOrWhiteSpace(responseText))
                {
                    AppViewModel.Instance.HasNewVersion = false;
                    AppViewModel.Instance.LatestVersion = "Unknown";
                }
                else
                {
                    var jsonResult = JObject.Parse(responseText);
                    var latest = jsonResult["Version"].ToString();
                    var updateNotes = jsonResult["Notes"].ToList();
                    var enabledFeatures = jsonResult["Features"];
                    try
                    {
                        foreach (var feature in enabledFeatures)
                        {
                            var key = feature["Hash"].ToString();
                            var enabled = (bool) feature["Enabled"];
                            switch (key)
                            {
                                case "E9FA3917-ACEB-47AE-88CC-58AB014058F5":
                                    XIVDBViewModel.Instance.MonsterUploadEnabled = enabled;
                                    break;
                                case "6D2DB102-B1AE-4249-9E73-4ABC7B1947BC":
                                    XIVDBViewModel.Instance.NPCUploadEnabled = enabled;
                                    break;
                                case "D95ADD76-7DA7-4692-AD00-DB12F2853908":
                                    XIVDBViewModel.Instance.KillUploadEnabled = enabled;
                                    break;
                                case "6A50A13B-BA83-45D7-862F-F110049E7E78":
                                    XIVDBViewModel.Instance.LootUploadEnabled = enabled;
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    try
                    {
                        foreach (var note in updateNotes.Select(updateNote => updateNote.Value<string>()))
                        {
                            //DispatcherHelper.Invoke(delegate
                            //{
                            //    MainView.View.UpdateNotesSP.Children.Add(new Label{Content = note});
                            //});
                            AppViewModel.Instance.UpdateNotes.Add(note);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowMessage("Error", ex.Message);
                    }
                    AppViewModel.Instance.DownloadUri = jsonResult["DownloadUri"].ToString();
                    AppViewModel.Instance.LatestVersion = (latest == "Unknown") ? "Unknown" : String.Format("3{0}", latest.Substring(1));
                    switch (latest)
                    {
                        case "Unknown":
                            AppViewModel.Instance.HasNewVersion = false;
                            break;
                        default:
                            var lver = latest.Split('.');
                            var cver = current.Split('.');
                            int lmajor = 0, lminor = 0, lbuild = 0, lrevision = 0;
                            int cmajor = 0, cminor = 0, cbuild = 0, crevision = 0;
                            try
                            {
                                lmajor = Int32.Parse(lver[0]);
                                lminor = Int32.Parse(lver[1]);
                                lbuild = Int32.Parse(lver[2]);
                                lrevision = Int32.Parse(lver[3]);
                                cmajor = Int32.Parse(cver[0]);
                                cminor = Int32.Parse(cver[1]);
                                cbuild = Int32.Parse(cver[2]);
                                crevision = Int32.Parse(cver[3]);
                            }
                            catch (Exception ex)
                            {
                                AppViewModel.Instance.HasNewVersion = false;
                                Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
                            }
                            if (lmajor <= cmajor)
                            {
                                if (lminor <= cminor)
                                {
                                    if (lbuild == cbuild)
                                    {
                                        AppViewModel.Instance.HasNewVersion = lrevision > crevision;
                                        break;
                                    }
                                    AppViewModel.Instance.HasNewVersion = lbuild > cbuild;
                                    break;
                                }
                                AppViewModel.Instance.HasNewVersion = true;
                                break;
                            }
                            AppViewModel.Instance.HasNewVersion = true;
                            break;
                    }
                }
                if (AppViewModel.Instance.HasNewVersion)
                {
                    var title = AppViewModel.Instance.Locale["app_DownloadNoticeHeader"];
                    var message = AppViewModel.Instance.Locale["app_DownloadNoticeMessage"];
                    MessageBoxHelper.ShowMessageAsync(title, message, () => ShellView.CloseApplication(true), delegate { });
                }
                var uri = "http://ffxiv-app.com/Analytics/Google/?eCategory=Application Launch&eAction=Version Check&eLabel=FFXIVAPP";
                DispatcherHelper.Invoke(() => MainView.View.GoogleAnalytics.Navigate(uri));
                return true;
            };
            updateCheck.BeginInvoke(null, null);
        }

        /// <summary>
        /// </summary>
        public static void SetSignatures()
        {
            var signatures = AppViewModel.Instance.Signatures;
            signatures.Clear();
            signatures.Add(new Signature
            {
                Key = "GAMEMAIN",
                Value = "47616D654D61696E000000",
                Offset = 1176
            });
            signatures.Add(new Signature
            {
                Key = "CHARMAP",
                Value = "DB0FC93F6F1283??????????000000??DB0FC93F6F1283????????00",
                Offset = 780
            });
            //+3436 list of agro 
            //+5744 agro count
            signatures.Add(new Signature
            {
                Key = "NPCMAP",
                Value = "3E000000????????4000000001000000000000000001000000",
                Offset = 2444
            });
            signatures.Add(new Signature
            {
                Key = "MAP",
                Value = "F783843E????????DB0FC93F6F12833A",
                Offset = 784
            });
            signatures.Add(new Signature
            {
                Key = "TARGET",
                Value = "DB0FC93F6F12833ADB0FC940920A063F",
                Offset = 224
            });
        }

        /// <summary>
        /// </summary>
        /// <returns> </returns>
        private static int GetProcessID()
        {
            if (Constants.IsOpen && Constants.ProcessID > 0)
            {
                try
                {
                    Process.GetProcessById(Constants.ProcessID);
                    return Constants.ProcessID;
                }
                catch (ArgumentException ex)
                {
                    Constants.IsOpen = false;
                    Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
                }
            }
            Constants.ProcessIDs = Process.GetProcessesByName("ffxiv");
            if (Constants.ProcessIDs.Length == 0)
            {
                Constants.IsOpen = false;
                return -1;
            }
            Constants.IsOpen = true;
            foreach (var process in Constants.ProcessIDs)
            {
                SettingsView.View.PIDSelect.Items.Add(process.Id);
            }
            SettingsView.View.PIDSelect.SelectedIndex = 0;
            UpdateProcessID(Constants.ProcessIDs.First()
                                     .Id);
            return Constants.ProcessIDs.First()
                            .Id;
        }

        /// <summary>
        /// </summary>
        public static void SetProcessID()
        {
            StopMemoryWorkers();
            if (SettingsView.View.PIDSelect.Text == "")
            {
                return;
            }
            UpdateProcessID(Convert.ToInt32(SettingsView.View.PIDSelect.Text));
            StartMemoryWorkers();
        }

        /// <summary>
        /// </summary>
        public static void ResetProcessID()
        {
            Constants.ProcessID = -1;
        }

        /// <summary>
        /// </summary>
        /// <param name="pid"> </param>
        private static void UpdateProcessID(int pid)
        {
            Constants.ProcessID = pid;
        }

        /// <summary>
        /// </summary>
        public static void StartMemoryWorkers()
        {
            StopMemoryWorkers();
            var id = SettingsView.View.PIDSelect.Text == "" ? GetProcessID() : Constants.ProcessID;
            Constants.IsOpen = true;
            if (id < 0)
            {
                Constants.IsOpen = false;
                return;
            }
            var process = Process.GetProcessById(id);
            MemoryHandler.Instance.SetProcess(process);
            MemoryHandler.Instance.SigScanner.LoadOffsets(AppViewModel.Instance.Signatures);
            _chatLogWorker = new ChatLogWorker();
            _chatLogWorker.StartScanning();
            _monsterWorker = new MonsterWorker();
            _monsterWorker.StartScanning();
            _npcWorker = new NPCWorker();
            _npcWorker.StartScanning();
            _playerInfoWorker = new PlayerInfoWorker();
            _playerInfoWorker.StartScanning();
        }

        /// <summary>
        /// </summary>
        public static void SetupPlugins()
        {
            // get official plugin logos
            var parseLogo = new BitmapImage(new Uri(Common.Constants.AppPack + "Resources/Media/Icons/Parse.png"));
            // setup headers for existing plugins
            ShellView.View.ParsePlugin.HeaderTemplate = TabItemHelper.ImageHeader(parseLogo, "Parse");
            // append third party plugins
            foreach (var pluginTabItem in AppViewModel.Instance.PluginTabItems)
            {
                ShellView.View.PluginsTC.Items.Add(pluginTabItem);
            }
        }

        public static void UpdatePluginConstants()
        {
            ConstantsHelper.UpdatePluginConstants();
        }

        /// <summary>
        /// </summary>
        public static void StopMemoryWorkers()
        {
            if (_chatLogWorker != null)
            {
                _chatLogWorker.StopScanning();
                _chatLogWorker.Dispose();
            }
            if (_monsterWorker != null)
            {
                _monsterWorker.StopScanning();
                _monsterWorker.Dispose();
            }
            if (_npcWorker != null)
            {
                _npcWorker.StopScanning();
                _npcWorker.Dispose();
            }
            if (_playerInfoWorker != null)
            {
                _playerInfoWorker.StopScanning();
                _playerInfoWorker.Dispose();
            }
        }
    }
}
