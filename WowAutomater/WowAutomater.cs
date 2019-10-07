﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace ClassicWowNeuralParasite
{
    public enum ActionMode
    {
        AutoAttack,
        AutoWalk,
        FindTarget,
        KillTarget,
        LootTarget,
        RegenerateVitals,
        Revive,
        SellItems,
        ReadyToStart,
        WaitingForWow,
        WaitingForAddon
    }

    public class AutomaterActionEventArgs : EventArgs
    {
        public ActionMode CurrentAction;

        public AutomaterActionEventArgs(ActionMode currentAction)
        {
            CurrentAction = currentAction;
        }
    }

    public static class WowAutomater
    {
        public static ActionMode CurrentActionMode
        {
            set
            {
                m_ResetCoordinates = true;
                m_CurrentActionMode = value;
                m_SetActionMode = value;
            }
            get
            {
                return m_CurrentActionMode;
            }
        }

        private static ActionMode m_SetActionMode = ActionMode.FindTarget;
        private static volatile ActionMode m_CurrentActionMode = ActionMode.WaitingForAddon;

        public delegate void AutomaterActionEventHandler(object sender, AutomaterActionEventArgs wea);
        public static event AutomaterActionEventHandler AutomaterStatusEvent;

        private static List<double> m_PathXCoordinates = new List<double>();
        private static List<double> m_PathYCoordinates = new List<double>();
        private static List<double> m_ReviveXCoordinates = new List<double>();
        private static List<double> m_ReviveYCoordinates = new List<double>();
        private static List<double> m_ShopXCoordinates = new List<double>();
        private static List<double> m_ShopYCoordinates = new List<double>();
        private static List<double> m_WalkXCoordinates = new List<double>();
        private static List<double> m_WalkYCoordinates = new List<double>();

        private static EventWaitHandle m_ActionEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private static volatile bool m_Run = true;

        private static bool m_NoDead = false;
        private static bool m_NoShop = false;
        private static bool m_NoWalk = false;
        private static bool m_ResetCoordinates = true;

        private static PlayerClassType m_CurrentClass = PlayerClassType.None;

        public static double RegisterDelay = 0.1;
        public static double XReviveButtonLocation = 32500;
        public static double YReviveButtonLocation = 14000;
        public static volatile bool SkinLoot = false;

        public static double RegenerateVitalsHealthPercentage = 60;

        private static volatile bool m_WalkBackwards = false;
        private static volatile bool m_Turn = true;
        private static bool m_TurnDirection = true;

        private static bool m_Ghosted = false;
        private static bool m_Potion = false;
        private static bool m_Idle = false;
        private static bool m_StartedEating = false;
        private static bool m_FarTarget = false;

        private static Stopwatch m_ReviveSw = new Stopwatch();

        private static WowClassAutomater m_WowClassAutomater = null;

        public static WarriorAutomater Warrior = new WarriorAutomater();
        public static PaladinAutomater Paladin = new PaladinAutomater();
        public static RogueAutomater   Rogue =   new RogueAutomater();
        public static PriestAutomater  Priest =  new PriestAutomater();
        public static MageAutomater    Mage =    new MageAutomater();
        public static WarlockAutomater Warlock = new WarlockAutomater();
        public static HunterAutomater  Hunter =  new HunterAutomater();
        public static ShamanAutomater  Shaman =  new ShamanAutomater();
        public static DruidAutomater   Druid =   new DruidAutomater();

        private static void WoWAPIUpdateEvent(object sender, EventArgs wea)
        {
            if (WowApi.CurrentPlayerData.PlayerActionError == ActionErrorType.BehindTarget || 
                WowApi.CurrentPlayerData.PlayerActionError == ActionErrorType.FacingWrongWay)
            {
                m_WalkBackwards = true;
                m_Turn = true;
            }
                
            CheckClass();
        }

        private static void CheckClass()
        {
            if(WowApi.CurrentPlayerData.Found && m_CurrentClass != WowApi.CurrentPlayerData.Class)
            {
                m_CurrentClass = WowApi.CurrentPlayerData.Class;
                switch (WowApi.CurrentPlayerData.Class)
                {
                    case PlayerClassType.Warrior:
                        m_WowClassAutomater = Warrior;
                        break;
                    case PlayerClassType.Paladin:
                        m_WowClassAutomater = Paladin;
                        break;
                    case PlayerClassType.Rogue:
                        m_WowClassAutomater = Rogue;
                        break;
                    case PlayerClassType.Priest:
                        m_WowClassAutomater = Priest;
                        break;
                    case PlayerClassType.Mage:
                        m_WowClassAutomater = Mage;
                        break;
                    case PlayerClassType.Warlock:
                        m_WowClassAutomater = Warlock;
                        break;
                    case PlayerClassType.Hunter:
                        m_WowClassAutomater = Hunter;
                        break;
                    case PlayerClassType.Shaman:
                        m_WowClassAutomater = Shaman;
                        break;
                    case PlayerClassType.Druid:
                        m_WowClassAutomater = Druid;
                        break;
                    default:
                        break;
                }
            }
        }

        public static void SetPathCoordinates(List<double> xCoordinates, List<double> yCoordinates)
        {
            if (xCoordinates.Count != yCoordinates.Count)
                throw new Exception("The number of x and y coordinates must match.");

            m_ResetCoordinates = true;

            m_PathXCoordinates = new List<double>(xCoordinates);
            m_PathYCoordinates = new List<double>(yCoordinates);
        }

        public static void SetReviveCoordinates(List<double> xCoordinates, List<double> yCoordinates)
        {
            if (xCoordinates.Count != yCoordinates.Count)
                throw new Exception("The number of x and y coordinates must match.");

            m_ResetCoordinates = true;

            m_ReviveXCoordinates = new List<double>(xCoordinates);
            m_ReviveYCoordinates = new List<double>(yCoordinates);

            if (m_ReviveXCoordinates.Count == 0)
                m_NoDead = true;
        }

        public static void SetShopCoordinates(List<double> xCoordinates, List<double> yCoordinates)
        {
            if (xCoordinates.Count != yCoordinates.Count)
                throw new Exception("The number of x and y coordinates must match.");

            m_ResetCoordinates = true;

            m_ShopXCoordinates = new List<double>(xCoordinates);
            m_ShopYCoordinates = new List<double>(yCoordinates);

            if (m_ShopXCoordinates.Count == 0)
                m_NoShop = true;
        }

        public static void SetWalkCoordinates(List<double> xCoordinates, List<double> yCoordinates)
        {
            if (xCoordinates.Count != yCoordinates.Count)
                throw new Exception("The number of x and y coordinates must match.");

            m_ResetCoordinates = true;

            m_WalkXCoordinates = new List<double>(xCoordinates);
            m_WalkYCoordinates = new List<double>(yCoordinates);

            if (m_WalkXCoordinates.Count == 0)
                m_NoWalk = true;
        }

        public static void Stop()
        {
            m_Run = false;
            m_ActionEventWaitHandle.WaitOne();
            WowApi.UpdateEvent -= WoWAPIUpdateEvent;
        }

        private static void CheckIdle()
        {
            if (!WowApi.CurrentPlayerData.IsWowForeground)
            {
                WaypointFollower.StopFollowingWaypoints();
                m_CurrentActionMode = ActionMode.WaitingForWow;
                m_Idle = true;
            }
            else if (!WowApi.CurrentPlayerData.Found)
            {
                WaypointFollower.StopFollowingWaypoints();
                m_CurrentActionMode = ActionMode.WaitingForAddon;
                m_Idle = true;
            }
            else if (!WowApi.CurrentPlayerData.Start)
            {
                WaypointFollower.StopFollowingWaypoints();
                m_CurrentActionMode = ActionMode.ReadyToStart;
                m_Idle = true;
            }
            else if (m_Idle)
            {
                m_CurrentActionMode = m_SetActionMode;
                m_Idle = false;
            }
                
        }

        public static void Run()
        {
            WowApi.UpdateEvent += WoWAPIUpdateEvent;

            while (m_Run)
            {
                WowApi.Sync.WaitOne();

                CheckIdle();

                switch (m_CurrentActionMode)
                {
                    case ActionMode.AutoAttack:
                        AutoAttackTarget();
                        break;
                    case ActionMode.AutoWalk:
                        AutoWalk();
                        break;
                    case ActionMode.FindTarget:
                        FindTarget();
                        break;
                    case ActionMode.KillTarget:
                        KillTarget();
                        break;
                    case ActionMode.LootTarget:
                        LootTarget();
                        break;
                    case ActionMode.RegenerateVitals:
                        RegenerateVitals();
                        break;
                    case ActionMode.Revive:
                        RunFromGraveToBody();
                        break;
                    case ActionMode.SellItems:
                        SellItems();
                        break;
                    default:
                        break;
                }

                AutomaterStatusEvent?.Invoke(null, new AutomaterActionEventArgs(m_CurrentActionMode));
                m_ActionEventWaitHandle.Set();
            }
        }

        private static void AutoWalk()
        {
            if (m_NoWalk)
                return;

            if(m_ResetCoordinates)
            {
                WaypointFollower.SetWaypoints(m_WalkXCoordinates, m_WalkYCoordinates);

                m_ResetCoordinates = false;
            }

            WaypointFollower.FollowWaypoints(false);
        }

        private static void RunFromGraveToBody()
        {
            if (m_NoDead)
                return;

            if (!m_Ghosted)
            {
                List<double> ghostXCoordinates = new List<double>();
                List<double> ghostYCoordinates = new List<double>();

                ghostXCoordinates.AddRange(m_ReviveXCoordinates);
                ghostXCoordinates.AddRange(m_PathXCoordinates);

                ghostYCoordinates.AddRange(m_ReviveYCoordinates);
                ghostYCoordinates.AddRange(m_PathYCoordinates);

                WaypointFollower.SetWaypoints(ghostXCoordinates, ghostYCoordinates);

                Helper.WaitSeconds(5.0);
                Input.MoveMouseTo(XReviveButtonLocation, YReviveButtonLocation);
                Helper.WaitSeconds(0.1);
                Input.LeftClick();
                m_Ghosted = true;

                Helper.WaitSeconds(1.0);
                m_ReviveSw.Start();
            }

            WaypointFollower.FollowWaypoints(true);

            if (m_ReviveSw.ElapsedMilliseconds > 1000)
            {
                Input.LeftClick();
                m_ReviveSw.Restart();
            }

            if (WowApi.CurrentPlayerData.PlayerHealth > 1)
            {
                WaypointFollower.StopFollowingWaypoints();
                m_Ghosted = false;
                m_CurrentActionMode = ActionMode.RegenerateVitals;
                m_ReviveSw.Stop();

                m_ResetCoordinates = true;
            }
        }

        private static void SellItems()
        {
            if (m_NoShop)
                return;
        }

        private static void LootTarget()
        {
            Helper.WaitSeconds(0.250);
            Input.KeyDown(VirtualKeyCode.LSHIFT);

            Input.MoveMouseTo(33300, 30000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.MoveMouseTo(33300, 40000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.MoveMouseTo(43300, 40000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.MoveMouseTo(23300, 40000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.MoveMouseTo(33300, 50000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.MoveMouseTo(43300, 50000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.MoveMouseTo(23300, 50000);
            Input.RightClick();
            Helper.WaitSeconds(0.250);

            Input.KeyUp(VirtualKeyCode.LSHIFT);

            Helper.WaitSeconds(1.0);

            if (SkinLoot)
            {
                Helper.WaitSeconds(0.250);
                Input.KeyDown(VirtualKeyCode.LSHIFT);

                Input.MoveMouseTo(33300, 30000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);   

                Input.MoveMouseTo(33300, 40000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);

                Input.MoveMouseTo(43300, 40000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);

                Input.MoveMouseTo(23300, 40000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);

                Input.MoveMouseTo(33300, 50000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);

                Input.MoveMouseTo(43300, 50000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);

                Input.MoveMouseTo(23300, 50000);
                Input.RightClick();
                Helper.WaitSeconds(0.250);

                Helper.WaitSeconds(4.000);
                Input.KeyUp(VirtualKeyCode.LSHIFT);
            }

            if (WowApi.CurrentPlayerData.PlayerInCombat)
                m_CurrentActionMode = ActionMode.KillTarget;
            else if (WowApi.CurrentPlayerData.PlayerHealthPercentage <= RegenerateVitalsHealthPercentage)
                m_CurrentActionMode = ActionMode.RegenerateVitals;
            else
                m_CurrentActionMode = ActionMode.FindTarget;
        }

        private static void RegenerateVitals()
        {
            if (!m_StartedEating)
            {
                Helper.WaitSeconds(1.500);
                Input.KeyPress(VirtualKeyCode.VK_X);
                Helper.WaitSeconds(RegisterDelay);
                Input.KeyPress(VirtualKeyCode.VK_8);
                Helper.WaitSeconds(RegisterDelay);
                Input.KeyPress(VirtualKeyCode.VK_9);
                Helper.WaitSeconds(RegisterDelay);

                m_WowClassAutomater.RegenerateVitals();

                m_StartedEating = true;
            }
            else if (WowApi.CurrentPlayerData.PlayerInCombat)
            {
                m_StartedEating = false;
                CurrentActionMode = ActionMode.KillTarget;
            }
            else if (WowApi.CurrentPlayerData.PlayerHealth == WowApi.CurrentPlayerData.MaxPlayerHealth)
            {
                CurrentActionMode = ActionMode.FindTarget;
                m_StartedEating = false;
            }
        }

        private static void FindTarget()
        {
            if(m_ResetCoordinates)
            {
                WaypointFollower.SetWaypoints(m_PathXCoordinates, m_PathYCoordinates);

                m_ResetCoordinates = false;
            }

            if (WowApi.CurrentPlayerData.PlayerInCombat)
            {
                WaypointFollower.StopFollowingWaypoints();

                Helper.WaitSeconds(0.5);
                m_CurrentActionMode = ActionMode.KillTarget;
                return;
            }

            m_WowClassAutomater.FindTarget();
        }

        private static void AutoAttackTarget()
        {
            m_WowClassAutomater.AutoAttackTarget();
        }

        private static void KillTarget()
        {
            if (WowApi.CurrentPlayerData.IsPlayerDead)
            {
                m_Potion = false;
                m_CurrentActionMode = ActionMode.Revive;
            }
            else if (!WowApi.CurrentPlayerData.PlayerInCombat)
            {
                m_Potion = false;
                m_CurrentActionMode = ActionMode.LootTarget;
            }
            else if (!WowApi.CurrentPlayerData.PlayerHasTarget || 
                     !WowApi.CurrentPlayerData.TargetInCombat ||
                     WowApi.CurrentPlayerData.TargetFaction > 0)
            {
                Input.KeyPress(VirtualKeyCode.TAB);
                Helper.WaitSeconds(RegisterDelay);
                m_WalkBackwards = true;
                m_Turn = true;
            }
            else if (m_WalkBackwards)
            {
                Input.KeyDown(VirtualKeyCode.VK_S);

                if (m_Turn)
                {
                    if (m_TurnDirection)
                        Input.KeyDown(VirtualKeyCode.VK_D);
                    else
                        Input.KeyDown(VirtualKeyCode.VK_A);
                }

                Helper.WaitSeconds(0.125);

                Input.KeyUp(VirtualKeyCode.VK_S);

                if (m_Turn)
                {
                    if (m_TurnDirection)
                        Input.KeyUp(VirtualKeyCode.VK_D);
                    else
                        Input.KeyUp(VirtualKeyCode.VK_A);
                }

                m_WalkBackwards = false;
                m_Turn = false;
                m_TurnDirection = !m_TurnDirection;
            }
            // Wait for enemy to be close
            else if (!WowApi.CurrentPlayerData.IsInCloseRange)
            {
                m_FarTarget = true;
                m_WalkBackwards = true;
                Helper.WaitSeconds(1.0);
            }
            else if (m_FarTarget && WowApi.CurrentPlayerData.IsInCloseRange)
            {
                m_FarTarget = false;
                m_WalkBackwards = true;
                Helper.WaitSeconds(1.0);
            }
            else if (!WowApi.CurrentPlayerData.PlayerIsAttacking)
            {
                Input.KeyPress(VirtualKeyCode.VK_1);
                Helper.WaitSeconds(RegisterDelay);

                if(m_WowClassAutomater.IsMelee)
                    m_WalkBackwards = true;
            }
            else if (WowApi.CurrentPlayerData.PlayerHealthPercentage < 10 && !m_Potion)
            {
                Input.KeyPress(VirtualKeyCode.VK_0);
                m_Potion = true;
            }
            else
            {
                m_WowClassAutomater.KillTarget();
            }

            // Strafe randomly

            if ((Helper.RandomNumberGenerator.NextDouble() <= 0.005) && m_WowClassAutomater.IsMelee)
            {
                if (Helper.RandomNumberGenerator.NextDouble() >= 0.5)
                {
                    Task.Run(() =>
                    {
                        Input.KeyDown(VirtualKeyCode.LEFT);
                        Helper.WaitSeconds(0.075);
                        Input.KeyUp(VirtualKeyCode.LEFT);
                        Input.KeyDown(VirtualKeyCode.RIGHT);
                        Helper.WaitSeconds(0.075);
                        Input.KeyUp(VirtualKeyCode.RIGHT);
                    });
                }
                else
                {
                    Task.Run(() =>
                    {
                        Input.KeyDown(VirtualKeyCode.UP);
                        Helper.WaitSeconds(0.075);
                        Input.KeyUp(VirtualKeyCode.UP);
                        Input.KeyDown(VirtualKeyCode.DOWN);
                        Helper.WaitSeconds(0.075);
                        Input.KeyUp(VirtualKeyCode.DOWN);
                    });
                }
            }

        }

    }
}
