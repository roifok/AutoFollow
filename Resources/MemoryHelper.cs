using System;
using GreyMagic;
using Zeta.Bot;
using Zeta.Game;

namespace AutoFollow.Resources
{
    public static class MemoryHelperState
    {
        public static bool InFramelock
        {
            get { return MemoryHelperLevel > 0; }
        }

        public static uint CurrentFrame
        {
            get { return ZetaDia.Memory.Executor.FrameCount; }
        }

        public static uint LastUpdatedActorsFrame { get; set; }

        public static bool UpdatedActorsThisFrame
        {
            get { return CurrentFrame == LastUpdatedActorsFrame; }
        }

        public static int MemoryHelperLevel { get; set; }
    }

    public class MemoryHelper : IDisposable
    {
        private bool _disposed;
        private ExternalReadCache _externalReadCache;
        private FrameLockRelease _frameLockRelease;

        public MemoryHelper()
        {
            if (MemoryHelperState.InFramelock || BotMain.IsRunning)
                return;

            _frameLockRelease = ZetaDia.Memory.ReleaseFrame();
            MemoryHelperState.MemoryHelperLevel++;

            if (ZetaDia.Service.IsInGame)
            {
                if (!MemoryHelperState.UpdatedActorsThisFrame)
                {
                    ZetaDia.Actors.Update();
                    MemoryHelperState.LastUpdatedActorsFrame = MemoryHelperState.CurrentFrame;
                }

                _externalReadCache = ZetaDia.Memory.SaveCacheState();
                ZetaDia.Memory.TemporaryCacheState(false);
            }
        }

        public void Dispose()
        {
            MemoryHelperState.MemoryHelperLevel--;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MemoryHelper()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    if (_frameLockRelease != null)
                        _frameLockRelease.Dispose();
                }
                catch (Exception ex)
                {
                }

                try
                {
                    if (_externalReadCache != null)
                        _externalReadCache.Dispose();
                }
                catch (Exception ex)
                {
                }
            }

            _externalReadCache = null;
            _frameLockRelease = null;

            _disposed = true;
        }
    }
}