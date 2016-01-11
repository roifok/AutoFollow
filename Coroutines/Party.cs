using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Service;

namespace AutoFollow.Coroutines
{
    public class Party
    {
        public static bool IsLocked 
        {
            get
            {
                if (ZetaDia.Service.Party.CurrentPartyLockReasonFlags != PartyLockReasonFlag.None)
                {
                    Log.Verbose("PartyLockFlags = {0}", ZetaDia.Service.Party.CurrentPartyLockReasonFlags);
                    return true;
                }
                return false;
            }
        }

        public async static Task<bool> JoinLeadersGameInprogress()
        {
            if (!Player.IsPartyleader && Player.IsInParty && !ZetaDia.IsInGame && GameUI.PlayGameButton.IsEnabled && AutoFollow.CurrentLeader.IsInGame)
            {
                Log.Info("We're in party, leader already in game, joining game!");
                GameUI.SafeClick(GameUI.PlayGameButton, ClickDelay.Delay, "Join Game", 3000, true);
                return true;
            }
            return false;
        }

        public async static Task<bool> TeleportWhenInDifferentWorld(Message player)
        {
            if (RiftHelper.IsGreaterRiftStarted)
                return false;

            if (Player.IsFollower && player.WorldSnoId != Player.Instance.Message.WorldSnoId && player.IsInSameGame && !player.IsInCombat)
            {
                Log.Info("{0} is in a different world... attempting teleport!", player.HeroName);
                await TeleportToPlayer.Execute(player);
                return true;
            }
            return false;
        }

        public async static Task<bool> TeleportWhenTooFarAway(Message player)
        {
            if (Player.IsFollower && player.WorldSnoId == Player.Instance.Message.WorldSnoId && player.IsInSameGame && !player.IsInCombat && player.Distance > 300f)
            {
                Log.Info("{0} is getting quite far away... attempting teleport!", player.HeroName);
                await TeleportToPlayer.Execute(player);
                return true;
            }
            return false;
        }

        public async static Task<bool> StartGameWhenPartyReady()
        {
            if (Player.IsPartyleader && Player.NumPlayersInParty > 1 && Player.NumPlayersInParty == AutoFollow.NumberOfConnectedBots + 1 && GameUI.PlayGameButton.IsEnabled && Player.NumPlayersInParty == AutoFollow.CurrentParty.Count(p => !p.IsInGame))
            {
                Log.Warn("We're all here, starting game");
                GameUI.SafeClick(GameUI.PlayGameButton, ClickDelay.Delay, "Join Game", 3000, true);
                return true;
            }
            return false;
        }

        public async static Task<bool> LeavePartyUnknownPlayersInGame()
        {
            if (AutoFollow.NumberOfConnectedBots == 0 && !GameUI.ChangeQuestButton.IsEnabled)
            {
                Log.Info("Unknown players in the party and no connected bots, leaving party.");
                GameUI.SafeClick(GameUI.OutOfGameLeavePartyButton, ClickDelay.NoDelay, "Leave Party Button", 1000);
                return true;
            }
            return false;
        }

        public async static Task<bool> WaitForPlayersToLeaveGame()
        {
            if (Player.IsInParty && Player.IsPartyleader && AutoFollow.ClientMessages.Any(f => f.Value.IsInGame) || !GameUI.ChangeQuestButton.IsEnabled)
            {
                Log.Info("Waiting for party members to leave game");
                return true;
            }
            return false;
        }

        public async static Task<bool> WaitToBecomePartyLeader()
        {
            if (ZetaDia.Service.IsValid && ZetaDia.Service.Hero.IsValid && !ZetaDia.IsInGame && !Player.IsPartyleader)
            {
                Log.Info("Waiting to become party leader");
                return true;
            }

            if (AutoFollow.CurrentFollowers.Any(f => f.IsInParty && f.IsInSameGame) && !Player.IsPartyleader)
            {
                Log.Info("Waiting to become party leader");
                return true;
            }
            return false;
        }

        public async static Task<bool> LeaveWhenInWrongGame()
        {
            if(!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || DateTime.UtcNow.Subtract(Player.LastGameJoinedTime).Seconds < 5)
                return false;

            if (Player.IsFollower && Player.IsInParty && Player.IsPartyleader && !ZetaDia.IsInGame && GameUI.ElementIsVisible(GameUI.OutOfGameLeavePartyButton) && DateTime.UtcNow.Subtract(_lastAttemptQuickJoin) > TimeSpan.FromSeconds(10))
            {
                Log.Info("We are a follower but leader of party - leaving party");
                GameUI.SafeClick(GameUI.OutOfGameLeavePartyButton, ClickDelay.NoDelay, "Leave Party Button", 1000);
                return true;
            }

            if (ZetaDia.IsInGame && Player.IsFollower && Player.Instance.IsInGame && !AutoFollow.CurrentLeader.IsInGame && Player.IsInParty && AutoFollow.CurrentLeader.BNetPartyMembers > 1)
            {
                Log.Warn("Leader is waiting for me to leave game!", AutoFollow.CurrentLeader.IsInSameGame);
                await SafeLeaveGame.Execute();
                return true;
            }

            if (ZetaDia.IsInGame && Player.IsFollower && !AutoFollow.CurrentLeader.IsMe && !AutoFollow.CurrentLeader.IsInSameGame)
            {
                Log.Warn("Leader is in a different game, Leave Game!", AutoFollow.CurrentLeader.IsInSameGame);
                await SafeLeaveGame.Execute();
                return true;
            }

            return false;
        }

