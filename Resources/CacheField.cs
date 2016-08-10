using System;
using Zeta.Game;

namespace AutoFollow.Resources
{
    /// <summary>
    /// Information about a field and the tools to refresh it.
    /// </summary>
    public class CacheField<T>
    {
        #region Fields

        private T _cachedValue;
        private readonly Type _type;

        #endregion

        #region Constructors

        /// <summary>
        /// Create cache field; default is to always return cache value after the first refresh.
        /// </summary>
        public CacheField(int delay = -1)
        {
            Delay = delay;
            _type = typeof(T);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Cached value
        /// </summary>
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

        public uint LastUpdatedFrame { get; set; }

        /// <summary>
        /// If cache value has ever been set;
        /// </summary>
        public bool IsValueCreated { get; set; }

        /// <summary>
        /// Indicates if a new value should be requested
        /// </summary>
        public bool IsCacheValid
        {
            get
            {
                // Always use cache if SetValueOverride() was used.
                if (IsValueOverride)
                    return true;

                // Always update the cache if we dont have a value yet
                if (!IsValueCreated)
                    return false;

                // Always use cache value if default -1 delay
                if (Delay == -1)
                    return true;

                // Always use cache for requests on the same frame
                if (LastUpdatedFrame == ZetaDia.Memory.Executor.FrameCount)
                    return true;

                // Always update if set to realtime
                if (Delay == 0)
                    return false;

                // Use cache value if delay hasn't passed yet.
                if (DateTime.UtcNow.Subtract(LastUpdate).TotalMilliseconds >= Delay)
                    return false;

                // Use cache value
                return true;
            }
        }

        /// <summary>
        /// The amount of time (in Milliseconds) allowed to pass before refreshing
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// Time of the last refresh
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// If true, cache value will always be returned.
        /// </summary>
        public bool IsValueOverride { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Short cut to get value.
        /// </summary>
        /// <param name="retriever"> Cached value retriver. </param>
        /// <returns> The cached value. </returns>
        public T GetValue(Func<T> retriever)
        {
            if (IsCacheValid)
                return CachedValue;

            return retriever();
        }

        /// <summary>
        /// Set a permanent value, which is always returned.
        /// Intended for manually creating objects that are not linked to DB actors.
        /// </summary>
        internal void SetValueOverride(T value)
        {
            IsValueOverride = true;
            IsValueCreated = true;
            CachedValue = value;
        }

        /// <summary>
        /// Reset everything except delay property.
        /// </summary>
        public void Clear()
        {
            IsValueOverride = false;
            IsValueCreated = false;
            CachedValue = default(T);
        }

        /// <summary>
        /// Time since last update in Milliseconds
        /// </summary>
        public double Age => DateTime.UtcNow.Subtract(LastUpdate).TotalMilliseconds;

        internal void Invalidate()
        {
            Clear();
        }

        #endregion
    }
}
