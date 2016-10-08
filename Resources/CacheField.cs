using System;
using Zeta.Game;

namespace AutoFollow.Resources
{
    public class CacheField<T>
    {
        private T _cachedValue;
        public TimeSpan Delay { get; set; }
        public DateTime LastUpdate { get; set; }
        public uint LastUpdatedFrame { get; set; }
        public bool IsValueCreated { get; set; }

        public CacheField(int delayMilliseconds = 0)
        {
            Delay = TimeSpan.FromMilliseconds(delayMilliseconds);
        }

        public T CachedValue
        {
            get { return _cachedValue; }
            set
            {
                if (!IsValueCreated)
                    IsValueCreated = true;

                LastUpdate = DateTime.UtcNow;
                LastUpdatedFrame = ZetaDia.Memory.Executor.FrameCount;
                _cachedValue = value;
            }
        }

        public bool IsCacheValid
        {
            get
            {
                if (!IsValueCreated)
                    return false;

                if (LastUpdatedFrame == ZetaDia.Memory.Executor.FrameCount)
                    return true;

                if (DateTime.UtcNow.Subtract(LastUpdate) >= Delay)
                    return false;

                return true;
            }
        }

        public T GetValue(Func<T> retriever) => IsCacheValid ? CachedValue : retriever();

        public void Clear()
        {
            IsValueCreated = false;
            CachedValue = default(T);
        }
    }
}
