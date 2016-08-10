using System.ComponentModel;
using System.Threading.Tasks;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace AutoFollow.ProfileTags
{
    [XmlElement("AutoFollow")]
    public class AutoFollowTag : ProfileBehavior
    {
        public static AutoFollowTag Current;

        public AutoFollowTag()
        {
            Current = this;
        }

        [XmlAttribute("behavior")]
        public string behaviorName { get; set; }

        public IBehavior CurrentBehavior { get; set; }

        private bool _isDone;
        public override bool IsDone => _isDone || !IsActiveQuestStep;

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => AutoFollowTagTask());
        }

        public override void OnStart()
        {
            AutoFollow.AssignBehaviorByName(behaviorName);
            base.OnStart();
        }

        private async Task<bool> AutoFollowTagTask()
        {
            return false;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}

