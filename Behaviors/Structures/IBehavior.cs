namespace AutoFollow.Behaviors.Structures
{
    public interface IBehavior
    {
        void Activate();
        void Deactivate();
        BehaviorCategory Category { get; }
        BehaviorType Type { get; }
        string Name { get; }
    }
}
