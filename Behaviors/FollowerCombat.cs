using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Trinity.Components.Combat.Resources;
using Trinity.Framework;
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
        public override BehaviorCategory Category => BehaviorCategory.Follower;
        public override string Name => "Follower Combat";

        public override async Task<bool> OutOfGameTask()
        {
            if (await Party.LeaveWhenInWrongGame())
                return Repeat(PartyObjective.LeavingGame);

            if (await Party.StartGameWhenPartyReady())
                return Repeat(PartyObjective.JoiningGame);

            if (await Party.JoinGameInProgress())
                return Repeat(PartyObjective.JoiningGame);

            if (await Party.QuickJoinLeader())
                return Repeat(PartyObjective.JoiningGame);

            if (await Party.AcceptPartyInvite())
                return Repeat(PartyObjective.JoiningParty);

            if (await Party.RequestPartyInvite())
                return Repeat(PartyObjective.JoiningParty);

            Log.Verbose("Waiting... (Out of Game)");
            await Coroutine.Sleep(500);
            return true;
        }

        public override void OnPulse()
        {
            State = GetFollowMode();
        }

        public override async Task<bool> InGameTask()
        {
            // Returning True => go to next tick immediately, execution starts again from top of the tree.
            // Returning False => allow execution to continue to lower hooks. Such as profiles, Adventurer.

            if (await base.InGameTask())
                return Repeat(PartyObjective.TownRun);

            if (await Party.LeaveWhenInWrongGame())
                return Repeat(PartyObjective.LeavingGame);

            if (await Questing.UpgradeGems())
                return Continue(PartyObjective.Quest);

            if (await Coordination.StartTownRunWithLeader())
                return Continue(PartyObjective.Teleporting);

            if (await Coordination.WaitForGreaterRiftInProgress())
                return Repeat(PartyObjective.TownRun);

            if (Targetting.RoutineWantsToLoot() || Targetting.RoutineWantsToClickGizmo())
                return Continue(PartyObjective.None);

            if (await Coordination.LeaveFinishedRift())
                return Repeat(PartyObjective.TownRun);

            if (await Coordination.FollowLeaderThroughPortal())
                return Repeat(PartyObjective.FollowLeader);

            if (await Coordination.TeleportWhenInDifferentWorld(AutoFollow.CurrentLeader))
                return Repeat(PartyObjective.Teleporting);

            if (await Coordination.TeleportWhenTooFarAway(AutoFollow.CurrentLeader))
                return Repeat(PartyObjective.Teleporting);

            if (await FollowLeader())
                return Continue(PartyObjective.FollowLeader);

            if (await Questing.ReturnToGreaterRift())
                return Repeat(PartyObjective.TownRun);

            if (await Movement.MoveToGreaterRiftExitPortal())
                return Repeat(PartyObjective.FollowLeader);

            return false;
        }

        private static FollowMode GetFollowMode()
        {
            if (Trinity.Components.Combat.Combat.Routines.Current.ShouldIgnoreFollowing())
            {
                Targetting.State = CombatState.Enabled;
                return FollowMode.None;
            }

            if (AutoFollow.CurrentLeader.InDifferentLevelArea && RiftHelper.IsInGreaterRift)
            {
                Targetting.State = CombatState.Disabled;
                return FollowMode.MoveToRiftExit;
            }

            if (!AutoFollow.CurrentLeader.InDifferentLevelArea && !Targetting.IsPriorityTarget)
            {
                if (AutoFollow.CurrentLeader.Distance > Settings.Coordination.CatchUpDistance)
                {
                    Targetting.State = CombatState.Disabled;
                    return FollowMode.ChaseLeader;
                }
                if (AutoFollow.CurrentLeader.Distance > Settings.Coordination.FollowDistance)
                {
                    Targetting.State = CombatState.Pulsing;
                    return FollowMode.FollowLeader;
                }
            }
           
            Targetting.State = CombatState.Enabled;
            return FollowMode.Combat;          
        }
        
        public static FollowMode State { get; set; }

        public enum FollowMode
        {
            None = 0,
            Combat,
            MoveToRiftExit,
            FollowLeader,
            ChaseLeader,
        }

        private static async Task<bool> FollowLeader()
        {
            if (!AutoFollow.CurrentLeader.IsInSameGame)
                return false;

            switch (State)
            {
                case FollowMode.FollowLeader:
                case FollowMode.ChaseLeader:
                    await Navigator.MoveTo(AutoFollow.CurrentLeader.Destination);
                    return true;
                
                case FollowMode.MoveToRiftExit:
                    await Movement.MoveToGreaterRiftExitPortal();
                    return true;
            }
            return false;
        }

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
                if (closestPortal != null && !Player.IsInTown)
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
