using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Org.BouncyCastle.Utilities.Collections;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;

namespace AutoFollow.Resources
{
    public class BotHistory
    {
        public static void Enable()
        {
            Pulsator.OnPulse += Pulsator_OnPulse;           
        }

        public static void Disable()
        {
            Pulsator.OnPulse -= Pulsator_OnPulse;
        }

        private static void Pulsator_OnPulse(object sender, EventArgs e)
        {
            Update();
        }

        public static void Update()
        {
            PositionCache.AddPosition();
            TrackPortals();
        }

        public static Dictionary<Vector3, Interactable> PortalHistory = new Dictionary<Vector3, Interactable>();

        /// <summary>
        /// Record information about portals whenever they are near.
        /// </summary>
        public static void TrackPortals()
        {
            var portals = Data.Portals.Where(p => p.Distance <= 16f).ToList();

            foreach (var p in portals)
            {
                if (PortalHistory.ContainsKey(p.Position))
                {
                    Log.Verbose("Updating Last Seen Time for Portal - {0} ({1})", p.Name, p.ActorSnoId);
                    PortalHistory[p.Position].LastSeenTime = DateTime.UtcNow;
                }                    
            }

            var nearestPortal = portals.OrderBy(p => p.Distance).FirstOrDefault();
            if (nearestPortal != null && PortalHistory.All(p => p.Value.ActorPosition != nearestPortal.Position))
            {
                Log.Verbose("Adding new Portal to History - {0} ({1})", nearestPortal.Name, nearestPortal.ActorSnoId);
                PortalHistory.Add(nearestPortal.Position, new Interactable(nearestPortal));
            }

            if (PortalHistory.Count > 25)
                PortalHistory.Remove(PortalHistory.First().Key);
        }

        /// <summary>
        /// Track everywhere the player has been.
        /// </summary>
        public class PositionCache : IEquatable<PositionCache>
        {
            private const int CacheLimit = 150;
            private const int RecentPositionsLimit = 75;

            public Vector3 Position { get; set; }
            public DateTime RecordedAt { get; set; }
            public int WorldId { get; set; }

            public static DateTime LastRecordedTime { get; set; }
            public static Vector3 LastRecordedPosition { get; set; }

            public static List<PositionCache> Cache = new List<PositionCache>();
            public static List<Vector3> RecentPositions = new List<Vector3>();

            static PositionCache()
            {
                Pulsator.OnPulse += (sender, args) => AddPosition();
                GameEvents.OnWorldChanged += (sender, args) => RecentPositions.Clear();
            }

            public PositionCache()
            {
                if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && Data.IsValid(ZetaDia.Me))
                {
                    Position = ZetaDia.Me.Position;
                    RecordedAt = DateTime.UtcNow;
                    WorldId = ZetaDia.CurrentWorldSnoId;
                }
            }

            public static void AddPosition(float distance = 5f)
            {   
                MaintainCache();

                if (Cache.Any(p => DateTime.UtcNow.Subtract(p.RecordedAt).TotalMilliseconds < 250))
                    return;

                if (Cache.Any(p => p.Position.Distance2D(ZetaDia.Me.Position) < distance))
                    return;

                if (ZetaDia.Me.Position == Vector3.Zero)
                    return;

                RecentPositions.Add(ZetaDia.Me.Position);
                Cache.Add(new PositionCache());              
            }

            public static void MaintainCache()
            {
                if (RecentPositions.Count > RecentPositionsLimit)
                    RecentPositions.RemoveAt(0);

                if (Cache.Count > CacheLimit)
                    Cache.RemoveAt(0);
            }

            private static CacheField<Vector3> _centroid = new CacheField<Vector3>(100);
            public static Vector3 Centroid
            {
                get { return _centroid.GetValue(() => MathUtil.Centroid(RecentPositions)); }
            }

            public bool Equals(PositionCache other)
            {
                return Position == other.Position && WorldId == other.WorldId;
            }

            public static Vector3 GetLastPositionInWorld(int worldId = -1)
            {
                PositionCache lastCachedPosition;

                if (worldId > 0)
                {                    
                    lastCachedPosition = Cache.Where(p => p.WorldId == worldId).OrderBy(p => p.RecordedAt).LastOrDefault();
                }
                else
                {
                    lastCachedPosition = Cache.Where(p => p.WorldId != ZetaDia.CurrentWorldSnoId).OrderBy(p => p.RecordedAt).LastOrDefault();
                }
                
                return lastCachedPosition != null ? lastCachedPosition.Position : default(Vector3);
            }
        }
    }

}
