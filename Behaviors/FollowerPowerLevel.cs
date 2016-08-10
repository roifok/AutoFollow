using System;
using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;

namespace AutoFollow.Behaviors
{
    public class FollowerPowerLevel : BaseBehavior
    {
        public override BehaviorCategory Category => BehaviorCategory.Follower;

        public override BehaviorType Type => BehaviorType.Powerlevel;

        public override string Name => "Follower PowerLevel";

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

            if (await Party.AcceptPartyInvite())
                return true;

            if (await Party.RequestPartyInvite())
                return true;

            Log.Verbose("Waiting... (Out of Game)");
            await Coroutine.Sleep(500);
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

        public override async Task<bool> OnKilledRiftGaurdian(Message sender, EventData e)
        {
            if (e.IsLeaderEvent)
            {
                Log.Warn("{0} killed a rift gaurdian", e.OwnerHeroAlias);

                if (GameUI.ReviveAtCheckpointButton.IsVisible && GameUI.ReviveAtCheckpointButton.IsEnabled)
                {
                    GameUI.ReviveAtCheckpointButton.Click();
                    await Coroutine.Sleep(3000);
                }

                var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(8);
                while (Player.IsDead && DateTime.UtcNow < timeout)
                {
                    await Coroutine.Sleep(250);
                    await Coroutine.Yield();
                }

                await Coordination.TeleportToRiftGaurdianLoot(sender);
                return true;
            }
            return false;
        }
    }
}
