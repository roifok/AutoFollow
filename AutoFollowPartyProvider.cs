using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Components.Combat;
using Trinity.Components.Combat.Resources;
using Trinity.Framework;
using Trinity.Framework.Actors.ActorTypes;
using Zeta.Common;

namespace AutoFollow
{
    public class AutoFollowPartyProvider : IPartyProvider
    {
        public IEnumerable<IPartyMember> Members => AutoFollow.CurrentParty;
        public IEnumerable<IPartyMember> Followers => AutoFollow.CurrentFollowers;
        public IPartyMember Leader => AutoFollow.CurrentLeader;
        public ITargetable PriorityTarget => Leader.Target;
        public Vector3 FightLocation => Leader.Target.Position;
        public Vector3 SafeLocation => Leader.Position;
        public override string ToString() => $"{GetType().Name}: Members={Members.Count()}";
    }
}
