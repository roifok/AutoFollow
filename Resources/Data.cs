using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<GizmoPortal> Portals
        {
            get { return Gizmos.Where(g => g is GizmoPortal && IsValid(g)).Select(g => (GizmoPortal)g).ToList(); }
        }

        public static List<DiaUnit> Monsters
        {
            get { return ZetaDia.Actors.GetActorsOfType<DiaUnit>(true).Where(u => IsValid(u) && u.IsHostile).ToList(); }
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
            get { return ZetaDia.Minimap.Markers.CurrentWorldMarkers.Where(m => m.Position.Distance(Player.Instance.Position) < 100f).ToList(); }
        }

        public static Func<DiaObject, bool> IsValid = o => o != null && o.IsValid && o.CommonData != null && o.CommonData.IsValid && !o.CommonData.IsDisposed;
    }
}