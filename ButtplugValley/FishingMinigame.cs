using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ButtplugValley
{
    public enum FishingMinigameType
    {
        CatchLevel,
        DistanceToCenterCatchBar
    }

    internal class FishingMinigame
    {
        private IModHelper helper;
        private IReflectionHelper reflectionHelper;
        private ModConfig modConfig;
        public float previousVibrationLevel;
        private BPManager _bpManager;
        private IMonitor monitor;
        public bool isActive = true;

        private bool lastInBarStatus = true;
        
        
        public float maxVibration = 100f; // Adjust as desired

        public FishingMinigame(IModHelper modHelper, IMonitor MeMonitor, BPManager MEbpManager, ModConfig ModConfig)
        {
            helper = modHelper;
            monitor = MeMonitor;
            modConfig = ModConfig;
            _bpManager = MEbpManager;
            reflectionHelper = helper.Reflection;
            previousVibrationLevel = 0f;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            FishingCheck();
        }

        private void FishingCheck()
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.activeClickableMenu is StardewValley.Menus.BobberBar menu)
            {
                maxVibration = modConfig.MaxFishingVibration;
                monitor.Log("FishingMinigameIsActive", LogLevel.Debug);

                switch(modConfig.FishingMinigameSetting)
                {
                    case FishingMinigameType.CatchLevel:
                        // Get the distanceFromCatching field using reflection
                        IReflectedField<float> distanceFromCatchingField = this.reflectionHelper.GetField<float>(menu, "distanceFromCatching");

                        if (distanceFromCatchingField == null)
                        {
                            monitor.Log("distanceFromCatching field not found", LogLevel.Debug);
                            return;
                        }

                        float captureLevel = distanceFromCatchingField.GetValue();
                        monitor.Log($"distancefrom {captureLevel}", LogLevel.Debug);

                        // Scale the capture level based on the maximum vibration value
                        float scaledCaptureLevel = captureLevel * maxVibration;

                        // Ensure the scaled capture level does not exceed the maximum vibration value
                        float capturePercentage = Math.Min(scaledCaptureLevel, maxVibration);

                        // Vibrate the device based on the capture percentage if it has changed
                        if (capturePercentage != previousVibrationLevel)
                        {
                            monitor.Log($"FISHINGMINIGAME {capturePercentage}", LogLevel.Debug);
                            _ = _bpManager.VibrateDevice(capturePercentage);
                            previousVibrationLevel = capturePercentage;
                        }
                        break;

                    case FishingMinigameType.DistanceToCenterCatchBar:
                        IReflectedField<bool> bobberInBarField = this.reflectionHelper.GetField<bool>(menu, "bobberInBar");
                        bool inBar = bobberInBarField.GetValue();
                        if (!inBar)
                        {
                            _ = _bpManager.VibrateDevice(maxVibration * 0.1f);
                            break;
                        }

                        IReflectedField<float> bobberBarPosField = this.reflectionHelper.GetField<float>(menu, "bobberBarPos");
                        IReflectedField<int> bobberBarHeightField = this.reflectionHelper.GetField<int>(menu, "bobberBarHeight");
                        IReflectedField<float> bobberPositionField = this.reflectionHelper.GetField<float>(menu, "bobberPosition");

                        float bobberBarPos = bobberBarPosField.GetValue();
                        float bobberPos = bobberPositionField.GetValue();
                        int barHeight = bobberBarHeightField.GetValue();

                        float middleOfBobber = (bobberPos - 2f);

                        float halfBar = (float)barHeight / 2;
                        float middleOfBar = bobberBarPos - 32f + halfBar;

                        float distanceBetween = Math.Abs(middleOfBar - middleOfBobber);

                        float calculatedVibrationValue = ((-.5f / halfBar) * distanceBetween) + 1;

                        // Scale the capture level based on the maximum vibration value
                        float scaledVibrationValue = calculatedVibrationValue * maxVibration;

                        // Ensure the scaled capture level does not exceed the maximum vibration value
                        float vibrationPercentage = Math.Min(scaledVibrationValue, maxVibration);

                        // Vibrate the device based on the capture percentage if it has changed
                        if (vibrationPercentage != previousVibrationLevel)
                        {
                            monitor.Log($"FISHINGMINIGAME {vibrationPercentage}", LogLevel.Debug);
                            _ = _bpManager.VibrateDevice(vibrationPercentage);
                            previousVibrationLevel = vibrationPercentage;
                        }

                        break;
                }
            }
            else
            {
                // The bobber bar menu is no longer active, stop vibrating the device
                if (previousVibrationLevel > 0)
                {
                    monitor.Log("Stopping device vibration", LogLevel.Debug);
                    _bpManager.VibrateDevice(0);
                    previousVibrationLevel = 0;
                }
            }
        }



        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // Reset previous capture level when a new day starts
            previousVibrationLevel = 0f;
        }
        
    }
}