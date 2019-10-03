﻿using WindowsInput.Native;

namespace ClassicWowNeuralParasite
{
    public class DruidAutomater : WowClassAutomater
    {
        private const int MOTW_COOLDOWN_TIME = 30 * 60;
        private const int THORNS_COOLDOWN_TIME = 10 * 60;
        private const int ROAR_COOLDOWN_TIME = 30;
        private const int FURY_COOLDOWN_TIME = 6;

        private const ushort MOTW_MANA_COST = 100;
        private const ushort THORNS_MANA_COST = 105;
        private const ushort ROAR_MANA_COST = 10;
        private const ushort MAUL_MANA_COST = 12;
        private const ushort FURY_MANA_COST = 30;
        private const ushort RAKE_MANA_COST = 37;
        private const ushort RIP_MANA_COST = 30;
        private const ushort CLAW_MANA_COST = 42;
        private const ushort HEALING_TOUCH_MANA_COST = 185;
        private const ushort WRATH_MANA_COST = 70;

        private const ushort RIP_COMBO_POINTS = 3;

        private const ushort HEALING_TOUCH_HEALTH_PERCENTAGE = 60;

        public bool Passive = true;

        public Action Attack;
        public Action Target;

        public BuffSpell Motw;
        public BuffSpell Thorns;

        public Spell Roar;
        public Spell Maul;
        public Spell TigersFury;
        public Spell Rake;
        public Spell Claw;
        public Spell Rip;
        public Spell HealingTouch;
        public Spell Wrath;

        public DruidAutomater()
        {
            Attack = new Action(VirtualKeyCode.VK_1);
            Target = new Action(VirtualKeyCode.TAB);

            Motw = new BuffSpell(VirtualKeyCode.VK_P, BuffType.MarkOfTheWild, MOTW_MANA_COST, MOTW_COOLDOWN_TIME);
            Thorns = new BuffSpell(VirtualKeyCode.VK_L, BuffType.Thorns, THORNS_MANA_COST, THORNS_COOLDOWN_TIME);

            Roar = new Spell(VirtualKeyCode.VK_R, ROAR_MANA_COST, ROAR_COOLDOWN_TIME);
            TigersFury = new Spell(VirtualKeyCode.VK_5, FURY_MANA_COST, FURY_COOLDOWN_TIME);
            Rake = new Spell(VirtualKeyCode.VK_6, RAKE_MANA_COST, useOnce: true);
            Claw = new Spell(VirtualKeyCode.VK_2, CLAW_MANA_COST);
            Rip = new Spell(VirtualKeyCode.VK_3, RIP_MANA_COST, comboPointsCost: RIP_COMBO_POINTS);
            HealingTouch = new Spell(VirtualKeyCode.VK_3, HEALING_TOUCH_MANA_COST, healthPercentage: HEALING_TOUCH_HEALTH_PERCENTAGE);
            Wrath = new Spell(VirtualKeyCode.VK_2, WRATH_MANA_COST);
            Maul = new Spell(VirtualKeyCode.VK_2, MAUL_MANA_COST);
        }

        public override bool IsMelee
        {
            get
            {
                switch (WowApi.CurrentPlayerData.Shape)
                {
                    case 0:
                        return false;
                    case 1:
                        return true;
                    case 2:
                        return true;
                    case 3:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public override void AutoAttackTarget()
        {
            switch (WowApi.CurrentPlayerData.Shape)
            {
                case 0:
                    AutoAttackTargetDruidHumanoid();
                    break;
                case 1:
                    AutoAttackTargetDruidBear();
                    break;
                case 3:
                    AutoAttackTargetDruidCat();
                    break;
                default:
                    break;
            }
        }

        private void AutoAttackTargetDruidHumanoid()
        {
            switch(Passive)
            {
                case true:
                    AutoAttackTargetDruidHumanoidPassive();
                    break;
                case false:
                    AutoAttackTargetDruidHumanoidActive();
                    break;
            }

        }

        private void AutoAttackTargetDruidHumanoidPassive()
        {
            if (Thorns.CanCastSpell)
                Thorns.CastSpell();
            else if (Motw.CanCastSpell)
                Motw.CastSpell();
            else if (HealingTouch.CanCastSpell)
                HealingTouch.CastSpell();
        }

        private void AutoAttackTargetDruidHumanoidActive()
        {
            if (!WowApi.CurrentPlayerData.PlayerInCombat)
                return;
            else if (!WowApi.CurrentPlayerData.PlayerHasTarget)
                Target.Act();
            else if (!WowApi.CurrentPlayerData.PlayerIsAttacking)
                Attack.Act();
            else if (HealingTouch.CanCastSpell)
                HealingTouch.CastSpell();
            else if (Wrath.CanCastSpell)
                Wrath.CastSpell();
        }

        private void AutoAttackTargetDruidBear()
        {
            if (!WowApi.CurrentPlayerData.PlayerInCombat)
                return;
            else if (!WowApi.CurrentPlayerData.PlayerHasTarget)
                Target.Act();
            else if (!WowApi.CurrentPlayerData.PlayerIsAttacking)
                Attack.Act();
            else if (Roar.CanCastSpell)
                Roar.CastSpell();
            else if (Maul.CanCastSpell)
                Maul.CastSpell();
        }

        private void AutoAttackTargetDruidCat()
        {
            if (!WowApi.CurrentPlayerData.PlayerInCombat)
                return;
            else if (!WowApi.CurrentPlayerData.PlayerHasTarget)
                Target.Act();
            else if (!WowApi.CurrentPlayerData.PlayerIsAttacking)
                Attack.Act();
            else if (Rake.CanCastSpell)
                Rake.CastSpell();
            else if (TigersFury.CanCastSpell)
                TigersFury.CastSpell();
            else if (Rip.CanCastSpell)
                Rip.CastSpell();
            else if (Claw.CanCastSpell)
                Claw.CastSpell();
        }

        public override void FindTarget()
        {
            WaypointFollower.FollowWaypoints(true);

            // Look for target
            Input.KeyPress(VirtualKeyCode.TAB);
            Helper.WaitSeconds(0.1);

            // Found a target
            if (WowApi.CurrentPlayerData.PlayerHasTarget)
            {
                bool validEnemy = WowApi.CurrentPlayerData.TargetHealth == 100 &&
                                    !WowApi.CurrentPlayerData.TargetInCombat &&
                                    !WowApi.CurrentPlayerData.IsTargetPlayer &&
                                    WowApi.CurrentPlayerData.IsInFarRange &&
                                    !WowApi.CurrentPlayerData.IsInCloseRange;

                if (validEnemy && WowApi.CurrentPlayerData.PlayerMana >= 20)
                {
                    WaypointFollower.StopFollowingWaypoints();

                    // PewPew Wrath
                    Helper.WaitSeconds(1);
                    Input.KeyPress(VirtualKeyCode.VK_2);
                    Helper.WaitSeconds(1.75);
                    Input.KeyPress(VirtualKeyCode.VK_2);
                    Helper.WaitSeconds(1.75);
                }
            }
        }

        public override void KillTarget()
        {
            if (WowApi.CurrentPlayerData.PlayerMana >= 20)
            {
                Input.KeyPress(VirtualKeyCode.VK_2);
            }
        }

        public override void RegenerateVitals()
        {
            
        }
    }
}
