using System;
using System.Diagnostics;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals;

namespace AutoFollow.Coroutines.Resources
{
    public class GameUI
    {
        private const ulong mercenaryOKHash = 1591817666218338490;
        private const ulong conversationSkipHash = 0x942F41B6B5346714;
        private const ulong talkToInteractButton1Hash = 0x8EB3A93FB1E49EB8;
        private const ulong confirmTimedDungeonOKHash = 0xF9E7B8A635A4F725;
        private const ulong genericOKHash = 0x891D21408238D18E;
        private const ulong partyLeaderBossAcceptHash = 0x69B3F61C0F8490B0;
        private const ulong partyFollowerBossAcceptHash = 0xF495983BA9BE450F;
        private const ulong potionButtonHash = 0xE1F43DD874E42728;
        private const ulong bountyRewardDialogHash = 0x278249110947CA00;
        private const ulong gamePotionHash = 0xE1F43DD874E42728;
        private const ulong tieredRiftRewardContinueHash = 0xE9F673BF3A02ECD5;
        private const ulong stashBuyNewTabButtonHash = 0x1B876AD677C9080;
        private const ulong salvageAllNormalsButton = 0xCE31A05539BE5710;
        private const ulong salvageAllMagicsButton = 0xD58A34C0A51E3A60;
        private const ulong salvageAllRaresButton = 0x9AA6E1AD644CF239;
        private const ulong reviveAtCorpseHash = 0xE3CBD66296A39588;
        private const ulong reviveAtCheckpointHash = 0xBFAAF48BA9316742;
        private const ulong reviveInTownHash = 0x7A2AF9C0F3045ADA;
        private const ulong riftCompleteOkButton = 0x6DA3168427892076;
        private const ulong patchOKButton = 0x16C4B9DB83655800;
        private const ulong MercenaryPartyOKHash = 0xF85A00117F5902E9;
        private const ulong GenericOKHash = 0x891D21408238D18E;
        private const ulong PartyLeaderBossAcceptHash = 0x69B3F61C0F8490B0;
        private const ulong PartyFollowerBossAcceptHash = 0xF495983BA9BE450F;
        private const ulong PlayGameButtonHash = 0x51A3923949DC80B7;
        private const ulong CustomizeBannerCloseHash = 0xF92E21E990666E6B;
        private const ulong PartySlot2IconHash = 0x24F157EFB9C1744A;
        
        private const ulong BattleNetOKHash = 0xB4433DA3F648A992;
        private const ulong ChangeQuestButtonHash = 0xC4A9CC94C0A929B;
        private const ulong LeaveGameButtonHash = 0x3B55BA1E41247F50;
        private const ulong OutOfGameLeavePartyButtonHash = 0xCD03E1585F7150B9;


        //[1A9C8110] Mouseover: 0x80AFF6E674F9ACB4, Name: Root.NormalLayer.BattleNetFooter_main.LayoutRoot.ButtonContainer.SocialFlyoutButton
        private const ulong SocialFlyoutButtonHash = 0x80AFF6E674F9ACB4;

        //[1A1CDE00] Mouseover: 0x96B374DEB0CE4FB6, Name: Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.FilterContainer.FilterInputBox
        private const ulong SocialFilterInputBoxHash = 0x96B374DEB0CE4FB6;

        //[1A1CC580] Mouseover: 0xE5AEF56E036011D0, Name: Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.FilterContainer.FilterClearButton
        private const ulong SocialFilterInputClearButtonHash = 0xE5AEF56E036011D0;

        //[1A3E9590] Mouseover: 0x9EFA05648195042D, Name: Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification.PartyOkButton
        private const ulong PartyInviteOKHash = 0x9EFA05648195042D;

        //[212DD0F0] Mouseover: 0xDBAA2AE6E3255205, Name: Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification.PartyCancelButton
        private const ulong PartyInviteCancelHash = 0xDBAA2AE6E3255205;

        //[1A2111B0] Mouseover: 0x2726C5120FB9C929, Name: Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.SocialListContainer.SocialList.FriendsListContainer.FriendsGroupHeader.ToggleFriendsListButton
        private const ulong ToggleFriendsListButtonHash = 0x2726C5120FB9C929;

