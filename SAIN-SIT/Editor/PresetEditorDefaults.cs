﻿using SAIN.Attributes;
using SAIN.Plugin;

namespace SAIN.Editor
{
    public class PresetEditorDefaults
    {
        public PresetEditorDefaults()
        {
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        public PresetEditorDefaults(string selectedPreset)
        {
            SelectedPreset = selectedPreset;
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        [Hidden]
        public string SelectedPreset;
        [Hidden]
        public string DefaultPreset;

        [Name("Advanced Bot Configs")]
        [Default(false)]
        public bool AdvancedBotConfigs;

        [Name("Debug Mode")]
        [Default(false)]
        public bool GlobalDebugMode;

        [Name("Draw Debug Gizmos")]
        [Default(false)]
        public bool DrawDebugGizmos;

        [Name("GUI Size Scaling")]
        [Default(1f)]
        [MinMax(1f, 2f, 100f)]
        public float ConfigScaling = 1f;

        [Name("Collect and Export Bot Layer and Brain Info")]
        [Default(false)]
        public bool CollectBotLayerBrainInfo = false;

        [Name("Draw Debug Suppression Points")]
        [Default(false)]
        public bool DebugDrawProjectionPoints = false;

        [Name("Path Safety Tester")]
        [Default(false)]
        public bool DebugEnablePathTester = false;

        [Name("Draw Debug Path Safety Tester")]
        [Default(false)]
        public bool DebugDrawSafePaths = false;

    }
}