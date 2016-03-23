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
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
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

        public override void OnPulse()
        {
            StayCloseToPlayer(AutoFollow.CurrentLeader);
        }

        public override async Task<bool> InGameTask()
        {
            // Returning True => go to next tick immediately, execution starts again from top of the tree.
            // Returning False => allow execution to continue to lower hooks. Such as profiles, Adventurer.

            if (await base.InGameTask())
                return true;

            if (await Party.LeaveWhenInWrongGame())
                return true;

            if (await Questing.UpgradeGems())
                return false;

            if (await Coordination.StartTownRunWithLeader())
                return false;

            if (await Coordination.WaitForGreaterRiftInProgress())
                return true;

            if (await Questing.LeaveRiftWhenDone())
                return true;

            if (Targetting.RoutineWantsToLoot() || Targetting.RoutineWantsToClickGizmo())
                return false;

            if (await Coordination.FollowLeaderThroughPortal())
                return true;

            if (await Coordination.TeleportWhenInDifferentWorld(AutoFollow.CurrentLeader))
                return true;

            if (await Coordination.TeleportWhenTooFarAway(AutoFollow.CurrentLeader))
                return true;

            //if (await Combat.StandInFocussedPowerArea())
            //    return true;

            if (await Movement.MoveToPlayer(AutoFollow.CurrentLeader, Settings.Coordination.FollowDistance))
                return false;

            // ------- below wont be executed except if leader in different world.

            if (await Questing.ReturnToGreaterRift())
                return true;

            if (await Movement.MoveToGreaterRiftExitPortal())
                return true;

            return false;
        }

        /// <summary>
        /// Turn combat (Trinity) on and off while the follower is far away from the leader.   
        /// </summary>
        public void StayCloseToPlayer(Message player)
        {
            if (player.Distance > Settings.Coordination.CatchUpDistance && !Player.IsInTown && //!Navigation.IsBlocked && 
                !Navigator.StuckHandler.IsStuck && Player.HitpointsCurrentPct > 0.4 && !Targetting.RoutineWantsToAttackGoblin())
            {
                Targetting.State = CombatState.Disabled;
            }
            else
            {
                Targetting.State = CombatState.Enabled;
            }
        }

        //public async Task<bool> AttackWithPlayer(Message player)
        //{
        //    if (player.IsInCombat && player.CurrentTarget != null && player.Distance < 150f && 
        //        player.CurrentTarget.Distance < 150f && ZetaDia.Me.IsInCombat && Data.Monsters.Count(m => m.Distance <= 30f) < 10)
        //    {
        //        Log.Info("Moving to attack {0}'s target - {1} Distance={2}", 
        //            player.HeroAlias, player.CurrentTarget.Name, player.CurrentTarget.Distance);

        //        if(await Movement.MoveTo(() => AutoFollow.GetUpdatedMessage(player).CurrentTarget, player.CurrentTarget.Name, 10f, () => ZetaDia.Me.IsInCombat || !ZetaDia.Me.Movement.IsMoving))
        //            return true;                
        //    }
        //    return false;
        //}

        public override async Task<bool> OnUsedPortal(Message sender, EventData e)
        {            
            if (e.IsLeaderEvent)
            {
                var portal = e.NewValue as Interactable;
                if (portal == null)
                {
                    Log.Debug("The portal details weren't provided in event. :(");
                    return false;
                }

                if (portal.WorldSnoId != Player.CurrentWorldSnoId)
                {
                    Log.Debug("Portal is in a different world.");
                    return false;
                }

                Log.Info("Leader ({0}) used a portal ({1})", sender.HeroAlias, portal.InternalName);
                await Coroutine.Sleep(2000);

                if (AutoFollow.CurrentLeader.IsInSameWorld)
                {
                    Log.Debug("Leader is in same world.");
                    return false;
                }
                
                var positionMatch = Data.Portals.FirstOrDefault(p => (int)p.Position.X == (int)portal.ActorPosition.X && (int)p.Position.Y == (int)portal.ActorPosition.Y);
                if (positionMatch != null)
                {
                    Log.Info("Portal found by position, lets use it!");
                    await Movement.MoveToAndInteract(positionMatch);
                    return true;
                }

                var nameMatch = Data.Portals.FirstOrDefault(p => p.CommonData.Name.Contains(portal.BaseInternalName));
                if (nameMatch != null)
                {
                    Log.Info("Portal found by name, lets use it!");
                    await Movement.MoveToAndInteract(nameMatch);
                    return true;
                }

                var closestPortal = Data.Portals.OrderBy(p => p.Position.Distance(Player.Position)).FirstOrDefault();
                if (closestPortal != null)
                {
                    Log.Info("Trying our luck with this nearby portal...");
                    await Movement.MoveToAndInteract(closestPortal);
                    return true;
                }

                Log.Info("Unable to find the portal we need to clicky :(");
            }
            return false;
        }

        public override async Task<bool> OnWorldAreaChanged(Message sender, EventData e)
        {
            if (e.IsLeaderEvent)
            {
                Log.Info("Leader ({0}) changed world from {1} to {2}", 
                    sender.HeroAlias, e.OldValue, e.NewValue);
            }
            return false;
        }

        public override async Task<bool> OnLeavingGame(Message sender, EventData e)
        {
            if (e.IsLeaderEvent && Player.IsInGame)
            {
                Log.Info("Leader ({0}) is leaving game, lets leave too!",
                    sender.HeroAlias, e.OldValue, e.NewValue);

                await Party.LeaveGame();
                await Coroutine.Sleep(1000);
                return true;
            }
            return false;
        }

        public override async Task<bool> OnEngagedElite(Message sender, EventData e)
        {
            if (e.IsLeaderEvent)
            {
                Log.Info("Leader ({0}) is attacking a Unique! {1} at {2} DistanceFromMe={3}",
                    Common.CleanString(sender.HeroAlias),
                    Common.CleanString(sender.CurrentTarget.Name),
                    sender.Position.ToString(),
                    ZetaDia.Me.Position.Distance(sender.CurrentTarget.Position));
            }
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