        private static UIElement _leaderQuickJoinElement;
        private static DateTime _lastAttemptQuickJoin;

        public static async Task<bool> QuickJoinLeader()
        {
            //if (AutoFollow.CurrentLeader.IsInTown)
            //    return false;

            if (DateTime.UtcNow.Subtract(_lastAttemptQuickJoin) > TimeSpan.FromSeconds(5) && !Player.IsInParty && AutoFollow.CurrentLeader.IsInGame && AutoFollow.CurrentLeader != null)
            {
                var quickJoinElements = UIElement.UIMap.Where(e => e.Name.Contains("CallToArmsElem")).ToList();

                if (AutoFollow.NumberOfConnectedBots > 0 && Player.IsFollower && !AutoFollow.CurrentLeader.IsQuickJoinEnabled)
                {
                    Log.Info("Current leader doesn't have QuickJoin enabled!");
                }

                if (!quickJoinElements.Any())
                    return false;

                quickJoinElements.ForEach(e =>
                {
                    if (Message.IsBattleTag(Common.CleanString(e.Text), AutoFollow.CurrentLeader.BattleTagEncrypted))
                    {
                        _leaderQuickJoinElement = e;
                        Log.Info("Found Quick Join for Server's Game, Joining!");
                        GameUI.SafeClick(e, ClickDelay.NoDelay, "Quick Join Server", 3000);
                    }
                });
                _lastAttemptQuickJoin = DateTime.UtcNow;
                return true;
            }

            if (_leaderQuickJoinElement != null && _leaderQuickJoinElement.IsValid && _leaderQuickJoinElement.IsVisible && !_leaderQuickJoinElement.IsEnabled)
            {
                Log.Info("Quick Joining Game...");
                return true;
            }

            return false;
        }

        private static DateTime _lastInviteAttempt = DateTime.MinValue;                

        public static async Task<bool> InviteFollower(Message follower)
        {
            if (DateTime.UtcNow.Subtract(_lastInviteAttempt).TotalSeconds < 5)
                return false;

            if (AutoFollow.NumberOfConnectedBots == 0)
                return false;

            if (!GameUI.FriendsListContent.IsVisible)
            {
                Log.Info("Opening Social Panel");
                GameUI.SocialFlyoutButton.Click();
                await Coroutine.Sleep(250);
            }

            var stackPanel = GameUI.SocialFriendsListStackPanel;
            var stackPanelItems = stackPanel.GetStackPanelItems();

            if (!stackPanelItems.Any())
            {
                stackPanel = GameUI.SocialLocalPlayersStackPanel;
                stackPanelItems = stackPanel.GetStackPanelItems();

                if (!stackPanelItems.Any())
                {
                    Log.Info("No friends or local players were found!");
                    return false;
                }
            }

            foreach (var item in stackPanelItems)
            {
                var text = item.TextElement.Text;
                
                var isBattleTag = Message.IsBattleTag(Common.CleanString(text), follower.BattleTagEncrypted);

                Log.Info("Found {0} IsBattleTag={1} TagEnc={2}", text, isBattleTag, follower.BattleTagEncrypted);

                if (isBattleTag)
                {
                    Log.Info("Found follower on friends list!");
                    var inviteButton = item.TextElement.GetSiblingByName("PartyInviteButton");
                    inviteButton.Click();
                    await Coroutine.Sleep(250);
                }
            }

            _lastInviteAttempt = DateTime.UtcNow;
            return true;        
        }

        private static DateTime _lastInviteRequestTime = DateTime.MinValue;
        public static async Task<bool> RequestPartyInvite()
        {
            var partyInviteOkButton = GameUI.PartyInviteOK;
            if (partyInviteOkButton != null && partyInviteOkButton.IsVisible && partyInviteOkButton.IsEnabled)
            {
                var invitePlayerName = Common.CleanString(GameUI.PartyInviteFromPlayerName.Text);
                var isLeadersBattleTag = Message.IsBattleTag(invitePlayerName, AutoFollow.CurrentLeader.BattleTagEncrypted);
                if (!isLeadersBattleTag)
                {
                    Log.Info("{0}", AutoFollow.CurrentLeader.BattleTagEncrypted);
                    Log.Warn("Party invite is from '{0}' who is not our leader, ignoring", invitePlayerName);
                    await Coroutine.Sleep(new Random().Next(1000, 10000));
                    GameUI.PartyInviteCancelButton.Click();                    
                    return false;
                }

                Log.Warn("Accepting Party Invite");
                partyInviteOkButton.Click();
                await Coroutine.Sleep(250);
                return true;
            }

            var timeSinceRequest = DateTime.UtcNow.Subtract(_lastInviteRequestTime).TotalSeconds;
            if (timeSinceRequest < 10 && !Player.IsInParty)
            {
                Log.Verbose("Waiting to join party.. Request Sent {0}s ago", timeSinceRequest);
                return false;
            }

            _lastInviteRequestTime = DateTime.UtcNow;
            EventManager.FireEvent(new EventData(EventType.InviteRequest));
            return true;
        }
    }
}

