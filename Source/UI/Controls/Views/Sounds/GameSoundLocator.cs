﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sanguosha.UI.Controls
{
    public class GameSoundLocator
    {
        private static Dictionary<string, int> _soundsCount = new Dictionary<string, int>();
        private static Dictionary<string, string> _soundsPath = new Dictionary<string, string>();
        private static Dictionary<string, int> _soundsRotation = new Dictionary<string, int>();
        private static Random _soundPicker = new Random(DateTime.Now.Millisecond);

        private static void _ParseDirectory(string key, string path)
        {
            try
            {
                var files = Directory.GetFiles(path);
                foreach (var filePath in files)
                {
                    string file = Path.GetFileName(filePath).ToLower();
                    var parts = file.Split('.');
                    if (parts.Length == 0 || parts.Last() != "mp3") continue;
                    string totalKey = string.Format("{0}.{1}", key, parts[0]).ToLower();
                    if (!_soundsCount.ContainsKey(totalKey))
                    {
                        _soundsCount.Add(totalKey, 1);
                        _soundsRotation.Add(totalKey, 0);
                        _soundsPath.Add(totalKey, path + "/" + parts[0]);
                    }
                    else
                    {
                        int last = _soundsCount[totalKey]++;

                        // Pick the first sound effect on a random basis.
                        if (_soundPicker.Next(last + 1) == 0)
                        {
                            _soundsRotation[totalKey] = last;
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _ParseDirectory("BGM", "./Resources/Sounds/BGM");
            _ParseDirectory("System", "./Resources/Sounds/System");
            _ParseDirectory("Death", "./Resources/Sounds/Death");
            _ParseDirectory("Cards.Male", "./Resources/Sounds/Cards/Male");
            _ParseDirectory("Cards.Female", "./Resources/Sounds/Cards/Female");
            _ParseDirectory("Cards.Common", "./Resources/Sounds/Cards/Common");
            _ParseDirectory("Skills", "./Resources/Sounds/Skills");            
        }

        public static string _RemoveTrailingDigits(string s)
        {
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(s[i])) return s.Substring(0, i + 1);
            }
            return string.Empty;
        }

        public static Uri GetCardSound(string cardName, bool? isMale)
        {
            Uri uri = null;
            cardName = _RemoveTrailingDigits(cardName);
            if (isMale == true) uri = GetUriFromKey("Cards.Male." + cardName);
            else if (isMale == false) uri = GetUriFromKey("Cards.Female." + cardName);
            if (uri == null) uri = GetUriFromKey("Cards.Common." + cardName);
            return uri;
        }
        
        public static Uri GetSkillSound(string skillName, int skillTag = 0)
        {
            skillName = _RemoveTrailingDigits(skillName);
            string appendix = skillTag == 0 ? string.Empty : "_" + skillTag;
            return GetUriFromKey("Skills." + skillName + appendix);            
        }

        public static Uri GetSystemSound(string key)
        {
            return GetUriFromKey(string.Format("System.{0}", key));
        }

        public static Uri GetDeathSound(string key)
        {
            return GetUriFromKey(string.Format("Death.{0}", key));
        }

        public static Uri GetBgm()
        {
            return GetUriFromKey("BGM.default");
        }

        private static Uri GetUriFromKey(string key)
        {
            key = key.ToLower();
            if (!_soundsCount.ContainsKey(key)) return null;
            int usable = _soundsCount[key];           
            int i = _soundsRotation[key];
            
            string filePath;
            if (i == 0)
                filePath = string.Format("{0}.mp3", _soundsPath[key], _soundsRotation[key]);
            else
                filePath = string.Format("{0}.{1}.mp3", _soundsPath[key], _soundsRotation[key]);
            if (!File.Exists(filePath)) return null;
            
            _soundsRotation[key]++;
            if (_soundsRotation[key] >= usable)
            {
                _soundsRotation[key] = 0;
            }

            return GetUriFromPath(filePath);
        }

        private static Uri GetUriFromPath(string path)
        {
            path = path.TrimStart('.', '/');
            Uri uri = new Uri(string.Format("pack://siteoforigin:,,,/{0}", path));
            return uri;
        }
    }
}
