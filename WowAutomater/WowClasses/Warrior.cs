﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicWowNeuralParasite
{
    public class WarriorAutomater : WowClassAutomater
    {
        public override bool IsMelee => true;

        public override void AutoAttackTarget()
        {
            throw new NotImplementedException();
        }

        public override void FindTarget()
        {
            throw new NotImplementedException();
        }

        public override void KillTarget()
        {
            throw new NotImplementedException();
        }

        public override void RegenerateVitals()
        {
            throw new NotImplementedException();
        }
    }
}
