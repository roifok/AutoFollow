#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Trinity.Components.Combat.Resources;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.Game.Internals.Service;
using Zeta.TreeSharp;

#endregion

namespace AutoFollow.Behaviors
{
    public class BaseBehavior : IBehavior
    {

        public static DateTime LastActivated { get; set; }
        public static bool IsActive { get; set; }

        private static Composite _inGameHook;
        private static Composite _outGameHook;

        public virtual BehaviorCategory Category => BehaviorCategory.Unknown;

        public PartyObjective Objective { get; set; }

        public virtual string Name => "Default";

        public void Activate()
        {
            if (IsActive)
                return;

            Log.Info("Activated {0}", Name);

            _outGameHook = new ActionRunCoroutine(ret => BaseOutOfGameTask());
            _inGameHook = new ActionRunCoroutine(ret => InGameTask());

            InsertHooks();
            TreeHooks.Instance.OnHooksCleared += Instance_OnHooksCleared;                                    
            EventManager.WorldAreaChanged += OnWorldAreaChanged;
            EventManager.EngagedElite += OnEngagedElite;
            EventManager.LeftGame += OnLeftGame;
            EventManager.JoinedGame += OnJoinedGame;
            EventManager.Died += OnPlayerDied;
            EventManager.InTrouble += OnInTrouble;
            EventManager.UsedPortal += OnUsedPortal;
            EventManager.InviteRequest += OnInviteRequest;
            EventManager.LeavingGame += OnLeavingGame;
            EventManager.KilledRiftGaurdian += OnKilledRiftGaurdian;
            EventManager.SpawnedRiftGaurdian += OnSpawnedRiftGaurdian;

            Pulsator.OnPulse += Pulsator_OnPulse;
            LastActivated = DateTime.UtcNow;
            IsActive = true;
            OnActivated();
        }

        private void Instance_OnHooksCleared(object sender, EventArgs e)
        {
            InsertHooks();
        }

        private static void InsertHooks()
        {
            TreeHooks.Instance.InsertHook("BotBehavior", 0, _inGameHook);
            TreeHooks.Instance.InsertHook("OutOfGame", 0, _outGameHook);
        }

        private static void RemoveHooks()
        {
            TreeHooks.Instance.RemoveHook("BotBehavior", _inGameHook);
            TreeHooks.Instance.RemoveHook("OutOfGame", _outGameHook);
        }

        public void Deactivate()
        {
            RemoveHooks();
            TreeHooks.Instance.OnHooksCleared -= Instance_OnHooksCleared;
            EventManager.EngagedElite -= OnEngagedElite;
            EventManager.WorldAreaChanged -= OnWorldAreaChanged;
            EventManager.LeftGame -= OnLeftGame;
            EventManager.JoinedGame -= OnJoinedGame;
            EventManager.Died -= OnPlayerDied;
            EventManager.InTrouble -= OnInTrouble;
            EventManager.UsedPortal -= OnUsedPortal;
            EventManager.InviteRequest -= OnInviteRequest;
            EventManager.LeavingGame -= OnLeavingGame;
            EventManager.KilledRiftGaurdian -= OnKilledRiftGaurdian;
            EventManager.SpawnedRiftGaurdian -= OnSpawnedRiftGaurdian;

            Pulsator.OnPulse -= Pulsator_OnPulse;
            Log.Info("Stopped {0}", Name);
            IsActive = false;
            OnDeactivated();
        }

        /// <summary>
        /// Allow profiles and combat to run
        /// </summary>
        /// <param name="objective">reason or current state</param>
        /// <returns>false</returns>
        public bool Continue(PartyObjective objective)
        {
            Objective = objective;
            return false;
        }

        /// <summary>
        ///  Prevent profiles and combat from running, moves to next tick at tree-top.
        /// </summary>
        /// <param name="objective">reason or current state</param>
        /// <returns>false</returns>
        public bool Repeat(PartyObjective objective)
        {
            Objective = objective;
            return true;
        }

        private async Task<bool> BaseOutOfGameTask()
        {
            if (await DefaultOutOfGameChecks())
                return true;

            return await OutOfGameTask();
        }

