using Fraktalia.Core.Collections;
using Fraktalia.Utility.DataStructures;
using Fraktalia.VoxelGen;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace Fraktalia.VoxelGen.Modify.Procedural
{
    public struct MeshRay
    {
        public float u, v, w;
        public byte hit;
        public float distance;
        public float faceSign;
        public int faceIndex;
    }

    public struct MeshRayTracer
    {

        public int MaxDepth;

        public int LeafNodes;

        public float AvgFacesPerLeaf { get { return Faces.Length / (float)LeafNodes; } }

        [NativeDisableContainerSafetyRestriction]
        private FNativeList<Vector3> Vertices;

        [NativeDisableContainerSafetyRestriction]
        private FNativeList<int> Indices;

        private int NumFaces;

        private int FreeNode;

        private int InnerNodes;

        [NativeDisableContainerSafetyRestriction]
        private FNativeList<AABBNode> Nodes;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<int> Faces;

        [NativeDisableContainerSafetyRestriction]
        private FNativeList<Box3> FaceBounds;

        private int CurrentDepth;

        [NativeDisableContainerSafetyRestriction]
        FNativeList<int> faces;

        public void Initialize(NativeMesh mesh)
        {
            Vertices = new FNativeList<Vector3>(Allocator.Persistent);
            Indices = new FNativeList<int>(Allocator.Persistent);
            mesh.GetVerticeArray(true, Vertices);
            mesh.GetTriangleArray(Indices);

            NumFaces = Indices.Length / 3;

            Nodes = new FNativeList<AABBNode>((int)(NumFaces * 1.5), Allocator.Persistent);
            Faces = new NativeArray<int>(NumFaces, Allocator.Persistent);
            FaceBounds = new FNativeList<Box3>(Allocator.Persistent);


            faces = new FNativeList<int>(Allocator.Persistent);

            MaxDepth = 0;
            InnerNodes = 0;
            LeafNodes = 0;
            CurrentDepth = 0;
            FaceBounds.Clear();

            for (int i = 0; i < NumFaces; i++)
            {
                Box3 top = CalculateFaceBounds(i);

                Faces[i] = i;
                FaceBounds.Add(top);
            }

            CurrentDepth = 0;
            FreeNode = 1;


        }

        public void BuildTree()
        {
            BuildRecursive(0, 0, NumFaces);
        }


        public void CleanUp()
        {
            Vertices.Dispose();
            Indices.Dispose();

            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].CleanUp();
            }

            Nodes.Dispose();
            Faces.Dispose();
            FaceBounds.Dispose();

            faces.Dispose();
        }


        public MeshRay TraceRay(Vector3 start, Vector3 dir)
        {

            MeshRay ray = new MeshRay();
            ray.distance = float.PositiveInfinity;

            TraceRecursive(0, start, dir, ref ray);

            if (ray.distance != float.PositiveInfinity)
            {
                ray.hit = 1;
            }

            return ray;
        }

        private void TraceRecursive(int nodeIndex, Vector3 start, Vector3 dir, ref MeshRay ray)
        {
            AABBNode node = Nodes[nodeIndex];

            if (node.Faces.IsCreated == false)
            {
                // find closest node
                AABBNode leftChild = Nodes[node.Children + 0];
                AABBNode rightChild = Nodes[node.Children + 1];

                float result1 = 0;
                float result2 = 0;

                IntersectRayAABB(start, dir, leftChild.Bounds.Min, leftChild.Bounds.Max, out result1);
                IntersectRayAABB(start, dir, rightChild.Bounds.Min, rightChild.Bounds.Max, out result2);


                BlitableNativeArray<float> dist = new BlitableNativeArray<float>();
                dist.Initialize(2);
                dist[0] = result1;
                dist[1] = result2;


                int closest = 0;
                int furthest = 1;

                if (dist[1] < dist[0])
                {
                    closest = 1;
                    furthest = 0;
                }

                if (dist[closest] < ray.distance)
                    TraceRecursive(node.Children + closest, start, dir, ref ray);

                if (dist[furthest] < ray.distance)
                    TraceRecursive(node.Children + furthest, start, dir, ref ray);

                dist.Dispose();
            }
            else
            {
                float t, u, v, w, s;

                for (int i = 0; i < node.Faces.Length; ++i)
                {
                    int indexStart = node.Faces[i] * 3;

                    Vector3 a = Vertices[Indices[indexStart + 0]];
                    Vector3 b = Vertices[Indices[indexStart + 1]];
                    Vector3 c = Vertices[Indices[indexStart + 2]];

                    if (IntersectRayTriTwoSided(start, dir, a, b, c, out t, out u, out v, out w, out s))
                    {
                        if (t < ray.distance)
                        {
                            ray.distance = t;
                            ray.u = u;
                            ray.v = v;
                            ray.w = w;
                            ray.faceSign = s;
                            ray.faceIndex = node.Faces[i];
                        }
                    }
                }
            }

            //Nodes[nodeIndex] = node;
        }

        private void BuildRecursive(int nodeIndex, int start, int numFaces)
        {
            int MaxFacesPerLeaf = 6;

            // a reference to the current node, need to be careful here as this reference may become invalid if array is resized
            AABBNode n = GetNode(nodeIndex);

            // track max tree depth
            ++CurrentDepth;
            MaxDepth = Math.Max(MaxDepth, CurrentDepth);

            FNativeList<int> faces = GetFaces(start, numFaces);

            Vector3 min, max;
            CalculateFaceBounds(faces, out min, out max);

            n.Bounds = new Box3(min, max);
            n.Level = CurrentDepth - 1;
            Nodes[nodeIndex] = n;
            // calculate bounds of faces and add node  
            if (numFaces <= MaxFacesPerLeaf)
            {
                n.Faces.Initialize(faces.AsArray());
                Nodes[nodeIndex] = n;
                ++LeafNodes;
            }
            else
            {
                ++InnerNodes;

                // face counts for each branch
                //const uint32_t leftCount = PartitionMedian(n, faces, numFaces);
                int leftCount = PartitionSAH(faces);
                int rightCount = numFaces - leftCount;

                // alloc 2 nodes
                AABBNode node = Nodes[nodeIndex];
                node.Children = FreeNode;
                Nodes[nodeIndex] = node;

                // allocate two nodes
                FreeNode += 2;

                // split faces in half and build each side recursively
                BuildRecursive(GetNode(nodeIndex).Children + 0, start, leftCount);
                BuildRecursive(GetNode(nodeIndex).Children + 1, start + leftCount, rightCount);
            }

            --CurrentDepth;

        }

        // partion faces based on the surface area heuristic
        private int PartitionSAH(FNativeList<int> faces)
        {
            int numFaces = faces.Length;
            int bestAxis = 0;
            int bestIndex = 0;
            float bestCost = float.PositiveInfinity;

            FaceSorter predicate = new FaceSorter();
            predicate.Vertices = Vertices;
            predicate.Indices = Indices;

            // two passes over data to calculate upper and lower bounds
            BlitableNativeArray<float> cumulativeLower = new BlitableNativeArray<float>();
            cumulativeLower.Initialize(numFaces);

            BlitableNativeArray<float> cumulativeUpper = new BlitableNativeArray<float>();
            cumulativeUpper.Initialize(numFaces);

            for (int a = 0; a < 3; ++a)
            {
                // sort faces by centroids
                predicate.Axis = a;
                faces.Sort(predicate);

                Box3 lower = new Box3(float.PositiveInfinity, float.NegativeInfinity);
                Box3 upper = new Box3(float.PositiveInfinity, float.NegativeInfinity);

                for (int i = 0; i < numFaces; ++i)
                {
                    lower.Min = Min(lower.Min, FaceBounds[faces[i]].Min);
                    lower.Max = Max(lower.Max, FaceBounds[faces[i]].Max);

                    upper.Min = Min(upper.Min, FaceBounds[faces[numFaces - i - 1]].Min);
                    upper.Max = Max(upper.Max, FaceBounds[faces[numFaces - i - 1]].Max);

                    cumulativeLower[i] = lower.SurfaceArea;
                    cumulativeUpper[numFaces - i - 1] = upper.SurfaceArea;
                }

                float invTotalSA = 1.0f / cumulativeUpper[0];

                // test all split positions
                for (int i = 0; i < numFaces - 1; ++i)
                {
                    float pBelow = cumulativeLower[i] * invTotalSA;
                    float pAbove = cumulativeUpper[i] * invTotalSA;

                    float cost = 0.125f + (pBelow * i + pAbove * (numFaces - i));
                    if (cost <= bestCost)
                    {
                        bestCost = cost;
                        bestIndex = i;
                        bestAxis = a;
                    }
                }
            }

            cumulativeLower.Dispose();
            cumulativeUpper.Dispose();

            // re-sort by best axis
            predicate.Axis = bestAxis;
            faces.Sort(predicate);

            return bestIndex + 1;
        }

        private void CalculateFaceBounds(FNativeList<int> faces, out Vector3 outMin, out Vector3 outMax)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            // calculate face bounds
            for (int i = 0; i < faces.Length; ++i)
            {
                Vector3 a = Vertices[Indices[faces[i] * 3 + 0]];
                Vector3 b = Vertices[Indices[faces[i] * 3 + 1]];
                Vector3 c = Vertices[Indices[faces[i] * 3 + 2]];

                min = Min(a, min);
                max = Max(a, max);

                min = Min(b, min);
                max = Max(b, max);

                min = Min(c, min);
                max = Max(c, max);
            }

            outMin = min;
            outMax = max;
        }

        private Box3 CalculateFaceBounds(int i)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            Vector3 a = Vertices[Indices[i + 0]];
            Vector3 b = Vertices[Indices[i + 1]];
            Vector3 c = Vertices[Indices[i + 2]];

            min = Min(a, min);
            max = Max(a, max);

            min = Min(b, min);
            max = Max(b, max);

            min = Min(c, min);
            max = Max(c, max);

            return new Box3(min, max);
        }

        private Vector3 Min(Vector3 a, Vector3 b)
        {
            a.x = Math.Min(a.x, b.x);
            a.y = Math.Min(a.y, b.y);
            a.z = Math.Min(a.z, b.z);

            return a;
        }

        private Vector3 Max(Vector3 a, Vector3 b)
        {
            a.x = Math.Max(a.x, b.x);
            a.y = Math.Max(a.y, b.y);
            a.z = Math.Max(a.z, b.z);

            return a;
        }

        private AABBNode GetNode(int index)
        {
            if (index >= Nodes.Length)
            {
                int diff = index - Nodes.Length + 1;
                for (int i = 0; i < diff; i++)
                    Nodes.Add(new AABBNode());
            }

            return Nodes[index];
        }

        private FNativeList<int> GetFaces(int start, int num)
        {
            faces.Clear();

            for (int i = 0; i < num; i++)
            {
                faces.Add(Faces[i + start]);
            }


            return faces;
        }

        private bool IntersectRayTriTwoSided(Vector3 p, Vector3 dir, Vector3 a, Vector3 b, Vector3 c, out float t, out float u, out float v, out float w, out float sign)
        {
            // Moller and Trumbore's method
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 n = Vector3.Cross(ab, ac);

            float d = Vector3.Dot(dir * -1.0f, n);
            float ood = 1.0f / d;
            Vector3 ap = p - a;

            t = u = v = w = sign = 0.0f;

            t = Vector3.Dot(ap, n) * ood;
            if (t < 0.0f)
                return false;

            Vector3 e = Vector3.Cross(dir * -1.0f, ap);

            v = Vector3.Dot(ac, e) * ood;
            if (v < 0.0 || v > 1.0)
                return false;

            w = -Vector3.Dot(ab, e) * ood;
            if (w < 0.0 || v + w > 1.0)
                return false;

            u = 1.0f - v - w;
            sign = d;

            return true;
        }

        private bool IntersectRayAABB(Vector3 start, Vector3 dir, Vector3 min, Vector3 max, out float t)
        {
            //calculate candidate plane on each axis
            float tx = -1.0f, ty = -1.0f, tz = -1.0f;
            bool inside = true;
            t = 0;

            if (start.x < min.x)
            {
                if (dir.x != 0.0)
                    tx = (min.x - start.x) / dir.x;
                inside = false;
            }
            else if (start.x > max.x)
            {
                if (dir.x != 0.0)
                    tx = (max.x - start.x) / dir.x;
                inside = false;
            }

            if (start.y < min.y)
            {
                if (dir.y != 0.0)
                    ty = (min.y - start.y) / dir.y;
                inside = false;
            }
            else if (start.y > max.y)
            {
                if (dir.y != 0.0)
                    ty = (max.y - start.y) / dir.y;
                inside = false;
            }

            if (start.z < min.z)
            {
                if (dir.z != 0.0)
                    tz = (min.z - start.z) / dir.z;
                inside = false;
            }
            else if (start.z > max.z)
            {
                if (dir.z != 0.0)
                    tz = (max.z - start.z) / dir.z;
                inside = false;
            }

            //if point inside all planes
            if (inside)
            {
                t = 0.0f;
                return true;
            }

            //we now have t values for each of possible intersection planes
            //find the maximum to get the intersection point
            float tmax = tx;
            int taxis = 0;

            if (ty > tmax)
            {
                tmax = ty;
                taxis = 1;
            }
            if (tz > tmax)
            {
                tmax = tz;
                taxis = 2;
            }

            if (tmax < 0.0f)
                return false;

            //check that the intersection point lies on the plane we picked
            //we don't test the axis of closest intersection for precision reasons

            //no eps for now
            float eps = 0.0f;

            Vector3 hit = start + dir * tmax;

            if ((hit.x < min.x - eps || hit.x > max.x + eps) && taxis != 0)
                return false;
            if ((hit.y < min.y - eps || hit.y > max.y + eps) && taxis != 1)
                return false;
            if ((hit.z < min.z - eps || hit.z > max.z + eps) && taxis != 2)
                return false;

            //output results
            t = tmax;
            return true;
        }

        private unsafe struct AABBNode
        {
            public Box3 Bounds;



            public byte IsLeaf
            {
                get
                {
                    if (Faces.IsCreated) return 0;
                    return 1;
                }

            }

            public int Level;

            public BlitableNativeArray<int> Faces;

            public int Children;

            public void CleanUp()
            {
                Faces.Dispose();
            }
        };

        private struct FaceSorter : IComparer<int>
        {
            internal NativeArray<Vector3> Vertices;
            internal NativeArray<int> Indices;
            internal int Axis;

            public int Compare(int i0, int i1)
            {
                float a = GetCentroid(i0);
                float b = GetCentroid(i1);

                return a.CompareTo(b);
            }

            private float GetCentroid(int face)
            {
                Vector3 a = Vertices[Indices[face * 3 + 0]];
                Vector3 b = Vertices[Indices[face * 3 + 1]];
                Vector3 c = Vertices[Indices[face * 3 + 2]];

                return (a[Axis] + b[Axis] + c[Axis]) / 3.0f;
            }
        }

    }

}
