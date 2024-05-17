using System;
using UnityEngine;

namespace ManualGimbalControl
{
    public class ModuleGimbalTrim : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool trimActive = false;

        // A comfortable incrementalSpeed was found with trial-and-error
        [KSPAxisField(isPersistant = true, incrementalSpeed = 6f, guiActive = false, guiActiveEditor = false, guiName = "#GimbalTrim_GimbalXField", groupName = "gimbalTrim", groupDisplayName = "#GimbalTrim_CategoryName", groupStartCollapsed = true)]
        [UI_FloatRange(minValue = -15f, stepIncrement = 0.25f, maxValue = 15f, requireFullControl = true)]
        public float gimbalAngleX = 0f;

        [KSPAxisField(isPersistant = true, incrementalSpeed = 6f, guiActive = false, guiActiveEditor = false, guiName = "#GimbalTrim_GimbalYField", groupName = "gimbalTrim", groupDisplayName = "#GimbalTrim_CategoryName", groupStartCollapsed = true)]
        [UI_FloatRange(minValue = -15f, stepIncrement = 0.25f, maxValue = 15f, requireFullControl = true)]
        public float gimbalAngleY = 0f;

        private ModuleGimbal moduleGimbalInstance;
        private List<Quaternion> defaultGimbalRotations = new List<Quaternion>();

        // Runtime values
        private int i = 0;
        private float gimbalRange;
        KSPActionParam emptyParam = new KSPActionParam(KSPActionGroup.None, KSPActionType.Toggle); // Some methods require (i think?) this argument even though this mod doesn't make use of it

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#GimbalTrim_ToggleTrim", groupName = "gimbalTrim", groupDisplayName = "#GimbalTrim_CategoryName", groupStartCollapsed = true)]
        public void ToggleTrimButton()
        {
            ToggleTrim(emptyParam);
        }

        public override void OnStart(StartState state)
        {
            moduleGimbalInstance = (ModuleGimbal) part.Modules.GetModule("ModuleGimbal");

            foreach (Transform gimbalTransform in moduleGimbalInstance.gimbalTransforms)
            {
                // Store the default rotations of all the nozzles to then change
                defaultGimbalRotations.Add(gimbalTransform.localRotation);
            }

            // Limit the maximum settable gimbal to its proper value
            gimbalRange = moduleGimbalInstance.gimbalRange;
            ((BaseAxisField)Fields["gimbalAngleX"]).maxValue = ((BaseAxisField)Fields["gimbalAngleY"]).maxValue = gimbalRange;
            ((BaseAxisField)Fields["gimbalAngleX"]).minValue = ((BaseAxisField)Fields["gimbalAngleY"]).minValue = - gimbalRange;

            // For some reason the variable is separate for the editor and flight scenes, hence the ugliness
            if (state == StartState.Editor)
            {
                ((UI_FloatRange)Fields["gimbalAngleX"].uiControlEditor).maxValue = ((UI_FloatRange)Fields["gimbalAngleY"].uiControlEditor).maxValue = gimbalRange;

                ((UI_FloatRange)Fields["gimbalAngleX"].uiControlEditor).minValue = ((UI_FloatRange)Fields["gimbalAngleY"].uiControlEditor).minValue = - gimbalRange;
            }
            else
            {
                ((UI_FloatRange)Fields["gimbalAngleX"].uiControlFlight).maxValue = ((UI_FloatRange)Fields["gimbalAngleY"].uiControlFlight).maxValue = gimbalRange;

                ((UI_FloatRange)Fields["gimbalAngleX"].uiControlFlight).minValue = ((UI_FloatRange)Fields["gimbalAngleY"].uiControlFlight).minValue = -gimbalRange;
            }

            // Force disable gimbal trim because the game doesn't do it itself in the editor
            if (!trimActive)
            {
                DisableTrim(emptyParam);
            }
            // Force enable gimbal trim because the game doesn't do it itself in the flight scene.....
            else
            {
                EnableTrim(emptyParam);
            }
        }

        public void FixedUpdate()
        {
            if (trimActive)
            {
                SetEngineGimbal(gimbalAngleX, gimbalAngleY);
            }
        }

        // Giving this the KSPAction attribute and separating the methods is less elegant, but makes this module usable in action groups
        [KSPAction("Toggle Gimbal Trim")]
        public void ToggleTrim(KSPActionParam param)
        {
            if (trimActive)
            {
                DisableTrim(param);
            }
            else
            {
                EnableTrim(param);
            }
        }

        [KSPAction("Enable Gimbal Trim")]
        public void EnableTrim(KSPActionParam param)
        {
            trimActive = true;
            // Show sliders
            Fields["gimbalAngleX"].guiActive = Fields["gimbalAngleX"].guiActiveEditor = trimActive;
            Fields["gimbalAngleY"].guiActive = Fields["gimbalAngleY"].guiActiveEditor = trimActive;
        }

        [KSPAction("Disable Gimbal Trim")]
        public void DisableTrim(KSPActionParam param)
        {
            trimActive = false;
            // Hide sliders
            Fields["gimbalAngleX"].guiActive = Fields["gimbalAngleX"].guiActiveEditor = trimActive;
            Fields["gimbalAngleY"].guiActive = Fields["gimbalAngleY"].guiActiveEditor = trimActive;
            // Reset engine position
            SetEngineGimbal(0, 0); 
        }

        private void SetEngineGimbal(float X, float Y)
        {
            i = 0;
            // Some engines have multiple nozzles and therefore multiple gimbals, we need to update all of them
            for (i = 0; i < defaultGimbalRotations.Count(); i++)
            {
                moduleGimbalInstance.gimbalTransforms[i].localRotation = defaultGimbalRotations[i] * Quaternion.AngleAxis(X, Vector3.right) * Quaternion.AngleAxis(Y, Vector3.up);
            }
        }
    }
}