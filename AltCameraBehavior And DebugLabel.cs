using System;
using System.Collections.Generic;
using System.Reflection;
using Dungeonator;
using ETGGUI;
using MonoMod.RuntimeDetour;
using SGUI;
using UnityEngine;

namespace StaticCam
{
    internal class DebugLabel : SLabel
    {
        public void Start()
        {
            SGUIRoot.Main.Children.Add(this);
            this.Size = new Vector2(1000f, 40f);
            this.Alignment = TextAnchor.MiddleRight;
            this.OnUpdateStyle = delegate (SElement elem)
            {
                this.LoadFont();
                elem.Font = DebugLabel.font;
                try
                {
                    elem.Position = new Vector2((float)Screen.width * this.pos.x, (float)Screen.height * this.pos.y);
                }
                catch
                {
                    ETGModConsole.Log("Couldn't set position", false);
                }
                elem.Size = new Vector2(1000f, 40f);
            };
            this.SetText("--");
        }

        public void SetText(string text)
        {
            this.Text = text;
        }

        public void ShowLabel()
        {
            this.isEnabled = true;
        }

        public void LoadFont()
        {
            DebugLabel.gameFont = (dfFont)GameUIRoot.Instance.Manager.DefaultFont;
            DebugLabel.font = FontConverter.GetFontFromdfFont(DebugLabel.gameFont, 2);
        }

        private float map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public void SetPosition(float x, float y)
        {
            this.pos = new Vector2(x, y);
            this.Position = new Vector2((float)Screen.width * this.pos.x, (float)Screen.height * this.pos.y);
        }

        public bool isEnabled = true;
        public static Font font;
        public static dfFont gameFont;
        private Vector2 pos;
    }

    public class AltCameraBehaviour : MonoBehaviour
    {
        private void Start()
        {
            AltCameraBehaviour.Instance = this;
            this.handledRooms = new List<RoomHandler>();
            GameManager.Instance.OnNewLevelFullyLoaded += delegate ()
            {
                this.handledRooms.Clear();
                this.camControl = Camera.main.GetComponent<CameraController>();
            };
            try
            {
                Hook hook = new Hook(typeof(PlayerController).GetMethod("EnteredNewRoom", BindingFlags.Instance | BindingFlags.NonPublic), typeof(AltCameraBehaviour).GetMethod("EnteredNewRoom"));
            }
            catch (Exception ex)
            {
                ETGModConsole.Log(ex.Message, false);
            }
            ETGModConsole.Commands.AddUnit("roomsize", new Action<string[]>(this.RoomSize));
        }

        private void HandleBossFreeze()
        {
            GameManager instance = GameManager.Instance;
            RoomHandler roomHandler;
            if (instance == null)
            {
                roomHandler = null;
            }
            else
            {
                PlayerController primaryPlayer = instance.PrimaryPlayer;
                roomHandler = ((primaryPlayer != null) ? primaryPlayer.CurrentRoom : null);
            }
            RoomHandler roomHandler2 = roomHandler;
            bool flag = roomHandler2 == null;
            if (!flag)
            {
                bool flag2 = roomHandler2.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS && !roomHandler2.HasActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear);
                if (flag2)
                {
                    for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
                    {
                        PlayerController playerController = GameManager.Instance.AllPlayers[i];
                        playerController.ClearInputOverride("bossKillCam");
                    }
                }
            }
        }

        private void DoDebugLabels()
        {
            this.activeLabel.SetText("Active: " + AltCameraBehaviour.isActive.ToString());
            this.lockLabel.SetText("Locked: " + this.locked.ToString());
            this.inputLabel.SetText("Input:" + GameManager.Instance.PrimaryPlayer.AcceptingAnyInput.ToString());
            this.manualControlLabel.SetText("Manual: " + Camera.main.GetComponent<CameraController>().ManualControl.ToString());
        }

