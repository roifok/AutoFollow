using Trinity.Components.Combat.Resources;

namespace AutoFollow.Behaviors.Structures
{
    public interface IBehavior
    {
        void Activate();
        void Deactivate();
        string Name { get; }
        BehaviorCategory Category { get; }
        PartyObjective Objective { get; }
    }
}
