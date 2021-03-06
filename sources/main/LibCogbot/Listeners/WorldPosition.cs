﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cogbot.World;
using MushDLR223.ScriptEngines;
using OpenMetaverse;
using PathSystem3D.Navigation;

namespace Cogbot
{
    partial class WorldObjects
    {

        private static WorldPathSystem _SimPaths;

        public WorldPathSystem SimPaths
        {
            get { return _SimPaths; }
        }

        internal SimObject GetSimObjectFromVector(Vector3d here, out double dist)
        {
            SimObject retObj = null;
            dist = double.MaxValue;
            if (here == Vector3d.Zero) return retObj;
            bool oddCase = false;
            foreach (SimObject obj in GetAllSimObjects())
            {
                if (!obj.IsRegionAttached)
                {
                    continue;
                    oddCase = true;
                }
                Vector3d at;
                if (obj.TryGetGlobalPosition(out at))
                {
                    if (at == here)
                    {
                        dist = 0;
                        if (oddCase)
                        {
                            return obj;
                        }
                        return obj;
                    }
                    double ld = Vector3d.Distance(at, here);
                    if (ld < dist)
                    {
                        retObj = obj;
                        dist = ld;
                    }
                }
            }
            // odd indeed?!
            if (oddCase && retObj != null && dist < 0.01 && !retObj.IsRegionAttached)
            {
                return retObj;
            }
            return retObj;
        }

        private object AsLocation(Simulator simulator, Vector3 position, SimPosition pos)
        {
            SimRegion r = SimRegion.GetRegion(simulator);
            if (position == Vector3.Zero)
            {
                if (r == null)
                {
                    if (pos != null && pos.IsRegionAttached) return new SimHeading(pos);
                    return SimHeading.UNKNOWN;
                }
                return r;
            }
            Quaternion rot = Quaternion.Identity;
            if (pos != null && pos.IsRegionAttached)
            {
                rot = pos.SimRotation;
            }
            return new SimHeading(r.GetPathStore(position), position, rot);
        }

        private object AsRLocation(Simulator simulator, Vector3d targetPos, SimPosition pos)
        {
            Vector3 lpos = new Vector3((float)targetPos.X, (float)targetPos.Y, (float)targetPos.Z);
            SimRegion r = SimRegion.GetRegion(simulator);
            if (targetPos == Vector3d.Zero)
            {
                if (r == null)
                {
                    if (pos != null && pos.IsRegionAttached) return new SimHeading(pos);
                    return SimHeading.UNKNOWN;
                }
                return r;
            }
            return new SimHeading(pos, lpos);
        }

        private object AsLocation(UUID reg, Vector3 position)
        {
            SimRegion r = GetRegion(reg);
            if (position == Vector3.Zero)
            {
                if (r == null)
                {
                    if (reg == UUID.Zero)
                    {
                        return SimHeading.UNKNOWN;
                    }
                    return SimHeading.UNKNOWN;
                    // return r;
                }
                return r;
            }
            return new SimHeading(r.GetPathStore(position), position, Quaternion.Identity);
        }

        internal SimRegion GetRegion(ulong RegionHandle)
        {
            if (RegionHandle == 0) return null;
            var R =  SimRegion.GetRegion(RegionHandle, client);
            R.RegionMaster = client;
            R.TheWorldSystem = this;
            return R;
        }

        public SimRegion GetRegion(UUID uuid)
        {
            return SimRegion.GetRegion(uuid, client);
        }

        public static bool tryGetCenterPos(List<Primitive> group, out Vector3 centroid)
        {
            centroid = new Vector3();
            if (group.Count < 4)
                return false;
            else
            {
                bool first = true;
                Vector3 min = new Vector3(), max = new Vector3(), pos;
                foreach (Primitive prim in group)
                {
                    if (prim != null && prim.Position != null)
                    {
                        pos = prim.Position;

                        if (first)
                        {
                            min = pos;
                            max = pos;
                            first = false;
                        }
                        else
                        {
                            if (pos.X < min.X)
                                min.X = pos.X;
                            if (pos.Y < min.Y)
                                min.Y = pos.Y;
                            if (pos.Z < min.Z)
                                min.Z = pos.Z;

                            if (pos.X > max.X)
                                max.X = pos.X;
                            if (pos.Y > max.Y)
                                max.Y = pos.Y;
                            if (pos.Z > max.Z)
                                max.Z = pos.Z;
                        }
                    }
                }

                Vector3 size = max - min;
                if (size.X > 3 && size.Y > 3 && size.Z > 3)
                {
                    centroid = min + (size * (float)0.5);
                    return true;
                }
                else
                    return false;
            }
        }

        public int posComp(Vector3 v1, Vector3 v2)
        {
            return (int)(Vector3.Mag(client.Self.RelativePosition - v1) -
                          Vector3.Mag(client.Self.RelativePosition - v2));
        }

