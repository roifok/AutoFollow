using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;

namespace AutoFollow.Behaviors
{
    public class LeaderManual : BaseBehavior
    {
        public override BehaviorCategory Category => BehaviorCategory.Leader;

        public override string Name => "Leader Manual";

        public override void OnActivated()
        {
            Targetting.State = CombatState.Disabled;
        }

        public override void OnDeactivated()
        {
            Targetting.State = CombatState.Enabled;
        }

        public override async Task<bool> OutOfGameTask()
        {
            return true;
        }

        public override async Task<bool> InGameTask()
        {
            if (!AutoFollow.CurrentLeader.IsValid)
                return false;

            return false;
        }

        public override async Task<bool> OnInviteRequest(Message sender, EventData e)
        {
            if (e.IsFollowerEvent)
            {
                Log.Info("My minion {0} is requesting a party invite!", sender.HeroAlias);
                await Party.InviteFollower(sender);
            }
            return true;
        }

    }
}