        //[21DA5F50] Mouseover: 0x5F7789EE7EE455DE, Name: Root.TopLayer.ContextMenus.PlayerContextMenu.PlayerContextMenuContent.PlayerContextMenuList.RequestToJoinParty
        private const ulong RequestToJoinPartyButtonHash = 0x5F7789EE7EE455DE;

        //[20CBAAC0] Mouseover: 0xDED5B2ABED3A471A, Name: Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent
        private const ulong FriendsListContentHash = 0xDED5B2ABED3A471A;

        //[20527430] Mouseover: 0xA41B846275DC7A09, Name: Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification
        private const ulong PartyInviteDialogHash = 0xA41B846275DC7A09;

        //12614685561186959926 Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification.PartyPlayer
        private const ulong PartyInviteFromPlayerNameHash = 12614685561186959926;


        internal const int ClickThreadSleepInterval = 125;
        private static DateTime _lastCheckedUiButtons = DateTime.MinValue;
        private static readonly Stopwatch clickTimer = new Stopwatch();
        private static readonly Random clickDelayRnd = new Random();
        private static readonly TimeSpan ClickTimerTimeout = new TimeSpan(0, 0, 0, 5, 250);
        private static int clickTimerRandomVal = -1;
        private static DateTime lastSafeClickCheck = DateTime.MinValue;
        private static DateTime _lastClick = DateTime.MinValue;

        public static bool IsAnyTownWindowOpen
        {
            get
            {
                if (KanaisCubeWindow.IsVisible)
                    return true;

                if (UIElements.VendorWindow.IsVisible)
                    return true;

                if (UIElements.StashWindow.IsVisible)
                    return true;

                return false;
            }
        }

        public static UIElement FriendsListContent
        {
            get { return UIElement.FromHash(FriendsListContentHash); }
        }

        public static UIElement PartyInviteFromPlayerName
        {

            get { return UIElement.FromHash(PartyInviteFromPlayerNameHash); }
        }

        public static UIElement RequestToJoinPartyButton
        {
            get { return UIElement.FromHash(RequestToJoinPartyButtonHash); }
        }

        public static UIElement ToggleFriendsListButton
        {
            get { return UIElement.FromHash(ToggleFriendsListButtonHash); }
        }

        public static UIElement PartyInviteDialog
        {
            get { return UIElement.FromHash(PartyInviteDialogHash); }
        }

        public static UIElement SocialFriendsListStackPanel
        {
            //[212DE9D0] Mouseover: 0x6546288812192AFF, Name: Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer
            //.FriendsListContent.SocialListContainer.SocialList.FriendsListContainer.FriendsList._content._stackpanel._item0
            get
            {
                return UIElement.FromName("Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.SocialListContainer.SocialList.FriendsListContainer.FriendsList._content._stackpanel"); 
            }
        }
        public static UIElement SocialLocalPlayersStackPanel
        {
            //[1F09BAE0] Mouseover: 0x648E62A0B271E5BF, Name: Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer
            //.FriendsListContent.SocialListContainer.SocialList.LocalPlayersListContainer.LocalPlayersList._content._stackpanel._item0
            get
            {
                return UIElement.FromName("Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.SocialListContainer.SocialList.LocalPlayersListContainer.LocalPlayersList._content._stackpanel");
            }
        }

        public static UIElement SocialFlyoutButton
        {
            get { return UIElement.FromHash(SocialFlyoutButtonHash); }
        }

        public static UIElement PartyInviteCancelButton
        {
            get { return UIElement.FromHash(PartyInviteCancelHash); }
        }

        public static UIElement SocialFilterInputBox
        {
            get { return UIElement.FromHash(SocialFilterInputBoxHash); }
        }

        public static UIElement SocialFilterInputClearButton
        {
            get { return UIElement.FromHash(SocialFilterInputClearButtonHash); }
        }

        public static UIElement StashWindow
        {
            get { return UIElements.StashWindow; }
        }

        public static UIElement KanaisCubeWindow
        {
            get { return UIElement.FromHash(0xCF916D15D32769F9); }
        }

        public static UIElement ChinaStoreCloseButton
        {
            get { return UIElement.FromHash(0xCDD29D7F6A61DAD8); }
        }

        public static UIElement CloseCreditsButton
        {
            get { return UIElement.FromHash(0x981391BBDF64B009); }
        }

        public static UIElement PatchOKButton
        {
            get { return UIElement.FromHash(patchOKButton); }
        }