        private void Update()
        {
            this.HandleBossFreeze();
            if (this.player == null)
            {
                this.player = GameManager.Instance.PrimaryPlayer;
            }
            else
            {
                if (this.camControl == null)
                {
                    Camera main = Camera.main;
                    this.camControl = ((main != null) ? main.GetComponent<CameraController>() : null);
                }
                else
                {
                    if (!AltCameraBehaviour.isActive)
                    {
                        GameManager instance = GameManager.Instance;
                        if (((instance != null) ? instance.GetLastLoadedLevelDefinition() : null) != null && GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName == "tt_foyer")
                        {
                            this.camControl.SetManualControl(false, true);
                        }
                        else
                        {
                            if (!this.camControl.ManualControl && this.player.AcceptingAnyInput && AltCameraBehaviour.room.HasActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear))
                            {
                                this.Lock();
                            }
                        }
                    }
                }
            }
        }

        public void SetActive(bool isActive)
        {
            AltCameraBehaviour.isActive = isActive;
            if (!isActive)
            {
                if (this.camControl != null)
                {
                    this.camControl.OverrideZoomScale = 1f;
                    this.zoom = 1f;
                    if (this.player != null && this.player.AcceptingAnyInput)
                    {
                        this.camControl.SetManualControl(false, true);
                    }
                }
            }
            else
            {
                this.HandleNewRoom(this.player.CurrentRoom, this.player.CurrentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear));
            }
        }

        private void HandleNewRoom(RoomHandler room, bool shouldLock)
        {
            AltCameraBehaviour.room = room;
            bool flag = !this.handledRooms.Contains(room);
            if (flag)
            {
                room.OnEnemiesCleared = (Action)Delegate.Combine(room.OnEnemiesCleared, new Action(delegate ()
                {
                    this.Unlock();
                }));
                this.handledRooms.Add(room);
            }
            if (shouldLock)
            {
                this.Lock();
            }
        }

        private void InitDebugLabels()
        {
            this.activeLabel = new DebugLabel();
            this.activeLabel.SetPosition(0.8f, 0.5f);
            this.lockLabel = new DebugLabel();
            this.lockLabel.SetPosition(0.8f, 0.53f);
            this.inputLabel = new DebugLabel();
            this.inputLabel.SetPosition(0.8f, 0.59f);
            this.manualControlLabel = new DebugLabel();
            this.manualControlLabel.SetPosition(0.8f, 0.56f);
        }

        private void Unlock()
        {
            bool flag = !AltCameraBehaviour.isActive;
            if (!flag)
            {
                this.camControl.OverrideZoomScale = 1f;
                this.camControl.SetManualControl(false, true);
                this.locked = false;
            }
        }

        private void Lock()
        {
            bool flag = !AltCameraBehaviour.isActive;
            if (!flag)
            {
                IntVector2 dimensions = AltCameraBehaviour.room.area.dimensions;
                float num = 27f;
                float num2 = ((float)dimensions.y >= num) ? (Mathf.Sqrt((float)dimensions.y - num) / 12f) : 0f;
                float num3 = 0.6f - num2;
                num3 = Mathf.Max(num3, 0.4f);
                bool flag2 = AltCameraBehaviour.room.AdditionalRoomState == RoomHandler.CustomRoomState.LICH_PHASE_THREE;
                if (flag2)
                {
                    num3 = 0.6f;
                }
                bool flag3 = true;
                bool flag4 = dimensions.y >= 40;
                if (flag4)
                {
                    flag3 = false;
                    num3 = 0.6f;
                }
                this.camControl.OverrideZoomScale = num3;
                this.zoom = num3;
                Vector3 position = this.camControl.Camera.transform.position;
                bool flag5 = flag3;
                if (flag5)
                {
                    this.camControl.OverridePosition = new Vector3(AltCameraBehaviour.room.area.Center.x, AltCameraBehaviour.room.area.Center.y + 1f, position.z);
                    this.camControl.SetManualControl(true, true);
                    this.locked = true;
                    Vector3 overridePosition = this.camControl.OverridePosition;
                    bool flag6 = float.IsNaN(this.camControl.OverridePosition.x) || float.IsNaN(this.camControl.OverridePosition.y);
                    if (flag6)
                    {
                        ETGModConsole.Log("<color=#FF0000>NaNs!</color>", false);
                        this.Unlock();
                    }
                }
            }
        }

        public static void EnteredNewRoom(Action<PlayerController, RoomHandler> orig, PlayerController self, RoomHandler newRoom)
        {
            orig(self, newRoom);
            AltCameraBehaviour.Instance.HandleNewRoom(newRoom, newRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear));
        }

        public void RoomSize(string[] args)
        {
            IntVector2 dimensions = AltCameraBehaviour.room.area.dimensions;
            ETGModConsole.Log(string.Format("Room size: {0}, {1}, last zoom: {2}", dimensions.x, dimensions.y, this.zoom), false);
        }

        private static RoomHandler room;

        private CameraController camControl;

        private PlayerController player;

        private static AltCameraBehaviour Instance;

        private float zoom;

        public static bool isActive = false;

        private List<RoomHandler> handledRooms;

        private DebugLabel activeLabel;

        private DebugLabel lockLabel;

        private DebugLabel manualControlLabel;

        private DebugLabel inputLabel;

        private bool locked;
    }
}
