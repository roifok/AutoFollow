using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Service;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace AutoFollow.Behaviors
{
    public class FollowerPowerLevel : BaseBehavior
    {
        public override BehaviorCategory Category
        {
            get { return BehaviorCategory.Follower; }
        }

        public override BehaviorType Type
        {
            get { return BehaviorType.Powerlevel; }
        }

        public override string Name
        {
            get { return "Follower PowerLevel"; }
        }

        public override async Task<bool> OutOfGameTask()
        {
            if (await base.OutOfGameTask())
                return true;

            if (await Party.LeaveWhenInWrongGame())
                return true;

            if (await Party.StartGameWhenPartyReady())
                return true;

            if (await Party.JoinLeadersGameInprogress())
                return true;

            if (await Party.QuickJoinLeader())
                return true;

            Log.Verbose("Waiting... (Out of Game)");
            return true;
        }

        public override async Task<bool> InGameTask()
        {
            if (await base.InGameTask())
                return true;

            if (await Party.TeleportWhenInDifferentWorld(AutoFollow.CurrentLeader))
                return true;

            return false;
        }

    }
}