        public static UIElement RiftCompleteOkButton
        {
            get { return UIElement.FromHash(riftCompleteOkButton); }
        }

        public static UIElement StashDialogMainPage
        {
            get { return UIElement.FromHash(0xB83F0423F7247928); }
        }

        public static UIElement StashDialogMainPageTab1
        {
            get { return UIElement.FromHash(0x276522EDF3238841); }
        }

        public static UIElement JoinRiftButton
        {
            get { return UIElement.FromHash(0x42E152B771A6BCC1); }
        }

        public static UIElement ReviveAtCorpseButton
        {
            get { return UIElement.FromHash(0xE3CBD66296A39588); }
        }

        public static UIElement ReviveAtCheckpointButton
        {
            get { return UIElement.FromHash(0xBFAAF48BA9316742); }
        }

        public static UIElement ReviveInTownButton
        {
            get { return UIElement.FromHash(0x7A2AF9C0F3045ADA); }
        }

        public static UIElement SalvageAllNormalsButton
        {
            get { return UIElement.FromHash(salvageAllNormalsButton); }
        }

        public static UIElement SalvageAllMagicsButton
        {
            get { return UIElement.FromHash(salvageAllMagicsButton); }
        }

        public static UIElement SalvageAllRaresButton
        {
            get { return UIElement.FromHash(salvageAllRaresButton); }
        }

        public static UIElement GamePotion
        {
            get { return UIElement.FromHash(gamePotionHash); }
        }

        public static UIElement BountyRewardDialog
        {
            get { return UIElement.FromHash(bountyRewardDialogHash); }
        }

        public static UIElement PotionButton
        {
            get { return UIElement.FromHash(potionButtonHash); }
        }

        public static UIElement ConfirmTimedDungeonOK
        {
            get { return UIElement.FromHash(confirmTimedDungeonOKHash); }
        }

        public static UIElement MercenaryOKButton
        {
            get { return UIElement.FromHash(mercenaryOKHash); }
        }

        public static UIElement ConversationSkipButton
        {
            get { return UIElement.FromHash(conversationSkipHash); }
        }

        public static UIElement PartyLeaderBossAccept
        {
            get { return UIElement.FromHash(partyLeaderBossAcceptHash); }
        }

        public static UIElement PartyFollowerBossAccept
        {
            get { return UIElement.FromHash(partyFollowerBossAcceptHash); }
        }

        public static UIElement GenericOK
        {
            get { return UIElement.FromHash(genericOKHash); }
        }

        public static UIElement TalktoInteractButton1
        {
            get { return UIElement.FromHash(talkToInteractButton1Hash); }
        }

        public static UIElement StashBuyNewTabButton
        {
            get { return UIElement.FromHash(stashBuyNewTabButtonHash); }
        }

        public static UIElement TieredRiftRewardContinueButton
        {
            get { return UIElement.FromHash(tieredRiftRewardContinueHash); }
        }

        public static bool IsPartyDialogVisible
        {
            get { return IsElementVisible(PartyFollowerBossAccept) || IsElementVisible(PartyLeaderBossAccept); }
        }

        public static UIElement OutOfGameLeavePartyButton
        {
            get { return UIElement.FromHash(OutOfGameLeavePartyButtonHash); }
        }

        public static UIElement PlayGameButton
        {
            get { return UIElement.FromHash(PlayGameButtonHash); }
        }

        public static UIElement ChangeQuestButton
        {
            get { return UIElement.FromHash(ChangeQuestButtonHash); }
        }

        public static UIElement BattleNetOK
        {
            get { return UIElement.FromHash(BattleNetOKHash); }
        }

        public static UIElement MercenaryPartyOK
        {
            get { return UIElement.FromHash(MercenaryPartyOKHash); }
        }

        public static UIElement PartyInviteOK
        {
            get { return UIElement.FromHash(PartyInviteOKHash); }
        }

        public static UIElement CustomizeBannerClose
        {
            get { return UIElement.FromHash(CustomizeBannerCloseHash); }
        }

        // this is used to check if we are in a party, from the game menu
        public static UIElement PartySlot2Icon
        {
            get { return UIElement.FromHash(PartySlot2IconHash); }
        }

        public static UIElement LeaveGameButton
        {
            get { return UIElement.FromHash(LeaveGameButtonHash); }
        }

