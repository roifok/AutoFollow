using System;
using System.Collections.Generic;
using System.Linq;
using AutoFollow.Networking;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;

namespace AutoFollow.Resources
{
    public class Data
    {
        public static List<DiaGizmo> Gizmos
        {
            get { return ZetaDia.Actors.GetActorsOfType<DiaGizmo>(true).Where(g => IsValid(g)).ToList(); }
        }

        public static List<DiaGizmo> Portals
        {
            get { return Gizmos.Where(g => g is GizmoPortal || g.ActorSnoId == Reference.DeathGateId).ToList(); }
        }

        public static List<DiaUnit> Monsters
        {
            get { return ZetaDia.Actors.GetActorsOfType<DiaUnit>(true).Where(u => IsValid(u) && u.IsHostile && !u.IsDead).ToList(); }
        }

        public static List<DiaObject> Actors
        {
            get { return ZetaDia.Actors.GetActorsOfType<DiaObject>(true).Where(IsValid).ToList(); }
        }

        public static List<MinimapMarker> Markers
        {
            get { return ZetaDia.Minimap.Markers.CurrentWorldMarkers.ToList(); }
        }

        public static List<DiaPlayer> Players
        {
            get { return ZetaDia.Actors.GetActorsOfType<DiaPlayer>(true).Where(o => IsValid(o)).ToList(); }
        }

        public static List<MinimapMarker> NearbyMarkers
        {
            get { return ZetaDia.Minimap.Markers.CurrentWorldMarkers.Where(m => m.Position.Distance(Player.Position) < 100f).ToList(); }
        }

        public static List<DiaObject> NavigationObstacles
        {
            get
            {
                var obstacles = new List<DiaObject>();
                obstacles.AddRange(Gizmos);
                obstacles.AddRange(Monsters);
                return obstacles;
            }
        }

        public static Func<DiaObject, bool> IsValid = o => o != null && o.IsValid && o.CommonData != null && o.CommonData.IsValid && !o.CommonData.IsDisposed;

        public static int GetAcdIdByHeroId(int heroId)
        {
            // AcdId is a local client object id that changes between d3 instances.
            var player = ZetaDia.Players.FirstOrDefault(p => p.HeroId == heroId);
            return player != null ? player.ACDId : -1;
        }

        public static DiaPlayer GetPlayerActor(Message player)
        {
            var acdId = GetAcdIdByHeroId(player.HeroId);
            return acdId > 0 ? Players.FirstOrDefault(p => p.ACDId == acdId) : null;
        }

        public static DiaGizmo FindNearestDeathGate()
        {
            //328830
            if (Reference.FortressLevelAreaIds.Contains(Player.CurrentLevelAreaId) || Reference.FortressWorldIds.Contains(Player.CurrentWorldSnoId))
            {
                var gizmo = ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                        .Where(u => u.IsValid && u.ActorSnoId == 328830 && u.Distance < 200)
                        .OrderBy(u => u.Distance)
                        .FirstOrDefault();

                return gizmo != null ? gizmo as DiaGizmo : null;
            }
            return null;
        }

        public class Reference
        {
            public static int DeathGateId = 328830;

            public static HashSet<int> FortressLevelAreaIds = new HashSet<int>
            {
                370512,
                366169,
                360494,
                349787,
                340533,
                276361,
                271271,
                271234,
                333758,

            };

            public static HashSet<int> FortressWorldIds = new HashSet<int>
            {
                271233,
                271235,
            };
        }
    }
}

