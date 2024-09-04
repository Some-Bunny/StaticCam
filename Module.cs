using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StaticCam
{ 
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class StaticCamMod : BaseUnityPlugin
    {
        public const string GUID = "kylethescientist.etg.staticcam";
        public const string NAME = "Static Cam : BepinEx Edition!";
        public const string VERSION = "1.0.1";
        public const string TEXT_COLOR = "#57ef57";

        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }

        public void GMStart(GameManager g)
        {
            StaticCamMod.altCam = ETGModMainBehaviour.Instance.gameObject.AddComponent<AltCameraBehaviour>();
            ETGModConsole.Commands.AddUnit("staticcam", delegate (string[] e)
            {
                bool flag = !AltCameraBehaviour.isActive;
                StaticCamMod.altCam.SetActive(flag);
                string str = flag ? "<color=#00FF00FF>" : "<color=#FF0000FF>";
                string str2 = flag ? "enabled" : "disabled";
                ETGModConsole.Log("Static camera " + str + str2 + "</color>", false);
            });
            ETGModConsole.Log("    Type <color=#00FF00FF>staticcam</color> to toggle.", false);
            Log($"{NAME} v{VERSION} started successfully.", TEXT_COLOR);
        }
        private static AltCameraBehaviour altCam;



        public static void Log(string text, string color="#FFFFFF")
        {
            ETGModConsole.Log($"<color={color}>{text}</color>");
        }
    }
}
