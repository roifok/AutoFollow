using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Resources;

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

            if (await Party.JoinGameOrLeaveParty())
                return true;

            if (await Party.QuickJoinLeader())
                return true;

            return true;
        }

        public override async Task<bool> InGameTask()
        {
            if (await base.InGameTask())
                return true;

            if (await Coordination.TeleportWhenInDifferentWorld(AutoFollow.CurrentLeader))
                return true;

            return false;
        }

    }
}
