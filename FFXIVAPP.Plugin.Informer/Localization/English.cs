﻿// FFXIVAPP.Plugin.Informer
// English.cs
// 
// © 2013 Ryan Wilson

#region Usings

using System.Windows;

#endregion

namespace FFXIVAPP.Plugin.Informer.Localization
{
    public abstract class English
    {
        private static readonly ResourceDictionary Dictionary = new ResourceDictionary();

        /// <summary>
        /// </summary>
        /// <returns> </returns>
        public static ResourceDictionary Context()
        {
            Dictionary.Clear();
            Dictionary.Add("sample_", "PLACEHOLDER");
            Dictionary.Add("sample_ChatLogTabHeader", "Chat");
            Dictionary.Add("sample_ClearChatLogMessage", "Clear ChatLogFD");
            Dictionary.Add("sample_ClearChatLogToolTip", "Clear Chat");
            return Dictionary;
        }
    }
}
