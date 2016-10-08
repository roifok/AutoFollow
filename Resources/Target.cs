using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Trinity;
using Trinity.Components.Combat.Resources;
using Trinity.Framework;
using Trinity.Framework.Actors.ActorTypes;
using Trinity.Framework.Actors.Properties;
using Trinity.Framework.Objects;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace AutoFollow.Resources
{
    [DataContract]
    public class Target : ITargetable
    {
        public Target()
        {
        }

        public Target(DiaObject actor)
        {
            if (!Data.IsValid(actor))
                return;

            ActorSnoId = actor.ActorSnoId;
            AcdId = actor.ACDId;
            Name = actor.Name;
            Type = CommonProperties.GetObjectType(actor.ActorType, ActorSnoId, actor.ActorInfo.GizmoType, Name);
            WorldSnoId = Player.CurrentWorldSnoId;
            WorldDynamicId = actor.WorldId;

            var quality = actor.CommonData.MonsterQualityLevel;
            if (!Enum.IsDefined(typeof(MonsterQuality), quality) || (int) quality == -1)
                quality = MonsterQuality.Normal;

            Quality = quality;
            Position = actor.Position;
        }

        [DataMember]
        public int ActorSnoId { get; set; }
        [DataMember]
        public int AcdId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public TrinityObjectType Type { get; set; }
        [DataMember]
        public MonsterQuality Quality { get; set; }
        [DataMember]
        public Vector3 Position { get; set; }
        [DataMember]
        public int WorldSnoId { get; set; }
        [DataMember]
        public int WorldDynamicId { get; set; }

        [IgnoreDataMember]
        public float Distance => Player.Position.Distance(Position);

        public override string ToString() => $"( Id: {ActorSnoId} AcdId: {AcdId} Name: {Name} Quality: {Quality} Position: {Position} )";
    }
}
