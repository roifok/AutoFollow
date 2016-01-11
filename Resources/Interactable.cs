using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Common;
using Zeta.Game;
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
            ActorSNO = obj.ActorSNO;
            TimeFirstSeen = DateTime.UtcNow;
            LastSeenTime = DateTime.UtcNow;
            GizmoType = obj.CommonData.GizmoType;
            WorldId = ZetaDia.CurrentWorldId;

            var marker = ZetaDia.Minimap.Markers.AllMarkers.FirstOrDefault(m => m.Position.Distance(ActorPosition) < 15f);
            if (marker != null)
                MarkerHash = marker.NameHash;
        }

        public string InternalName { get; set; }
        public Vector3 ActorPosition { get; set; }
        public GizmoType GizmoType { get; set; }
        public DateTime TimeFirstSeen { get; set; }
        public int ActorSNO { get; set; }
        public int MarkerHash { get; set; }
        public int WorldId { get; set; }
        public DateTime LastSeenTime { get; set; }

        public override string ToString()
        {
            return string.Format("Portal Record: {0} ({1}) Position={2} WorldId={3} MsSinceFirstSeen={4}",
                InternalName, ActorSNO, ActorPosition, WorldId,
                DateTime.UtcNow.Subtract(TimeFirstSeen).TotalMilliseconds);
        }

    }


}
