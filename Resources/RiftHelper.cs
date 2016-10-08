using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;

namespace AutoFollow.Resources
{
    public static class RiftHelper
    {
        public static RiftInfo CurrentRift { get; set; }

        static RiftHelper()
        {
            GameEvents.OnGameJoined += GameEvents_OnGameJoined;
            Pulsator.OnPulse += Pulsator_OnPulse;
        }

        private static void Pulsator_OnPulse(object sender, EventArgs e)
        {
            Update();
        }

        private static void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            Update(true);            
        }

        private static void Reset()
        {
            IsLockedOutOfRift = false;
        }

        private static DateTime _lastUpdateTime = DateTime.MinValue;

        private static void Update(bool force = false)
        {
            if (!force && DateTime.UtcNow.Subtract(_lastUpdateTime).TotalMilliseconds < 250)
                return;

            _lastUpdateTime = DateTime.UtcNow;

            var currentRift = ZetaDia.CurrentRift;
            if (currentRift == null)
                return;

            RiftQuest = new RiftQuest();

            var currentWorldId = ZetaDia.CurrentWorldSnoId;

            CurrentRift = currentRift;
            IsInRift = RiftWorldIds.Contains(currentWorldId);
            CurrentWorldId = currentWorldId;
            IsStarted = currentRift.IsStarted;
            Type = currentRift.Type; ;
            CurrentDepth = GetDepthByWorldId(currentWorldId);
            IsGreaterRiftStarted = IsStarted && Type == RiftType.Greater;
            IsInGreaterRift = IsInRift && Type == RiftType.Greater;

            IsLockedOutOfRift = !ZetaDia.Me.IsParticipatingInTieredLootRun && IsGreaterRiftStarted;

            IsGreaterRiftProfile = Player.CurrentMessage != null && 
                Player.CurrentMessage.ProfileTagName != null && 
                Player.CurrentMessage.ProfileTagName.ToLower().Contains("greater");
        }

        public static bool IsInGreaterRift { get; set; }

        public static RiftQuest RiftQuest { get; set; }

        public static bool IsGreaterRiftStarted { get; set; }

        public static int CurrentDepth { get; set; }

        public static RiftType Type { get; set; }

        public static bool IsInRift { get; set; }

        public static int CurrentWorldId { get; set; }

        public static bool IsLockedOutOfRift { get; set; }

        public static bool IsStarted { get; set; }

        public static bool IsGreaterRiftProfile { get; set; }

        public static readonly HashSet<int> RiftWorldIds = new HashSet<int>
        {
            288454,
            288685,
            288687,
            288798,
            288800,
            288802,
            288804,
            288806,
        };

        private static int GetDepthByWorldId(int worldId)
        {
            switch (worldId)
            {
                case 288454: return 1;
                case 288685: return 2;
                case 288687: return 3;
                case 288798: return 4;
                case 288800: return 5;
                case 288802: return 6;
                case 288804: return 7;
                case 288806: return 8;
            }
            return -1;
        }

    }

    public class RiftQuest
    {
        private const int RIFT_QUEST_ID = 337492;

        public enum RiftStep
        {
            NotStarted,
            KillingMobs,
            BossSpawned,
            UrshiSpawned,
            Cleared,
            Completed
        }

        public QuestState State { get; private set; }
        public RiftStep Step { get; private set; }

        public RiftQuest()
        {
            Step = RiftStep.NotStarted;
            State = QuestState.NotStarted;

            var quest = QuestInfo.FromId(RIFT_QUEST_ID);
            if (quest != null)
            {
                State = quest.State;
                var step = quest.QuestStep;
                switch (step)
                {
                    case 1: // Normal rift 
                    case 13: // Greater rift
                        Step = RiftStep.KillingMobs;
                        break;
                    case 3: // Normal rift 
                    case 16: // Greater rift 
                        Step = RiftStep.BossSpawned;
                        break;
                    case 34:
                        Step = RiftStep.UrshiSpawned;
                        break;
                    case 10:
                        Step = RiftStep.Cleared;
                        break;
                }
            }

            if (State == QuestState.Completed)
            {
                Step = RiftStep.Completed;
            }
        }

    }

}