        private bool RelTryParse(string s, out float f, float f1)
        {
            char c = s.ToCharArray()[0];
            if (c == '+')
            {
                s = s.Substring(1);
                if (!float.TryParse(s, out f)) return false;
                f = f1 + f;
                return true;
            }
            if (c == '-')
            {
                s = s.Substring(1);
                if (!float.TryParse(s, out f)) return false;
                f = f1 - f;
                return true;
            }
            return float.TryParse(s, out f);
        }


        private SimPosition GetPolarRelative(SimPosition offset, string first, out int argsUsed)
        {
            Vector3 rel = offset.SimPosition;
            int star = first.IndexOf("*");
            if (star < 0)
            {
                argsUsed = 0;
                return offset;
            }

            // -180*5 == *-5
            // -180*5
            // +90*5
            // +180* == -180* == -0*-1 
            // +0*5 == -0*5 == *5 
            // 
            bool relative;
            double rotate;
            double dist = 1.0;
            if (star == 0)
            {
                relative = true;
                rotate = 0.0;
                float f;
                if (RelTryParse(first.Substring(1), out f, 0))
                {
                    dist = f;
                }
            }
            else
            {
                string arg = first.Substring(0, star);
                relative = true;
                if (arg.StartsWith("A"))
                {
                    relative = false;
                    arg = arg.Substring(1);
                }

                float f;
                float offsetZ = (float) (GetZHeading(offset.SimRotation)*180/Math.PI);
                if (!RelTryParse(arg, out f, offsetZ))
                {
                    argsUsed = 0;
                    return offset;
                }
                rotate = f;
                string firstSubstring = first.Substring(star + 1);
                if (firstSubstring.Length == 0)
                {
                    dist = 1.0;
                }
                else if (RelTryParse(firstSubstring, out f, 0))
                {
                    dist = f;
                }
                else
                {
                    argsUsed = 0;
                    return offset;
                }
            }

            Vector3 v3 = Vector3.Transform(Vector3.UnitX, Matrix4.CreateFromQuaternion(offset.SimRotation));

            v3.X = (float) (Math.Sin(rotate*(Math.PI/180))*dist) + rel.X;
            v3.Y = (float) (Math.Cos(rotate*(Math.PI/180))*dist) + rel.Y;
            v3.Z = rel.Z;
            argsUsed = 1;
            return SimWaypointImpl.CreateLocal(v3, offset.PathStore);
        }

        private SimPosition GetSimV(string[] tokens, int start, out int argsUsed, SimPosition offset)
        {
            // not enough args
            if (tokens.Length < start)
            {
                argsUsed = 0;
                return offset;
            }
            if (start > 0)
            {
                tokens = Parser.SplitOff(tokens, start);
                start = 0;
            }
            int maxArgs = 0;
            string first = tokens[0];
            argsUsed = 1;
            if (first == "last")
            {
                return offset;
            }
            if (first.EndsWith("s")) first = first.Substring(0, first.Length - 1);
            if (first == "forward")
            {
                return SimOffsetPosition.WithOrientation(offset, new Vector3(0, 1, 0));
            }
            if (first.EndsWith("ward")) first = first.Substring(0, first.Length - 4);
            if (first == "west")
            {
                return new SimOffsetPosition(offset, new Vector3(-1, 0, 0));
            }
            if (first == "east")
            {
                return new SimOffsetPosition(offset, new Vector3(1, 0, 0));
            }
            if (first == "north")
            {
                return new SimOffsetPosition(offset, new Vector3(0, 1, 0));
            }
            if (first == "south")
            {
                return new SimOffsetPosition(offset, new Vector3(0, -1, 0));
            }
            if (first == "left")
            {
                return SimOffsetPosition.WithOrientation(offset, new Vector3(-1, 0, 0));
            }
            if (first == "right")
            {
                return SimOffsetPosition.WithOrientation(offset, new Vector3(1, 0, 0));
            }
            if (first == "back")
            {
                return SimOffsetPosition.WithOrientation(offset, new Vector3(0, -1, 0));
            }
            if (first.Contains("/"))
            {
                argsUsed = 1;
                int uu;
                return GetSimV(first.Split(new char[] {'/'}), 0, out uu, offset);
            }
            bool relative = false;
            for (int st = start; st < tokens.Length; st++)
            {
                if (maxArgs > 2) break;
                string str = tokens[st];
                double doublke;
                if (double.TryParse(str, out doublke))
                {
                    maxArgs++;
                }
                else
                {
                    break;
                }
            }
            Vector3 rel = offset.SimPosition;
            Vector3 target;

            first = tokens[start];
            // Polar coords 
            if (first.Contains("*")) return GetPolarRelative(offset, first, out argsUsed);

            if (RelTryParse(first, out target.X, rel.X) &&
                RelTryParse(tokens[start + 1], out target.Y, rel.Y))
            {
                argsUsed = 2;
                target.Z = rel.Z;
                if (tokens.Length > start + 1)
                {
                    RelTryParse(tokens[start + 2], out target.Z, rel.Z);
                    argsUsed = 3;
                }
                if (target.X > 512 || target.Y > 512)
                {
                    // Globals
                    return SimWaypointImpl.CreateGlobal(target.X, target.Y, target.Z);
                }
                return SimWaypointImpl.CreateLocal(target, offset.PathStore);
            }
            argsUsed = 0;
            return null;
        }


