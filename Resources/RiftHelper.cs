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

        private static DateTime LastUpdateTime = DateTime.MinValue;

        private static void Update(bool force = false)
        {
            if (!force && DateTime.UtcNow.Subtract(LastUpdateTime).TotalMilliseconds < 250)
                return;

            LastUpdateTime = DateTime.UtcNow;

            var currentRift = ZetaDia.CurrentRift;
            if (currentRift == null)
                return;

            RiftQuest = new RiftQuest();

            var currentWorldId = ZetaDia.CurrentWorldSnoId;
            var isInRift = RiftWorldIds.Contains(currentWorldId);    
            var isStarted = currentRift.IsStarted;
            var type = currentRift.Type;
            var currentDepth = GetDepthByWorldId(currentWorldId);

            if (!IsLockedOutOfRift && !IsStarted && isStarted && type == RiftType.Greater && !isInRift)
                IsLockedOutOfRift = true;

            if (isInRift && type == RiftType.Greater)
                IsLockedOutOfRift = false;
            
            CurrentRift = currentRift;
            IsInRift = isInRift;
            CurrentWorldId = currentWorldId;
            IsStarted = isStarted;
            Type = type;
            CurrentDepth = currentDepth;
            IsGreaterRiftStarted = isStarted && type == RiftType.Greater;

            IsGreaterRiftProfile = Player.CurrentMessage != null && 
                Player.CurrentMessage.ProfileTagName != null && 
                Player.CurrentMessage.ProfileTagName.ToLower().Contains("greater");
        }

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

    //public class RiftMap
    //{
    //    static RiftMap()
    //    {
    //        Pulsator.OnPulse += (sender, args) => UpdateRiftMap();
    //    }

    //    private static DateTime _lastUpdated = DateTime.MinValue;
    //    private static RiftInfo _rift;
    //    private static bool _isParticipatingInRift;
    //    public static List<RiftLevel> Levels = new List<RiftLevel>();

    //    public class RiftLevel
    //    {
    //        public List<MapPoint> Points = new List<MapPoint>();
    //        public Vector3 EntrancePosition { get; set; }
    //        public Vector3 ExitPosition { get; set; }
    //        public MapPoint StartPoint { get; set; }
    //        public int Depth { get; set; }
    //        public int WorldSnoId { get; set; }
    //        public bool HasGaurdian { get; set; }

    //        public List<Vector3> GetPathToExit()
    //        {
    //            var path = new List<Vector3>();

    //            if (!Points.Any())
    //                return path;

    //            Log.Warn("Pathfinder analysing {0} points", Points.Count);

    //            var endPosition = ExitPosition;
    //            if (endPosition == Vector3.Zero)
    //            {
    //                var firstOrDefault = Points.OrderByDescending(p => p.DistanceToStart).FirstOrDefault();
    //                if (firstOrDefault != null)
    //                    endPosition = firstOrDefault.Position;
    //            }

    //            var usedPoints = new HashSet<MapPoint>();

    //            foreach (var point in Points)
    //            {
    //                var candidates = Points.Where(p => p.Position.Distance2D(point.Position) < 100f && !usedPoints.Contains(p));
    //                MapPoint bestCandidate = null;

    //                foreach (var candidate in candidates)
    //                {
    //                    candidate.DistanceToDirectLine = MathUtil.LineSegment.DistToSegment(point.Position, StartPoint.Position, endPosition);
    //                    candidate.Weight = point.DistanceToStart - point.DistanceToDirectLine;

    //                    if (bestCandidate == null || candidate.Weight > bestCandidate.Weight)
    //                        bestCandidate = candidate;
    //                }

    //                if (bestCandidate == null)
    //                {
    //                    Log.Warn("Pathfinding failed");
    //                    break;
    //                }

    //                usedPoints.Add(bestCandidate);
    //                path.Add(bestCandidate.Position);
    //            }

    //            Log.Warn("Pathfinder returned path with {0} points", path.Count);
    //            return path;
    //        }
    //    }

    //    public class MapPoint
    //    {
    //        public MapPoint(Vector3 position)
    //        {
    //            Position = position;
    //        }

    //        public Vector3 Position { get; }
    //        public MapPoint Previous { get; set; }
    //        public MapPoint Next { get; set; }
    //        public float DistanceToStart { get; set; }
    //        public double DistanceToDirectLine { get; set; }
    //        public double Weight { get; set; }

    //        public override int GetHashCode()
    //        {
    //            return Position.GetHashCode();
    //        }
    //    }

    //    private static void UpdateRiftMap()
    //    {
    //        if (DateTime.UtcNow.Subtract(_lastUpdated).TotalSeconds < 2)
    //            return;

    //        _lastUpdated = DateTime.UtcNow;
    //        _rift = ZetaDia.CurrentRift;

    //        if (_rift == null)
    //        {
    //            Reset();
    //            return;
    //        }

    //        if (_isParticipatingInRift && _rift.IsCompleted)
    //        {
    //            Log.Info("Rift Completed");
    //            Reset();
    //            return;
    //        }

    //        if (!_isParticipatingInRift && _rift.IsStarted)
    //        {
    //            Log.Info("Rift Started");
    //            _isParticipatingInRift = true;
    //        }

    //        var currentWorldId = ZetaDia.CurrentWorldSnoId;
    //        var currentLevel = Levels.FirstOrDefault(l => l.WorldSnoId == currentWorldId);
    //        var myPosition = ZetaDia.Me.Position;

    //        if (!IsInRift)
    //            return;

    //        // Record new levels within rift.
    //        if (currentLevel == null)
    //        {
    //            currentLevel = new RiftLevel
    //            {
    //                Depth = GetDepthByWorldId(currentWorldId),
    //                WorldSnoId = currentWorldId
    //            };
    //            Levels.Add(currentLevel);
    //        }

    //        // Record nearby entrance portals
    //        if (currentLevel.EntrancePosition == Vector3.Zero)
    //        {
    //            var entranceMarker = ZetaDia.Minimap.Markers.CurrentWorldMarkers.FirstOrDefault(m => m.IsPortalEntrance);
    //            if (entranceMarker != null)
    //                currentLevel.EntrancePosition = entranceMarker.Position;
    //        }

    //        // Record nearby exit portals
    //        if (currentLevel.ExitPosition == Vector3.Zero)
    //        {
    //            var exitMarker = ZetaDia.Minimap.Markers.CurrentWorldMarkers.FirstOrDefault(m => m.IsPortalExit);
    //            if (exitMarker != null)
    //                currentLevel.EntrancePosition = exitMarker.Position;
    //        }

    //        if (!currentLevel.HasGaurdian && _rift.HasGuardianSpawned && IsRiftGaurdianNearby)
    //            currentLevel.HasGaurdian = true;

    //        if (currentLevel.Points.Any(p => p.Position.Distance2D(myPosition) < 80f))
    //            return;

    //        var point = new MapPoint(myPosition)
    //        {
    //            DistanceToStart = currentLevel.StartPoint.Position.Distance2D(myPosition),
    //        };

    //        if (!currentLevel.Points.Any())
    //            currentLevel.StartPoint = point;

    //        currentLevel.Points.Add(point);
    //    }


    //    private static bool IsRiftGaurdianNearby
    //    {
    //        get { return ZetaDia.Actors.GetActorsOfType<DiaUnit>().Any(a => a.CommonData.MonsterQualityLevel == MonsterQuality.Boss); }
    //    }

    //    public static bool IsInRift
    //    {
    //        get { return RiftWorldIds.Contains(ZetaDia.CurrentLevelAreaSnoId); }
    //    }

    //    private static void Reset()
    //    {
    //        Levels.Clear();
    //    }

    //    public static void AddPosition(BotHistory.PositionCache position)
    //    {

    //    }

    //    private static int GetDepthByWorldId(int worldId)
    //    {
    //        switch (worldId)
    //        {
    //            case 288454: return 1;
    //            case 288685: return 2;
    //            case 288687: return 3;
    //            case 288798: return 4;
    //            case 288800: return 5;
    //            case 288802: return 6;
    //            case 288804: return 7;
    //            case 288806: return 8;
    //        }
    //        return -1;
    //    }

    //    public static readonly HashSet<int> RiftWorldIds = new HashSet<int>
    //{
    //    288454,
    //    288685,
    //    288687,
    //    288798,
    //    288800,
    //    288802,
    //    288804,
    //    288806,
    //};
    //}
}




