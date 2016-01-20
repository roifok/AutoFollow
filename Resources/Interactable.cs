using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.SNO;

namespace AutoFollow.Resources
{
    [Serializable]
    public class Interactable
    {
        public Interactable(DiaObject obj)
        {
            InternalName = obj.Name;
            ActorPosition = obj.Position;
            ActorSnoId = obj.ActorSnoId;
            AcdId = obj.ACDId;
            TimeFirstSeen = DateTime.UtcNow;
            LastSeenTime = DateTime.UtcNow;
            GizmoType = obj.CommonData.GizmoType;
            WorldSnoId = ZetaDia.CurrentWorldSnoId;

            var marker = ZetaDia.Minimap.Markers.AllMarkers.FirstOrDefault(m => m.Position.Distance(ActorPosition) < 15f);
            if (marker != null)
                MarkerHash = marker.NameHash;
        }

        public int AcdId { get; set; }
        public string InternalName { get; set; }
        public Vector3 ActorPosition { get; set; }
        public GizmoType GizmoType { get; set; }
        public DateTime TimeFirstSeen { get; set; }
        public int ActorSnoId { get; set; }
        public int MarkerHash { get; set; }
        public int WorldSnoId { get; set; }
        public DateTime LastSeenTime { get; set; }

        //public Interactable ExitPortal { get; set; }
        //public Interactable EntryPortal { get; set; }

        public bool IsWorldEntryPoint
        {
            get { return GizmoType == GizmoType.Portal && Marker != null && Marker.IsPortalEntrance; }
        }

        public bool IsWorldExitPoint
        {
            get { return GizmoType == GizmoType.Portal && Marker != null && Marker.IsPortalExit; }
        }

        [IgnoreDataMember]
        public MinimapMarker Marker
        {
            get { return Data.Markers.FirstOrDefault(m => m.Position.Distance(ActorPosition) < 5f); }
        }

        public override string ToString()
        {
            return string.Format("Portal Record: {0} ({1}) Position={2} WorldSnoId={3} MsSinceFirstSeen={4}",
                InternalName, ActorSnoId, ActorPosition, WorldSnoId,
                DateTime.UtcNow.Subtract(TimeFirstSeen).TotalMilliseconds);
        }

    }


}
