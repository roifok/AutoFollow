using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using AutoFollow.UI.Settings;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Service;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace AutoFollow.Behaviors
{
    public class FollowerCombat : BaseBehavior
    {
        public override BehaviorCategory Category
        {
            get { return BehaviorCategory.Follower; }
        }

        public override BehaviorType Type
        {
            get { return BehaviorType.Follow; }
        }

        public override string Name
        {
            get { return "Follower Combat"; }
        }

        public override async Task<bool> OutOfGameTask()
        {
            if (await base.OutOfGameTask())
                return true;

            if (!AutoFollow.CurrentLeader.IsValid)
            {
                if (!AutoFollow.CurrentParty.Any(m => m.IsLeader))
                {
                    Log.Info("There is currently no leader");
                }
                else
                {
                    Log.Debug("Leader message was invalid");
                }
                
                return true;
            }

            if (await Party.LeaveWhenInWrongGame())
                return true;

            if (await Party.StartGameWhenPartyReady())
                return true;

            if (await Party.JoinLeadersGameInprogress())
                return true;

            if (await Party.QuickJoinLeader())
                return true;

            if (await Party.RequestPartyInvite())
                return true;

            Log.Verbose("Waiting... (Out of Game)");
            return true;
        }

        public override async Task<bool> InGameTask()
        {
            if (await base.InGameTask())
                return true;

            if (!AutoFollow.CurrentLeader.IsValid)
            {
                if (!AutoFollow.CurrentParty.Any(m => m.IsLeader))
                {
                    Log.Info("There is currently no leader");
                }
                else
                {
                    Log.Debug("Leader message was invalid");
                }

                return true;
            }

            if (await Party.LeaveWhenInWrongGame())
                return true;

            if (await Questing.UpgradeGems())
                return false;

            if (await Coordination.WaitForGreaterRiftInProgress())
                return true;

            if (await Movement.FollowThroughPortal(AutoFollow.CurrentLeader))
                return true;

            if (await Party.TeleportWhenInDifferentWorld(AutoFollow.CurrentLeader))
                return true;

            if (await Party.TeleportWhenTooFarAway(AutoFollow.CurrentLeader))
                return true;

            if (await AttackWithPlayer(AutoFollow.CurrentLeader))
                return true;

            if (await Movement.MoveToPlayer(AutoFollow.CurrentLeader, 4f))
                return true;

            if (await Movement.UseNearbyRiftDeeperPortal())
                return true;

            if (await Movement.UseOpenRiftPortalInTown())
                return true;

            return false;
        }

        public async Task<bool> AttackWithPlayer(Message player)
        {
            if(player.IsInCombat && player.CurrentTarget != null && player.Distance < 150f && 
                player.CurrentTarget.Distance < 150f && ZetaDia.Me.IsInCombat && Data.Monsters.Count(m => m.Distance <= 30f) < 10)
            {
                Log.Info("Moving to attack {0}'s target - {1} Distance={2}", 
                    player.HeroName, player.CurrentTarget.Name, player.CurrentTarget.Distance);

                await Movement.MoveTo(player.CurrentTarget, player.CurrentTarget.Name, 10f, () => 
                    ZetaDia.Me.IsInCombat || !ZetaDia.Me.Movement.IsMoving);

                return true;
            }
            return false;
        }

        public override async Task<bool> OnUsedPortal(Message sender, EventData e)
        {            
            if (e.IsLeaderEvent)
            {
                var portal = e.NewValue as Interactable;

                Log.Info("Leader ({0}) used a portal ({1})",
                    sender.HeroName, portal != null ? portal.InternalName : "Unknown");
                
                if (portal == null)
                {
                    Log.Info("Portal that leader used was not provided in event.");
                    return false;
                }

                var nearbyPortal = Data.Portals.FirstOrDefault(p => p.Position == portal.ActorPosition);
                if (nearbyPortal != null)
                {
                    await Movement.MoveToAndInteract(nearbyPortal);
                }        
            }
            return false;
        }

        public override async Task<bool> OnWorldAreaChanged(Message sender, EventData e)
        {
            if (e.IsLeaderEvent)
            {
                Log.Info("Leader ({0}) changed world from {1} to {2}", 
                    sender.HeroName, e.OldValue, e.NewValue);
            }
            return false;
        }

        public override async Task<bool> OnEngagedElite(Message sender, EventData e)
        {
            if (e.IsLeaderEvent)
            {
                Log.Info("Leader ({0}) is attacking a Unique! {1} at {2} DistanceFromMe={3}",
                    Common.CleanString(sender.HeroName),
                    Common.CleanString(sender.CurrentTarget.Name),
                    sender.Position.ToString(),
                    ZetaDia.Me.Position.Distance(sender.CurrentTarget.Position));
            }
            return false;
        }

    }
}
