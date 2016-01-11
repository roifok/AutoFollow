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
        public override bool IsDone
        {
            get { return _isDone || !IsActiveQuestStep; }
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => AutoFollowTagTask());
        }

        public override void OnStart()
        {
            if (!string.IsNullOrEmpty(behaviorName))
            {
                IBehavior behavior;
                if (AutoFollow.Behaviors.Items.TryGetValue(behaviorName, out behavior))
                {
                    Log.Info("AutoFollowTag started with mode: {0}", behaviorName);
                    CurrentBehavior = behavior;
                }
            }
            else
            {
                Log.Info("Requested behavior '{0}' was not found", behaviorName);
                CurrentBehavior = AutoFollow.DefaultBehavior;
                BotMain.Stop();
            }

            base.OnStart();
        }

        private async Task<bool> AutoFollowTagTask()
        {
            //if (!ZetaDia.IsInGame)
            //    return false;

            //if (ZetaDia.IsLoadingWorld)
            //    return false;

            //await Coroutine.Sleep(1000);
            return false;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}

