﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Service;

namespace AutoFollow.Coroutines
{
    public class Party
    {
        private static UIElement _leaderQuickJoinElement;
        private static DateTime _lastAttemptQuickJoin;
        private static DateTime _lastInviteAttempt = DateTime.MinValue;
        private static DateTime _lastInviteRequestTime = DateTime.MinValue;
        private static DateTime _lastLeaveGameAttempt = DateTime.MinValue;

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

        /// <summary>
        /// Handle being in a party lobby.
        /// </summary>
        public static async Task<bool> JoinGameInProgress()
        {
            if (!ZetaDia.Service.Party.IsPartyLeader && Player.IsInParty && !ZetaDia.IsInGame)
            {
                if (!IsLeaderInParty())
                {
                    //todo add check to make sure bot is in the out of game party lobby screen
                    //Log.Info("Leaving party, leader is not in this group!");
                    //GameUI.OutOfGameLeavePartyButton.Click();
                    //Coordination.WaitFor(TimeSpan.FromSeconds(2));
                }
                else if (GameUI.PlayGameButton.IsEnabled && AutoFollow.CurrentLeader.IsInGame)
                {
                    Log.Info("We're in party, leader already in game, joining game!");
                    GameUI.PlayGameButton.Click();
                    Coordination.WaitFor(TimeSpan.FromSeconds(2));
                    return true;
                }

            }
            return false;
        }

