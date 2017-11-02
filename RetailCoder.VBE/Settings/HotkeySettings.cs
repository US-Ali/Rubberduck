﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rubberduck.Common.Hotkeys;

namespace Rubberduck.Settings
{
    public interface IHotkeySettings
    {
        HotkeySetting[] Settings { get; set; }
    }

    public class HotkeySettings : IHotkeySettings, IEquatable<HotkeySettings>
    {
        private readonly IEnumerable<HotkeySetting> _defaultSettings;
        private HashSet<HotkeySetting> _settings;

        public HotkeySetting[] Settings
        {
            get => _settings?.ToArray() ?? _defaultSettings.ToArray();
            set
            {
                var defaults = _defaultSettings.ToArray();

                if (value == null || value.Length == 0)
                {
                    _settings = new HashSet<HotkeySetting>(defaults);
                    return;
                }
                _settings = new HashSet<HotkeySetting>();
                var incoming = value.ToList();
                //Make sure settings are valid to keep trash out of the config file.
                var hotkeyNames = defaults.Select(h => h.Name);
                incoming.RemoveAll(h => !hotkeyNames.Contains(h.Name) || !IsValid(h));

                //Only take the first setting if multiple definitions are found.
                foreach (var setting in incoming.GroupBy(s => s.Name).Select(hotkey => hotkey.First()))
                {
                    //Only allow one hotkey to be enabled with the same key combination.
                    setting.IsEnabled &= !IsDuplicate(setting);
                    _settings.Add(setting);
                }

                //Merge any hotkeys that weren't found in the input.
                foreach (var setting in defaults.Where(setting => _settings.FirstOrDefault(s => s.Name.Equals(setting.Name)) == null))
                {
                    setting.IsEnabled &= !IsDuplicate(setting);
                    _settings.Add(setting);
                }
            }
        }

        public HotkeySettings()
        {
        }

        public HotkeySettings(IEnumerable<HotkeySetting> defaultSettings)
        {
            _defaultSettings = defaultSettings;
        }

        private bool IsDuplicate(HotkeySetting candidate)
        {
            return _settings.FirstOrDefault(
                s =>
                    s.Key1 == candidate.Key1 &&
                    s.Key2 == candidate.Key2 &&
                    s.HasAltModifier == candidate.HasAltModifier &&
                    s.HasCtrlModifier == candidate.HasCtrlModifier &&
                    s.HasShiftModifier == candidate.HasShiftModifier) != null;
        }

        public bool Equals(HotkeySettings other)
        {
            return other != null && Settings.SequenceEqual(other.Settings);
        }

        private static bool IsValid(HotkeySetting candidate)
        {
            //This feels a bit sleazy...
            try
            {
                // ReSharper disable once UnusedVariable
                var test = new Hotkey(new IntPtr(), candidate.ToString(), null);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