        private async Task<bool> DefaultOutOfGameChecks()
        {
            if (!AutoFollow.Enabled || !ZetaDia.Service.Hero.IsValid || ZetaDia.Service.Hero.HeroId <= 0)
                return false;

            // Pulse does not fire while out of game. Need to be very careful how waits are handled.
            // Don't use long Coroutine.Sleeps out of game as it will prevent player updates for the duration.

            AutoFollow.Pulse();

            if (Service.IsConnected && AutoFollow.NumberOfConnectedBots == 0)
            {
                Log.Info("Waiting for bots to connect... ");
                await Coroutine.Sleep(500);
                return true;
            }

            if (DateTime.UtcNow < Coordination.WaitUntil)
            {
                Log.Debug("Waiting... (Generic OOC) Remaining={0}s", Coordination.WaitUntil.Subtract(DateTime.UtcNow).TotalSeconds);
                await Coroutine.Sleep(500);
                return true;
            }

            if (!IsGameReady || ZetaDia.IsInGame || Party.IsLocked || !ZetaDia.Service.IsValid || !ZetaDia.Service.Hero.IsValid)
            {
                Log.Verbose("Waiting... (Invalid State)");
                await Coroutine.Sleep(500);
                return true;
            }

            GameUI.SafeCheckClickButtons();
            return false;
        }

        public virtual async Task<bool> OutOfGameTask()
        {
            return false;
        }

        public virtual async Task<bool> InGameTask()
        {
            if (!AutoFollow.Enabled)
                return false;            

            if (!IsGameReady || !ZetaDia.IsInGame || Party.IsLocked)
            {
                Log.Verbose("Waiting (Invalid State)");
                return true;
            }

            AcceptRiftDialog();

            if (DateTime.UtcNow < Coordination.WaitUntil)
            {
                Log.Debug("Waiting... (Generic IC) Remaining={0}s", Coordination.WaitUntil.Subtract(DateTime.UtcNow).TotalSeconds);
                await Coroutine.Sleep(500);
                return true;
            }

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

            GameUI.SafeCheckClickButtons();
            return false;
        }

        private void AcceptRiftDialog()
        {
            if (GameUI.JoinRiftButton.IsValid && GameUI.JoinRiftButton.IsVisible)
            {
                if (GameUI.EmpoweredRiftToggle.IsValid && GameUI.EmpoweredRiftToggle.IsVisible)
                {
                    GameUI.EmpoweredRiftToggle.Click();
                    Thread.Sleep(500);
                }                
                GameUI.JoinRiftButton.Click();
            }
        }

        private void Pulsator_OnPulse(object sender, EventArgs e)
        {
            if (!TreeHooks.Instance.Hooks.Any(h => h.Value.Contains(_inGameHook)))
            {
                InsertHooks();
            }

            OnPulse();
        }

        public virtual void OnPulse()
        {

        }

        public virtual void OnActivated()
        {

        }

        public virtual void OnDeactivated()
        {

        }

        public virtual async Task<bool> OnEngagedElite(Message sender, EventData e) => false;

        public virtual async Task<bool> OnWorldAreaChanged(Message sender, EventData e) => false;

        public virtual async Task<bool> OnLeftGame(Message sender, EventData e) => false;

        public virtual async Task<bool> OnJoinedGame(Message sender, EventData e) => false;

        public virtual async Task<bool> OnPlayerDied(Message sender, EventData e) => false;

        public virtual async Task<bool> OnInTrouble(Message sender, EventData e) => false;

        public virtual async Task<bool> OnUsedPortal(Message sender, EventData e) => false;

        public virtual async Task<bool> OnInviteRequest(Message sender, EventData e) => false;

        public virtual async Task<bool> OnLeavingGame(Message sender, EventData e) => false;

        public virtual async Task<bool> OnKilledRiftGaurdian(Message sender, EventData e) => false;

        public virtual async Task<bool> OnSpawnedRiftGaurdian(Message sender, EventData e) => false;

        public static bool IsGameReady => ZetaDia.Service.IsValid && ZetaDia.Service.Hero.IsValid && AutoFollow.Enabled && !ZetaDia.IsLoadingWorld;

        public override int GetHashCode() => Name.GetHashCode() * 127;
    }
}


