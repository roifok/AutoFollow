﻿using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace AutoFollow.Resources
{
    public static class Town
    {        
        public static class ActorIds
        {
            public const int Stash = 130400;
            public const int TheQuaterMaster = 309718;
            public const int ThePeddler = 180783;
            public const int TheMiner = 178396;
            public const int Tyrael = 114622;
            public const int BookOfCain = 297813;
            public const int Jeweler = 56949;
            public const int BlackSmith = 56947;
            public const int Kadala = 361241;
            public const int KanaisCube = 439975;
            public const int RiftObelisk = 364715;
            public const int TheCollector = 178362;
            public const int TheMystic = 212511;
            public const int TheFence = 178388;
            public const int Orek = 363744;
        }

        public static class Actors
        {
            public static IEnumerable<DiaUnit> Units 
                => ZetaDia.Actors.GetActorsOfType<DiaUnit>(true);

            public static IEnumerable<DiaUnit> Gizmos
                => ZetaDia.Actors.GetActorsOfType<DiaUnit>(true);

            public static DiaObject Stash 
                => Gizmos.FirstOrDefault(o => o.ActorSnoId == ActorIds.Stash);

            public static DiaObject TheQuaterMaster 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.TheQuaterMaster);

            public static DiaObject ThePeddler 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.ThePeddler);

            public static DiaObject TheMiner 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.TheMiner);

            public static DiaObject Tyrael 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.Tyrael);

            public static DiaObject BookOfCain 
                => Gizmos.FirstOrDefault(o => o.ActorSnoId == ActorIds.BookOfCain);

            public static DiaObject Jeweler 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.Jeweler);

            public static DiaObject BlackSmith 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.BlackSmith);

            public static DiaObject Kadala 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.Kadala);

            public static DiaObject KanaisCube 
                => Gizmos.FirstOrDefault(o => o.ActorSnoId == ActorIds.KanaisCube);

            public static DiaObject RiftObelisk 
                => Gizmos.FirstOrDefault(o => o.ActorSnoId == ActorIds.RiftObelisk);

            public static DiaObject TheCollector 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.TheCollector);

            public static DiaObject TheMystic 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.TheMystic);

            public static DiaObject TheFence 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.TheFence);

            public static DiaObject Orek 
                => Units.FirstOrDefault(o => o.ActorSnoId == ActorIds.Orek);
        }

        public static class LevelAreaIds
        {
            public const int A1CampaignTown = 19947;
            public const int A1OpenWorldTown = 332339;
            public const int A2Town = 168314;
            public const int A3A4Town = 92945;
            public const int A5Town = 270011;
        }

        public static class Locations
        {            
            public static Vector3 Stash
            {
                get
                {
                    switch (ZetaDia.CurrentLevelAreaSnoId)
                    {
                        case LevelAreaIds.A1CampaignTown: return new Vector3(2968.16f, 2789.63f, 23.94531f);
                        case LevelAreaIds.A1OpenWorldTown: return new Vector3(388.16f, 509.63f, 23.94531f);
                        case LevelAreaIds.A2Town: return new Vector3(323.0558f, 222.7048f, 0f);
                        case LevelAreaIds.A3A4Town: return new Vector3(387.6834f, 382.0295f, 0f);
                        case LevelAreaIds.A5Town: return new Vector3(502.8296f, 739.7472f, 2.598635f);
                        default: throw new ValueUnavailableException("Unknown LevelAreaId");
                    }
                }
            }

            public static Vector3 KanaisCube
            {
                get
                {
                    switch (ZetaDia.CurrentLevelAreaSnoId)
                    {
                        case LevelAreaIds.A1CampaignTown: return Vector3.Zero;
                        case LevelAreaIds.A1OpenWorldTown: return new Vector3(419.0077f, 581.7107f, 24.04533f);
                        case LevelAreaIds.A2Town: return new Vector3(290.3684f, 323.3933f, 0.1000038f);
                        case LevelAreaIds.A3A4Town:  return new Vector3(427.6537f, 493.0599f, 0.4094009f);
                        case LevelAreaIds.A5Town:  return new Vector3(574.3948f, 807.548f, 2.620763f);
                        default: throw new ValueUnavailableException("Unknown LevelAreaId");
                    }
                }
            }

            public static Vector3 ClosestMerchantToCube
            {
                get
                {
                    var levelAreaId = ZetaDia.CurrentLevelAreaSnoId;
                    switch (levelAreaId)
                    {
                        case 19947: // Campaign A1 Hub
                            return Vector3.Zero;
                        case 332339: // OpenWorld A1 Hub
                            return new Vector3(430.3188f, 577.7206f, 24.04533f);
                        case 168314: // A2 Hub
                            return new Vector3(291.7876f, 278.9904f, 0.1000038f);
                        case 92945: // A3/A4 Hub
                            return new Vector3(392.6478f, 513.2823f, 0.1f);
                        case 270011: // A5 Hub
                            return new Vector3(624.8824f, 819.2529f, 2.620764f);
                        default:
                            throw new ValueUnavailableException("Unknown LevelArea Id " + levelAreaId);
                    }
                }
            }

            public static Vector3 TheCollector
            {
                get
                {
                    var levelAreaId = ZetaDia.CurrentLevelAreaSnoId;
                    switch (levelAreaId)
                    {
                        case 19947: // Campaign A1 Hub
                            return Vector3.Zero;
                        case 332339: // OpenWorld A1 Hub
                            return new Vector3(430.3188f, 577.7206f, 24.04533f);
                        case 168314: // A2 Hub
                            return new Vector3(359.6478f, 133.995f, -16.39508f);
                        case 92945: // A3/A4 Hub
                            return new Vector3(444.4589f, 321.4779f, 0.1000005f);
                        case 270011: // A5 Hub
                            return new Vector3(625.013f, 815.3127f, 2.620764f);
                        default:
                            throw new ValueUnavailableException("Unknown LevelArea Id " + levelAreaId);
                    }
                }
            }

            public static Vector3 TheFence
            {
                get
                {
                    var levelAreaId = ZetaDia.CurrentLevelAreaSnoId;
                    switch (levelAreaId)
                    {
                        case 19947: // Campaign A1 Hub
                            return Vector3.Zero;
                        case 332339: // OpenWorld A1 Hub
                            return new Vector3(331.6506f, 301.1718f, 0.599789f);
                        case 168314: // A2 Hub
                            return new Vector3(337.4676f, 135.7881f, -16.39509f);
                        case 92945: // A3/A4 Hub
                            return new Vector3(444.4589f, 321.4779f, 0.1000005f);
                        case 270011: // A5 Hub
                            return new Vector3(621.2771f, 797.3148f, 2.620764f);
                        default:
                            throw new ValueUnavailableException("Unknown LevelArea Id " + levelAreaId);
                    }
                }
            }

            public static Vector3 TheMiner
            {
                get
                {
                    var levelAreaId = ZetaDia.CurrentLevelAreaSnoId;
                    switch (levelAreaId)
                    {
                        case 19947: // Campaign A1 Hub
                            return Vector3.Zero;
                        case 332339: // OpenWorld A1 Hub
                            return new Vector3(430.3188f, 577.7206f, 24.04533f);
                        case 168314: // A2 Hub
                            return new Vector3(348.0609f, 153.5159f, -16.39508f);
                        case 92945: // A3/A4 Hub
                            return new Vector3(441.0552f, 513.9581f, 0.1f);
                        case 270011: // A5 Hub
                            return new Vector3(425.7704f, 820.938f, 7.421445f);
                        default:
                            throw new ValueUnavailableException("Unknown LevelArea Id " + levelAreaId);
                    }
                }
            }

            public static Vector3 TheQuarterMaster
            {
                get
                {
                    var levelAreaId = ZetaDia.CurrentLevelAreaSnoId;
                    switch (levelAreaId)
                    {
                        case 270011: // A5 Hub
                            return new Vector3(545.3922f, 775.2578f, 2.782243f);
                        default:
                            throw new ValueUnavailableException($"There is no QuarterMaster merchant for this act ({levelAreaId})");
                    }
                }
            }

            public static Vector3 ThePeddler
            {
                get
                {
                    switch (ZetaDia.CurrentLevelAreaSnoId)
                    {
                        case LevelAreaIds.A2Town: return new Vector3(291.3571f, 276.9437f, 0.1000038f);
                        default: throw new ValueUnavailableException($"There is no QuarterMaster merchant for this act");
                    }
                }
            }

            public static Vector3 GetLocationFromActorId(int vendorId)
            {
                switch (vendorId)
                {
                    case ActorIds.Stash: return Stash;
                    case ActorIds.TheFence: return TheFence;
                    case ActorIds.TheMiner: return TheMiner;
                    case ActorIds.ThePeddler: return ThePeddler;
                    case ActorIds.TheQuaterMaster: return TheQuarterMaster;
                    default: throw new ValueUnavailableException($"There is no position available for this actorId ({vendorId})");
                }
            }

        }
    }
}
