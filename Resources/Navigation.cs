using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;

namespace AutoFollow.Resources
{
    public class Navigation
    {
        #region IsBlocked

        private static readonly Stopwatch BlockedTimer = new Stopwatch();
        private static readonly Stopwatch BlockedCheckTimer = new Stopwatch();     

        private const int TimeToBlockMs = 400;
        private const int TimeToCheckBlockingMs = 50;
        private static bool _isProperBlocked;

        internal static bool GetIsBlocked()
        {
            if (ZetaDia.Me == null || !ZetaDia.Me.IsValid || ZetaDia.Me.IsDead)
                return false;

            if(!BlockedCheckTimer.IsRunning)
                BlockedCheckTimer.Start();

            if (BlockedCheckTimer.ElapsedMilliseconds < TimeToCheckBlockingMs)
                return _isProperBlocked;

            BlockedCheckTimer.Restart();

            if (ZetaDia.Me.Movement != null && ZetaDia.Me.Movement.IsValid && ZetaDia.Me.Movement.SpeedXY > 0.8)
            {
                _isProperBlocked = false;
                BlockedTimer.Stop();
                BlockedTimer.Reset();
                return false;
            }
                
            var testObjects = Data.Monsters.Where(o => o.Distance <= 8f).ToList();
            if (testObjects.Count < 3)
            {
                _isProperBlocked = false;
                BlockedTimer.Stop();
                BlockedTimer.Reset();
                return false;
            }

            if (testObjects.Count > 12)
            {
                _isProperBlocked = true;
                return false;
            }

            var testPoints = MathUtil.GetCirclePoints(10, 10f, ZetaDia.Me.Position).Where(p => CanRayCast(ZetaDia.Me.Position, p)).ToList();

            var blocked = testPoints.All(p => !MainGridProvider.CanStandAt(p) || testObjects.Any(o => MathUtil.PositionIsInCircle(p, o.Position, o.CollisionSphere.Radius / 2)));            

            if (BlockedTimer.IsRunning && blocked && BlockedTimer.ElapsedMilliseconds > TimeToBlockMs)
            {
                Log.Debug("IsBlocked! Timer={0}ms TestObjects={1} TestPoints={2}", BlockedTimer.ElapsedMilliseconds, testObjects.Count, testPoints.Count());
                _isProperBlocked = true;
                return _isProperBlocked;
            }

            if (BlockedTimer.IsRunning && !blocked)
            {
                Log.Debug("No Longer Blocked!");
                BlockedTimer.Stop();
                BlockedTimer.Reset();                
                _isProperBlocked = false;  
                return _isProperBlocked;
            }

            if (blocked)
            {
                if (!BlockedTimer.IsRunning)
                    BlockedTimer.Restart();

                Log.Debug("Probably Blocked - Timer={0}ms TestObjects={1}", BlockedTimer.ElapsedMilliseconds, testObjects.Count());
            }

            return _isProperBlocked;
        }

        public static bool IsBlocked
        {
            get { return _isProperBlocked; }
        }

        public static long BlockedTimeMs
        {
            get { return BlockedTimer.ElapsedMilliseconds; }
        }

        #endregion

        internal static bool CanRayCast(Vector3 vStartLocation, Vector3 vDestination)
        {
            // Navigator.Raycast is REVERSE Of ZetaDia.Physics.Raycast
            // Navigator.Raycast returns True if it "hits" an edge
            // ZetaDia.Physics.Raycast returns False if it "hits" an edge
            // So ZetaDia.Physics.Raycast() == !Navigator.Raycast()
            // We're using Navigator.Raycast now because it's "faster" (per Nesox)

            bool rayCastHit = Navigator.Raycast(vStartLocation, vDestination);

            if (rayCastHit)
                return false;

            return !Data.NavigationObstacles.Any(o => MathEx.IntersectsPath(o.Position, o.CollisionSphere.Radius, vStartLocation, vDestination));          
        }

        internal static MainGridProvider MainGridProvider
        {
            get
            {
                return (MainGridProvider)Navigator.SearchGridProvider;
            }
        }
    }
}