        /// <summary>
        /// Clicks the start game button once all players have left the game.
        /// </summary>
        public static async Task<bool> StartGameWhenPartyReady()
        {
            if (ZetaDia.Service.Party.IsPartyLeader && Player.NumPlayersInParty > 1 &&
                Player.NumPlayersInParty == AutoFollow.NumberOfConnectedBots + 1 && GameUI.PlayGameButton.IsEnabled &&
                Player.NumPlayersInParty == AutoFollow.CurrentParty.Count(p => !p.IsInGame))
            {
                Log.Warn("We're all here, starting game");
                GameUI.PlayGameButton.Click();
                Coordination.WaitFor(TimeSpan.FromSeconds(2));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Leave the party if there are strange people in it.
        /// </summary>
        public static async Task<bool> LeavePartyUnknownPlayersInGame()
        {
            if (AutoFollow.NumberOfConnectedBots == 0 && !GameUI.ChangeQuestButton.IsEnabled &&
                Settings.Misc.AvoidUnknownPlayers)
            {
                Log.Info("Unknown players in the party and no connected bots, leaving party.");
                GameUI.OutOfGameLeavePartyButton.Click();
                Coordination.WaitFor(TimeSpan.FromSeconds(5));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tell other bots to leave their game and wait for them to do so.
        /// </summary>
        public static async Task<bool> WaitForPlayersToLeaveGame()
        {
            if (Player.IsInParty && !Player.IsInGame && (AutoFollow.CurrentParty.Any(m => m.IsInGame) || !GameUI.ChangeQuestButton.IsEnabled))
            {
                Log.Info("Waiting for party members to leave game");
                EventManager.FireEvent(new EventData(EventType.LeavingGame, null, null, true));
                Coordination.WaitFor(TimeSpan.FromSeconds(5));
                return true;
            }

            return false;
        }

        /// <summary>
        /// If the bot designated as the leader is currently in the battle.net group/party.
        /// </summary>
        public static bool IsLeaderInParty()
        {
            var message = AutoFollow.CurrentLeader;
            if (ZetaDia.IsInGame)
            {
                return message.IsInSameGame;
            }

            if (!ZetaDia.Service.IsInGame && ZetaDia.Service.Party.NumPartyMembers > 1)
            {
                var slot1NameElement = GameUI.PartySlot1Name;
                var cleanText1 = Common.CleanString(slot1NameElement.Text);
                if (slot1NameElement.IsVisible && slot1NameElement.HasText && Message.IsBattleTag(cleanText1, message.BattleTagEncrypted))
                {
                    Log.Debug("Leader is in slot 1 of our party");
                    return true;
                }
                var slot2NameElement = GameUI.PartySlot2Name;
                var cleanText2 = Common.CleanString(slot2NameElement.Text);
                if (slot2NameElement.IsVisible && slot2NameElement.HasText && Message.IsBattleTag(cleanText2, message.BattleTagEncrypted))
                {
                    Log.Debug("Leader is in slot 2 of our party");
                    return true;
                }
                var slot3NameElement = GameUI.PartySlot3Name;
                var cleanText3 = Common.CleanString(slot3NameElement.Text);
                if (slot3NameElement.IsVisible && slot3NameElement.HasText && Message.IsBattleTag(cleanText3, message.BattleTagEncrypted))
                {
                    Log.Debug("Leader is in slot 3 of our party");
                    return true;
                }
                var slot4NameElement = GameUI.PartySlot4Name;
                var cleanText4 = Common.CleanString(slot4NameElement.Text);
                if (slot4NameElement.IsVisible && slot4NameElement.HasText && Message.IsBattleTag(cleanText4, message.BattleTagEncrypted))
                {
                    Log.Debug("Leader is in slot 4 of our party");
                    return true;
                }
            }

            Log.Debug("Leader is not in our party");
            return false;
        }

        /// <summary>
        /// Check if we're in the same game as the leader, and leave if they're different.
        /// </summary>
        public static async Task<bool> LeaveWhenInWrongGame()
        {
            if (ZetaDia.Globals.IsLoadingWorld || DateTime.UtcNow.Subtract(ChangeMonitor.LastGameJoinedTime).Seconds < 5)
                return false;

            // Leaves party when out of game the d3-party leader and not the bot-leader.
            // Disbands party if leader leaves it.
            if (Player.IsFollower && Player.IsInParty && ZetaDia.Service.Party.IsPartyLeader && !ZetaDia.IsInGame &&
                GameUI.ElementIsVisible(GameUI.OutOfGameLeavePartyButton) && ZetaDia.Service.Party.CurrentPartyLockReasonFlags == PartyLockReasonFlag.None &&
                DateTime.UtcNow.Subtract(_lastAttemptQuickJoin) > TimeSpan.FromSeconds(10))
            {
                Log.Info("We are a follower but leader of party - leaving party");
                GameUI.OutOfGameLeavePartyButton.Click();
                Coordination.WaitFor(TimeSpan.FromSeconds(5));
                return true;
            }

            if (ZetaDia.IsInGame && Player.IsFollower && !ZetaDia.Globals.IsLoadingWorld)
            {
                if (!AutoFollow.CurrentLeader.IsInSameGame && !AutoFollow.CurrentLeader.IsLoadingWorld)
                {
                    if (LeaderGameMismatchLeaveTime == DateTime.MinValue)
                    {
                        Log.Warn("Leader gameId is different/invalid!", AutoFollow.CurrentLeader.IsInSameGame);
                        LeaderGameMismatchLeaveTime = DateTime.UtcNow.AddSeconds(10);
                        return false;
                    }

                    if (DateTime.UtcNow > LeaderGameMismatchLeaveTime)
                    {
                        Log.Warn("Leader is in a different game, Leave Game!", AutoFollow.CurrentLeader.IsInSameGame);
                        LeaderGameMismatchLeaveTime = default(DateTime);
                        await LeaveGame(true);
                        Coordination.WaitFor(TimeSpan.FromSeconds(10));
                        return true;
                    }
                }
                else
                {
                    LeaderGameMismatchLeaveTime = DateTime.MinValue;
                }
            }
            return false;
        }

        public static DateTime LeaderGameMismatchLeaveTime = DateTime.MinValue;

        /// <summary>
        /// Use the quickjoin links on the hero screen to join the leaders game.
        /// </summary>
        public static async Task<bool> QuickJoinLeader()
        {
            if (!AutoFollow.CurrentLeader.IsQuickJoinEnabled)
                return false;

            var quickJoinElements = UIElement.UIMap.Where(e => e.Name.Contains("CallToArmsElem")).ToList();
            if (!quickJoinElements.Any())
                return false;

            if (DateTime.UtcNow.Subtract(_lastAttemptQuickJoin) > TimeSpan.FromSeconds(5) && !Player.IsInParty && AutoFollow.CurrentLeader.IsInGame && AutoFollow.CurrentLeader != null)
            {                
                quickJoinElements.ForEach(e =>
                {
                    var name = Common.CleanString(e.Text);
                    if (!Message.IsBattleTag(name, AutoFollow.CurrentLeader.BattleTagEncrypted))
                    {
                        var isLeadersRealId = Message.IsRealId(name, AutoFollow.CurrentLeader.RealIdNameEncrypted);
                        if (!isLeadersRealId)
                            return;          
                    }
                    _leaderQuickJoinElement = e;
                    Log.Info("Found Quick Join for Server's Game, Joining!");
                    GameUI.SafeClick(e, ClickDelay.NoDelay, "Quick Join Server", 3000);

                });
                _lastAttemptQuickJoin = DateTime.UtcNow;
                return true;
            }

            // todo check for (ErrorDialog.IsVisible && ErrorDialog.ErrorCode == 300008. some other plugin is closing it too fast.

            if (_leaderQuickJoinElement != null && _leaderQuickJoinElement.IsValid && _leaderQuickJoinElement.IsVisible &&
                !_leaderQuickJoinElement.IsEnabled)
            {
                Log.Info("Quick Joining Game...");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Invites another bot to the current party.
        /// </summary>
        /// <param name="follower">the player to be invited</param>
        public static async Task<bool> InviteFollower(Message follower)
        {
            if (DateTime.UtcNow.Subtract(_lastInviteAttempt).TotalSeconds < 1)
                return false;

            if (ZetaDia.Service.Party.CurrentPartyLockReasonFlags != PartyLockReasonFlag.None)
            {
                Log.Info("Party is locked, can't invite right now");
                return false;
            }

            if (AutoFollow.NumberOfConnectedBots == 0)
                return false;

            if (!GameUI.FriendsListContent.IsVisible)
            {
                Log.Info("Opening Social Panel");
                GameUI.SocialFlyoutButton.Click();
                await Coroutine.Sleep(250);
            }

            var contacts = new List<StackPanelReader.StackPanelItem>();
            contacts.AddRange(GameUI.SocialFriendsListStackPanel.GetStackPanelItems());
            contacts.AddRange(GameUI.SocialLocalPlayersStackPanel.GetStackPanelItems());
            contacts.AddRange(GameUI.SocialRecentPlayersStackPanel.GetStackPanelItems());

            if (!contacts.Any())
            {
                Log.Info("No friends, local or recent players were found!");
                return false;
            }

            foreach (var item in contacts)
            {
                var name = Common.CleanString(item.TextElement.Text);
                var presenceElement = item.TextElement.GetSiblingByName("Presence");
                var matches = Common.SocialPanelEntryPresenceRegex.GroupMatches(presenceElement.Text);
                var activity = matches.StringByGroupName["Activity"].FirstOrDefault();
                var level = matches.NumberByGroupName["Level"].FirstOrDefault();
                var paragon = matches.NumberByGroupName["Paragon"].FirstOrDefault();
                var actorClass = matches.StringByGroupName["Class"].FirstOrDefault();

                var isBattleTag = Message.IsBattleTag(name, follower.BattleTagEncrypted);
                if (!isBattleTag)
                {
                    var isClassMatch = string.Equals(actorClass?.StringValue, follower.ActorClass.ToString(), StringComparison.CurrentCultureIgnoreCase);
                    var isParagonMatch = isClassMatch && paragon?.NumberValue == follower.Paragon && level?.NumberValue == follower.Level;
                    var isLeadersRealId = Message.IsRealId(name, AutoFollow.CurrentLeader.RealIdNameEncrypted);
                    if (!isParagonMatch && Settings.Misc.InviteByParagon && !isLeadersRealId || !Settings.Misc.InviteByParagon && !isLeadersRealId)
                        continue;
                }

                Log.Info("Found follower on friends list!");
                var inviteButton = item.TextElement.GetSiblingByName("PartyInviteButton");
                inviteButton.Click();
                _lastInviteAttempt = DateTime.UtcNow;

                await Coroutine.Sleep(250);

                if (GameUI.FriendsListContent.IsVisible)
                {
                    Log.Info("Closing Social Panel");
                    GameUI.SocialFlyoutButton.Click();
                    await Coroutine.Sleep(250);
                }

                return true;
            }

            Log.Info("Unable to find invitation requester on friends list!");
            _lastInviteAttempt = DateTime.UtcNow;
            return false;
        }

        /// <summary>
        /// Request leader send us an invite to their party.
        /// </summary>
        public static async Task<bool> RequestPartyInvite()
        {
            if (AutoFollow.NumberOfConnectedBots == 0)
                return false;

            var timeSinceRequest = DateTime.UtcNow.Subtract(_lastInviteRequestTime).TotalSeconds;
            if (timeSinceRequest < 5)
            {
                Log.Verbose("Waiting to join party.. Request Sent {0}s ago", timeSinceRequest);
                return false;
            }

            if (!Player.IsInParty)
            {
                Log.Warn("Requesting party invite!", timeSinceRequest);
                _lastInviteRequestTime = DateTime.UtcNow;
                EventManager.FireEvent(new EventData(EventType.InviteRequest));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Accept a party invite if its from a bot we are connected to.
        /// </summary>
        public static async Task<bool> AcceptPartyInvite()
        {
            var partyInviteOkButton = GameUI.PartyInviteOK;
            if (partyInviteOkButton != null && partyInviteOkButton.IsVisible && partyInviteOkButton.IsEnabled)
            {
                if (Settings.Misc.AlwaysAcceptInvites)
                {
                    Log.Warn("Accepting Party Invite (Always Accept Setting)");
                    partyInviteOkButton.Click();
                    await Coroutine.Sleep(250);
                    return true;
                }

                var invitePlayerName = Common.CleanString(GameUI.PartyInviteFromPlayerName.Text);
                var isLeadersBattleTag = Message.IsBattleTag(invitePlayerName, AutoFollow.CurrentLeader.BattleTagEncrypted);
                if (!isLeadersBattleTag)
                {
                    var matches = Common.InviteRequestRegex.GroupMatches(invitePlayerName);
                    var realId = matches.StringByGroupName["RealId"].FirstOrDefault();
                    var isLeadersRealId = Message.IsRealId(realId?.StringValue, AutoFollow.CurrentLeader.RealIdNameEncrypted);
                    if (!isLeadersRealId)
                    {
                        Log.Warn("Party invite is from '{0}' who is not our leader, ignoring", Settings.Misc.HideHeroName ? "[Redacted]" : invitePlayerName);
                        await Coroutine.Sleep(new Random().Next(1000, 10000));
                        GameUI.PartyInviteCancelButton.Click();
                        return false;
                    }
                }

                Log.Warn("Accepting Party Invite");
                partyInviteOkButton.Click();
                await Coroutine.Sleep(250);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Leave the current game.
        /// </summary>
        public static async Task<bool> LeaveGame(bool leaveParty = false)
        {
            if (!ZetaDia.IsInGame)
                return true;

            if (ZetaDia.Globals.IsLoadingWorld || GameUI.LeaveGameButton.IsVisible)
                return false;

            if (ZetaDia.Service.Party.CurrentPartyLockReasonFlags != PartyLockReasonFlag.None)
                return false;

            if (DateTime.UtcNow.Subtract(_lastLeaveGameAttempt).TotalSeconds < 5)
                return false;

            ZetaDia.Service.Party.LeaveGame(!leaveParty);
            _lastLeaveGameAttempt = DateTime.UtcNow;
            return true;
        }
    }
}