        public static bool IsElementVisible(UIElement element)
        {
            if (element == null)
                return false;
            if (!element.IsValid)
                return false;
            if (!element.IsVisible)
                return false;

            return true;
        }

        /// <summary>
        /// Checks to see if ZetaDia.Me.IsValid, element is visible, triggers fireWorldTransferStart if needed and clicks the
        /// element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="fireWorldTransfer"></param>
        /// <returns></returns>
        public static bool SafeClickElement(UIElement element, string name = "", bool fireWorldTransfer = false)
        {
            if (!ZetaDia.IsInGame)
                return false;

            if (!IsElementVisible(element))
                return false;

            if (fireWorldTransfer)
                GameEvents.FireWorldTransferStart();

            Log.Info("Clicking UI element {0} ({1})", name, element.BaseAddress);
            element.Click();
            return true;
        }

        public static void SafeClickUIButtons()
        {
            if (ZetaDia.IsLoadingWorld)
                return;

            // These buttons should be clicked with no delay

            if (SafeClickElement(CloseCreditsButton, "Close Credits Button"))
                return;
            if (SafeClickElement(PatchOKButton, "Patch Update OK Button"))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(ChinaStoreCloseButton, "Closing China Store Window"))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(BountyRewardDialog, "Bounty Reward Dialog"))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(ConversationSkipButton, "Conversation Button"))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(PartyLeaderBossAccept, "Party Leader Boss Accept", true))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(PartyFollowerBossAccept, "Party Follower Boss Accept", true))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(TalktoInteractButton1, "Conversation Button"))
                return;
            if (DateTime.UtcNow.Subtract(_lastCheckedUiButtons).TotalMilliseconds <= 500)
                return;
            if (ZetaDia.IsInGame && SafeClickElement(JoinRiftButton, "Join Rift Accept Button", true))
                return;

            _lastCheckedUiButtons = DateTime.UtcNow;

            var loopingAnimationEndTime = 0;
            try
            {
                loopingAnimationEndTime = ZetaDia.Me.LoopingAnimationEndTime;
            }
            catch (Exception ex)
            {
                Log.Debug("Error in getting LoopingAnimationEndTime {0}", ex.Message);
            }

            if (loopingAnimationEndTime > 0)
                return;
            if (ZetaDia.IsInGame && SafeClickElement(MercenaryOKButton, "Mercenary OK Button"))
                return;
            if (SafeClickElement(RiftCompleteOkButton, "Rift Complete OK Button"))
                return;
            if (SafeClickElement(GenericOK, "GenericOK"))
                return;
            if (SafeClickElement(UIElements.ConfirmationDialogOkButton, "ConfirmationDialogOKButton", true))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(ConfirmTimedDungeonOK, "Confirm Timed Dungeon OK Button", true))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(StashBuyNewTabButton, "Buying new Stash Tab"))
                return;
            if (ZetaDia.IsInGame && SafeClickElement(TieredRiftRewardContinueButton, "Tiered Rift Reward Continue Button"))
                return;
        }

        internal static bool ElementIsVisible(UIElement uiElement, string name = "")
        {
            if (uiElement == null)
            {
                return false;
            }

            if (!uiElement.IsValid)
            {
                //if (AutoFollowSettings.Instance.DebugLogging)
                //    Info.Debug("Element {0} is not valid", uiElement.Hash);
                return false;
            }

            if (!uiElement.IsVisible)
            {
                //if (AutoFollowSettings.Instance.DebugLogging)
                //    Info.Debug("Element {0} {1} ({2}) is not visible", uiElement.Hash, name, uiElement.Name);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for known windows, buttons, etc and clicks them
        /// </summary>
        internal static void SafeCheckClickButtons()
        {
            try
            {
                if (!ZetaDia.IsInGame)
                {
                    SafeClick(BattleNetOK, ClickDelay.NoDelay, "Battle.Net OK", 1000, true);
                }

                // limit this thing running to once a second, to save CPU
                if (DateTime.UtcNow.Subtract(lastSafeClickCheck).TotalMilliseconds < 1000)
                    return;

                if (ZetaDia.IsLoadingWorld)
                    return;

                if (ZetaDia.IsPlayingCutscene)
                    return;

                if (!ZetaDia.Service.IsValid)
                    return;

                lastSafeClickCheck = DateTime.UtcNow;

                // Handled seperately out of game
                if (ZetaDia.IsInGame)
                    SafeClick(PartyInviteOK, ClickDelay.Delay, "Party Invite", 750, true);

                SafeClick(GenericOK, ClickDelay.Delay, "Generic OK", 0, true);
                SafeClick(BattleNetOK, ClickDelay.NoDelay, "Battle.Net OK", 1000, true);

                if (!ZetaDia.IsInGame)
                    return;

                if (ZetaDia.Me.IsDead)
                    return;

                if (!ZetaDia.Me.IsValid)
                    return;

                SafeClick(PartyLeaderBossAccept, ClickDelay.NoDelay, "Boss Portal Accept", 0, true);

                if (PartyFollowerBossAccept.IsValid && PartyFollowerBossAccept.IsVisible)
                {
                    SafeClick(PartyFollowerBossAccept, ClickDelay.NoDelay, "Boss Portal Accept", 0, true);
                    ProfileManager.Load(ProfileManager.CurrentProfile.Path);
                }

                SafeClick(MercenaryPartyOK, ClickDelay.NoDelay, "Mercenary Party OK");
                SafeClick(CustomizeBannerClose, ClickDelay.NoDelay, "Customize Banner Close");
            }
            catch (Exception ex)
            {
                Log.Info("Error clicking UI Button: " + ex);
            }
        }

        private static void SetClickTimerRandomVal()
        {
            clickTimerRandomVal = clickDelayRnd.Next(1250, 2250);
            Log.Info("Random timer set to {0}ms", clickTimerRandomVal);
        }

        private static bool ClickTimerRandomReady()
        {
            return clickTimer.IsRunning && clickTimer.ElapsedMilliseconds >= clickTimerRandomVal;
        }

        private static bool ClickTimerRandomNotReady()
        {
            var timeRemaining = clickTimerRandomVal - clickTimer.ElapsedMilliseconds;
            Log.Info("Pausing bot -  waiting for button timer {0}ms", clickTimerRandomVal);
            return clickTimer.IsRunning && clickTimer.ElapsedMilliseconds < clickTimerRandomVal;
        }

        /// <summary>
        /// Clicks a UI Element after a random interval.
        /// </summary>
        /// <param name="uiElement"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool SafeClick(UIElement uiElement, ClickDelay delayOption, string name = "", int postClickThreadSleepDuration = 0, bool fireWorldTransferStart = false)
        {
            try
            {
                if (DateTime.UtcNow.Subtract(_lastClick).TotalMilliseconds < 500)
                    return false;

                if (ElementIsVisible(uiElement, name))
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        Log.Debug("{0} button is visible", name);
                    }
                    else
                    {
                        Log.Debug("{0}={1} is visible", uiElement.Hash, uiElement.Name);
                    }

                    if (!clickTimer.IsRunning && delayOption == ClickDelay.Delay)
                    {
                        clickTimer.Start();
                        SetClickTimerRandomVal();
                    }
                    else if ((ClickTimerRandomReady() && ElementIsVisible(uiElement, name)) || delayOption == ClickDelay.NoDelay)
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            Log.Info("Clicking {0} button", name);
                        }
                        else
                        {
                            Log.Info("Clicking {0}={1}", uiElement.Hash, uiElement.Name);
                        }

                        // sleep plugins for a bit
                        if (ZetaDia.IsInGame && fireWorldTransferStart)
                            GameEvents.FireWorldTransferStart();

                        _lastClick = DateTime.UtcNow;
                        uiElement.Click();
                        BotMain.PauseFor(TimeSpan.FromMilliseconds(ClickThreadSleepInterval));

                        if (postClickThreadSleepDuration > 0)
                            BotMain.PauseFor(TimeSpan.FromMilliseconds(postClickThreadSleepDuration));

                        clickTimer.Reset();
                    }
                    else
                    {
                        Log.Debug("Pausing bot, waiting for {0}={1}", uiElement.Hash, uiElement.Name);
                        BotMain.PauseWhile(ClickTimerRandomNotReady, 0, ClickTimerTimeout);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Info("Error clicking UI button {0}: " + ex, name);
                return false;
            }
        }
    }

    public enum ClickDelay
    {
        NoDelay = 0,
        Delay = 1
    }
}