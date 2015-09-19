﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CM3D2.MaidFiddler.Hook;
using CM3D2.MaidFiddler.Plugin.Gui;
using CM3D2.MaidFiddler.Plugin.Utils;
using ExIni;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;
using Application = System.Windows.Forms.Application;

namespace CM3D2.MaidFiddler.Plugin
{
    [PluginName("Maid Fiddler"), PluginVersion("BETA 0.3")]
    public class MaidFiddler : PluginBase, IDisposable
    {
        private static readonly KeyCode[] DEFAULT_KEY_CODE = {KeyCode.N};
        private MaidFiddlerGUI gui;
        private Thread guiThread;
        private KeyHelper keyCreateGUI;
        public static string DATA_PATH { get; private set; }

        public void Dispose()
        {
            gui?.Dispose();
        }

        public void Awake()
        {
            DATA_PATH = DataPath;
            LoadConfig();

            guiThread = new Thread(LoadGUI);

            FiddlerHooks.SaveLoadedEvent += OnSaveLoaded;
            Debugger.WriteLine("MaidFiddler loaded!");
        }

        private static string GetKeyCombo(IList<KeyCode> keys)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append(EnumHelper.GetName(keys[i]));
                if (i != keys.Count - 1)
                    sb.Append('+');
            }

            return sb.ToString();
        }

        public void LateUpdate()
        {
            gui?.UpdateSelectedMaidValues();
            gui?.UpdatePlayerValues();
        }

        private void LoadConfig()
        {
            Debugger.WriteLine(LogLevel.Info, "Loading launching key combination...");
            List<KeyCode> keys = new List<KeyCode>();
            IniKey value = Preferences["Keys"]["StartGUIKey"];
            if (value.Value == null || value.Value.Trim() == string.Empty)
            {
                value.Value = GetKeyCombo(DEFAULT_KEY_CODE);
                keys.AddRange(DEFAULT_KEY_CODE);
                SaveConfig();
            }
            else
            {
                try
                {
                    string[] keyCodes = value.Value.Split(new[] {'+'}, StringSplitOptions.RemoveEmptyEntries);
                    if (keyCodes.Length == 0)
                        throw new Exception();
                    foreach (KeyCode kc in
                    keyCodes.Select(keyCode => (KeyCode) Enum.Parse(typeof (KeyCode), keyCode.Trim(), true))
                            .Where(kc => !keys.Contains(kc)))
                        keys.Add(kc);
                    if (keyCodes.Length != keys.Count)
                    {
                        value.Value = GetKeyCombo(keys);
                        SaveConfig();
                    }
                }
                catch (Exception)
                {
                    Debugger.WriteLine(LogLevel.Error, "Failed to parse given key combo. Using default combination");
                    value.Value = GetKeyCombo(DEFAULT_KEY_CODE);
                    keys.AddRange(DEFAULT_KEY_CODE);
                    SaveConfig();
                }
            }
            keyCreateGUI = new KeyHelper(keys.ToArray());
            Debugger.WriteLine(LogLevel.Info, $"Loaded {keys.Count} long key combo: {GetKeyCombo(keys)}");
        }

        public void LoadGUI()
        {
            try
            {
                if (gui == null)
                    gui = new MaidFiddlerGUI();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(gui);
            }
            catch (Exception e)
            {
                ErrorLog.ThrowErrorMessage(e);
            }
        }

        public void OnDestroy()
        {
            if (gui == null)
                return;
            Debugger.WriteLine("Closing GUI...");
            gui.Close(true);
        }

        public void OnSaveLoaded(int saveNo)
        {
            Debugger.WriteLine(LogLevel.Info, $"Level loading! Save no. {saveNo}");
            gui?.ReloadMaids();
            gui?.ReloadPlayer();
        }

        public void OpenGUI()
        {
            if (guiThread.ThreadState != ThreadState.Running)
                guiThread.Start();
            else
                gui?.Show();
        }

        public void Update()
        {
            keyCreateGUI.Update();

            if (keyCreateGUI.HasBeenPressed())
                OpenGUI();
            gui?.UpdateMaids();
        }
    }
}