﻿using HugsLib.Settings;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class GameOptions
    {
        private readonly ModSettingsPack _settingsPack;

        public GameOptions(ModSettingsPack settingsPack, RimWorldEventHandler eventHandler)
        {
            _settingsPack = settingsPack;

            eventHandler.DefsLoaded += OnDefLoaded;
        }

        private void OnDefLoaded()
        {
            Log.Message("[PrepareLanding] GameOptions.OnDefLoaded().");

            DisableWorldData = _settingsPack.GetHandle("DisableWorldData", "PLGOPT_DisableWorldDataTitle".Translate(),
                "PLGOPT_DisableWorldDataDescription".Translate(), false);

            DisablePreciseWorldGenPercentage = _settingsPack.GetHandle("DisablePreciseWorldGenPercentage",
                "Disable Precise World Gen. %",
                "Disable Precise World Generation Percentage on the Create World parameter page.", true);

            PrepareLandingHotKey = _settingsPack.GetHandle("HotKey",
                "PrepareLanding Hotkey",
                "Hotkey for Prepare Landing Main Window",
                KeyCode.F12,
                null,
                "Hotkey_");

        }

        public SettingHandle<bool> DisableWorldData { get; private set; }

        public SettingHandle<bool> DisablePreciseWorldGenPercentage { get; private set; }

        public SettingHandle<KeyCode> PrepareLandingHotKey { get; private set; }

    }
}