        /// <summary>
        /// Get a Vector relative to TheSimAvatar
        /// </summary>
        /// <param name="args"></param>
        /// <param name="argsUsed"></param>
        /// <returns></returns>
        public SimPosition GetVector(string[] args, out int argsUsed)
        {
            return GetVector(args, out argsUsed, TheSimAvatar);
        }

        public SimPosition GetVector(string[] args, out int argsUsed, SimPosition offset)
        {
            argsUsed = 0;
            if (args.Length == 0) return offset ?? TheSimAvatar;
            //if (args.Length >= 2)
            {
                SimPosition R = GetSimV(args, 0, out argsUsed, offset);
                if (R != null) return R;
            }
            UUID uuid = UUID.Zero;
            if (UUID.TryParse(args[0], out uuid))
            {
                SimObject O = null;
                O = GetSimObjectFromUUID(uuid);
                if (O != null)
                {
                    argsUsed = 1;
                    return O;
                }
            }

            List<SimObject> prim = GetSingleArg(args, out argsUsed);
            if (prim.Count == 1) return prim[0];


            argsUsed = 0;
            string destination = String.Empty;

            // Handle multi-word sim names by combining the arguments
            foreach (string arg in args)
            {
                destination += arg + " ";
            }
            int consume = args.Length;
            destination = destination.Trim();

            string[] tokens = destination.Split(new char[] { '/' });
            float x, y = 128;
            if (!float.TryParse(tokens[0], out x))
            {
                x = 128;
            }
            else
            {
                argsUsed++;
            }
            string sim = tokens[argsUsed];

            //TODO use the GetSimV code 
            argsUsed += 1;
            float z = -1;
            if (tokens.Length > argsUsed + 1)
            {
                if (!float.TryParse(tokens[argsUsed], out x) ||
                    !float.TryParse(tokens[argsUsed + 1], out y))
                {
                    return null;
                }
                argsUsed += 2;
                if (tokens.Length > argsUsed)
                {
                    if (float.TryParse(tokens[argsUsed], out z))
                    {
                        argsUsed += 1;
                    } else
                    {
                        z = -1;
                    }
                }
                // test for global
                if (x > 512 || y > 512)
                {
                    float.TryParse(tokens[argsUsed], out z);
                    return SimWaypointImpl.CreateGlobal(x, y, z);
                }
            }
            SimRegion region = SimRegion.GetRegion(sim, client);
            if (region == null) return null;
            if (z == -1) z = region.AverageHieght;
            
            Vector3 v3 = new Vector3(x, y, z);
            return SimWaypointImpl.CreateLocal(v3, region.GetPathStore(v3));
        }



        public List<SimObject> GetNearByObjects(Vector3d here, object except, double maxDistance, bool rootOnly)
        {
            if (here.X < 256f)
            {
                throw new ArgumentException("GetNearByObjects is not using GetWorldPostion?");
            }
            List<SimObject> nearby = new List<SimObject>();
            foreach (SimObject obj in GetAllSimObjects())
            {
                if (obj != except)
                {
                    if (rootOnly && !obj.IsRoot) continue;
                    try
                    {
                        Vector3d pos;
                        if (obj.TryGetGlobalPosition(out pos))
                        {
                            if (obj.IsRegionAttached && Vector3d.Distance(pos, here) <= maxDistance)
                                nearby.Add(obj);                            
                        }
                    }
                    catch(Exception) {}
                }
            }
            ;
            return nearby;
        }

        private float distanceHeuristic(SimObject prim)
        {
            if (prim != null)
                return (float)(1.0 / Math.Exp((double)((SimMover)TheSimAvatar).Distance(prim))
                               );
            else
                return (float)0.01;
        }

        /// <summary>
        /// Convert a Quaternion to a Rotation around a Z-axis
        /// 2*PI = North
        /// 1/2*Pi = East
        /// PI = South
        /// 3/2*PI = West
        /// </summary>
        /// <param name="simRotation">Radians</param>
        /// <returns></returns>
        public static double GetZHeading(Quaternion simRotation)
        {
            Vector3 v3 = Vector3.Transform(Vector3.UnitX, Matrix4.CreateFromQuaternion(simRotation));
            return (Math.Atan2(-v3.X, -v3.Y) + Math.PI);
        }
    }
}
