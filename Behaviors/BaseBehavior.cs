#region

using System;
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

        private static Composite InGameHook;
        private static Composite OutGameHook;

        private readonly Data Data = new Data();

        public virtual BehaviorCategory Category
        {
            get { return BehaviorCategory.Unknown; }
        }

        public virtual BehaviorType Type
        {
            get { return BehaviorType.Default; }
        }

        public virtual string Name
        {
            get { return "Default"; }
        }

        public void Activate()
        {
            if (IsActive)
                return;

            Log.Info("Activated {0}", Name);

            OutGameHook = new ActionRunCoroutine(ret => OutOfGameTask());
            InGameHook = new ActionRunCoroutine(ret => InGameTask());          
            TreeHooks.Instance.OnHooksCleared += Instance_OnHooksCleared;
            InsertHooks();

            EventManager.WorldAreaChanged += OnWorldAreaChanged;
            EventManager.EngagedElite += OnEngagedElite;
            EventManager.LeftGame += OnLeftGame;
            EventManager.JoinedGame += OnJoinedGame;
            EventManager.Died += OnPlayerDied;
            EventManager.InTrouble += OnInTrouble;
            EventManager.UsedPortal += OnUsedPortal;
            EventManager.InviteRequest += OnInviteRequest;
            EventManager.RequestPartyLeaveGame += OnRequestPartyLeaveGame;

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
            TreeHooks.Instance.InsertHook("BotBehavior", 0, InGameHook);
            TreeHooks.Instance.InsertHook("OutOfGame", 0, OutGameHook);
        }

        public void Deactivate()
        {                               
            TreeHooks.Instance.RemoveHook("BotBehavior", InGameHook);
            TreeHooks.Instance.RemoveHook("OutOfGame", OutGameHook);
            TreeHooks.Instance.OnHooksCleared -= Instance_OnHooksCleared;

            EventManager.EngagedElite -= OnEngagedElite;
            EventManager.WorldAreaChanged -= OnWorldAreaChanged;
            EventManager.LeftGame -= OnLeftGame;
            EventManager.JoinedGame -= OnJoinedGame;
            EventManager.Died -= OnPlayerDied;
            EventManager.InTrouble -= OnInTrouble;
            EventManager.UsedPortal -= OnUsedPortal;
            EventManager.InviteRequest -= OnInviteRequest;

            Pulsator.OnPulse -= Pulsator_OnPulse;
            Log.Info("Stopped {0}", Name);
            IsActive = false;
            OnDeactivated();
        }

        public virtual async Task<bool> OutOfGameTask()
        {
            if (!IsGameReady || ZetaDia.IsInGame || Party.IsLocked || !ZetaDia.Service.IsValid || !ZetaDia.Service.Hero.IsValid)
            {
                Log.Verbose("Waiting... (Invalid State)");
                return true;
            }

            GameUI.SafeCheckClickButtons();
                        
            if(AutoFollow.CurrentBehavior.GetType() == typeof(BaseBehavior))
                return true;

            return false;
        }

        public virtual async Task<bool> InGameTask()
        {
            if (!IsGameReady || !ZetaDia.IsInGame || Party.IsLocked)
            {
                Log.Verbose("Waiting (Invalid State)");
                return true;
            }

            //Log.Verbose("Base InGameTask");
            GameUI.SafeCheckClickButtons();
            return false;
        }

        private void Pulsator_OnPulse(object sender, EventArgs e)
        {
            OnPulse();
        }

        public virtual void OnPulse()
        {
            if (!TreeHooks.Instance.Hooks.Any(h => h.Value.Contains(InGameHook)))
            {
                //Log.Info("InGameHook not found");
                InsertHooks();
            }
        }

        public virtual void OnActivated()
        {

        }

        public virtual void OnDeactivated()
        {

        }

        public virtual async Task<bool> OnEngagedElite(Message sender, EventData e)
        {            
            return false;
        }

        public virtual async Task<bool> OnWorldAreaChanged(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnLeftGame(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnJoinedGame(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnPlayerDied(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnInTrouble(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnUsedPortal(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnInviteRequest(Message sender, EventData e)
        {
            return false;
        }

        public virtual async Task<bool> OnRequestPartyLeaveGame(Message sender, EventData e)
        {
            return false;
        }
        
        public static bool IsGameReady
        {
            get { return ZetaDia.Service.IsValid && ZetaDia.Service.Hero.IsValid && AutoFollow.Enabled && !ZetaDia.IsLoadingWorld; }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + Type.GetHashCode() * 127;
        }
    }
}


