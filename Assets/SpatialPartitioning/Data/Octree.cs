/*
    Copyright (c) 2017 Ian Diaz

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sparse octree
public class SparseOctree<T> where T : class
{
    public class OctreeNode
    {
        public OctreeNode(SparseOctree<T> tree, OctreeNode parent, Bounds b)
        {
            NodeBounds = b;
            Tree = tree;
            Parent = parent;
            if (parent == null)
                Depth = 0;
            else
                Depth = parent.Depth + 1;
        }

        public SparseOctree<T> Tree { get; internal set; }
        public OctreeNode Parent { get; internal set; }
        public Bounds NodeBounds { get; internal set; }

        public HashSet<T> Contained = new HashSet<T>();
        public OctreeNode[,,] Divisions;
        public int Depth { get; internal set; }

        // Octree-space position
        public int x, y, z;
        
        void CheckChildren()
        {
            // If we're a leaf, ignore
            if (Depth >= Tree.OctreeDepth)
                return;

            // Delete children if we're empty
            if (Contained.Count == 0)
            {
                //Divisions = null;
            }
            // Otherwise, if we don't have children, 
            else if (Divisions == null)
            {
                Divisions = new OctreeNode[2, 2, 2];
                for (float z = 0; z < 2; ++z)
                {
                    for (float y = 0; y < 2; ++y)
                    {
                        for (float x = 0; x < 2; ++x)
                        {
                            var b = NodeBounds;
                            var cb = new Bounds();
                            cb.min = new Vector3(Mathf.Lerp(b.min.x, b.max.x, x / 2f), Mathf.Lerp(b.min.y, b.max.y, y / 2f), Mathf.Lerp(b.min.z, b.max.z, z / 2f));
                            cb.max = new Vector3(Mathf.Lerp(b.min.x, b.max.x, (x + 1) / 2f), Mathf.Lerp(b.min.y, b.max.y, (y + 1) / 2f), Mathf.Lerp(b.min.z, b.max.z, (z + 1) / 2f));
                            var child = Divisions[(int)x, (int)y, (int)z] = new OctreeNode(Tree, this, cb);

                            child.x = (int)((x) + 2 * this.x);
                            child.y = (int)((y) + 2 * this.y);
                            child.z = (int)((z) + 2 * this.z);
                        }
                    }
                }
            }
        }

        public OctreeNode RecursiveInsert(T obj, Vector3 position)
        {
            if (!Contained.Contains(obj))
                Contained.Add(obj);

            CheckChildren();
            if (Depth < Tree.OctreeDepth)
            {
                for (int z = 0; z < 2; ++z)
                {
                    for (int y = 0; y < 2; ++y)
                    {
                        for (int x = 0; x < 2; ++x)
                        {
                            if (Divisions[x, y, z].NodeBounds.Contains(position))
                            {
                                return Divisions[x, y, z].RecursiveInsert(obj, position);
                            }
                        }
                    }
                }
            }
            return this;
        }

        public OctreeNode RecursiveUpdate(T obj, Vector3 pos)
        {
            if (!NodeBounds.Contains(pos))
            {
                Contained.Remove(obj);
                CheckChildren();
                if (Parent != null)
                    return Parent.RecursiveUpdate(obj, pos);
                else
                    throw new System.InvalidOperationException("Node not contained in octree (bounds == "+NodeBounds+")");
            }

            return RecursiveInsert(obj, pos);
        }

        public void RecursiveRemove(T obj)
        {
            if (Contained.Contains(obj))
                Contained.Remove(obj);

            CheckChildren();
            if (Parent != null)
                Parent.RecursiveRemove(obj);
        }
    }
    public struct OctreeEntry
    {
        public Vector3 position;
        public OctreeNode Node;
    }

    public SparseOctree(Bounds b, int depth = 3)
    {
        OctreeBounds = b;
        OctreeDepth = depth;
        Root = new OctreeNode(this, null, b);
    }

    public Dictionary<T, OctreeEntry> Entries = new Dictionary<T, OctreeEntry>();
    public int OctreeDepth { get; internal set; }
    public Bounds OctreeBounds { get; internal set; }
    public OctreeNode Insert(T obj, Vector3 position)
    {
        if (!OctreeBounds.Contains(position))
            throw new System.InvalidOperationException("Attempting to insert out of octree bounds");

        var node = Root.RecursiveInsert(obj, position);
        OctreeEntry entry = new OctreeEntry();
        entry.position = position;
        entry.Node = node;
        Entries[obj] = entry;
        return node;
    }
    
    public OctreeNode Update(T obj, OctreeNode node, Vector3 newPos)
    {
        if (!OctreeBounds.Contains(newPos))
            throw new System.InvalidOperationException("Attempting to insert out of octree bounds");

        if (node.NodeBounds.Contains(newPos))
        {
            // Update position
            var ent = Entries[obj];
            ent.position = newPos;
            Entries[obj] = ent;

            return node;
        }

        var newNode = node.RecursiveUpdate(obj, newPos);
        OctreeEntry entry = new OctreeEntry();
        entry.position = newPos;
        entry.Node = newNode;
        Entries[obj] = entry;

        return newNode;
    }

    public void Remove(T obj, OctreeNode node)
    {
        node.RecursiveRemove(obj);
        Entries.Remove(obj);
    }

    public OctreeNode GetPosition(int tx, int ty, int tz, int targetDepth, OctreeNode root)
    {
        int max = (int)Mathf.Pow(2, OctreeDepth);
        if (tx < 0 || ty < 0 || tz < 0 || tx > max || ty > max || tz > max)
            return null;

        if (targetDepth == root.Depth && tx == root.x && ty == root.y && tz == root.z)
            return root;

        if (root.Divisions == null)
            return null;

        for (int z = 0; z < 2; ++z)
        {
            for (int y = 0; y < 2; ++y)
            {
                for (int x = 0; x < 2; ++x)
                {
                    int delta = (int)Mathf.Pow(2, (float)(root.Tree.OctreeDepth - root.Depth));
                    int rx = root.x * delta;
                    int ry = root.y * delta;
                    int rz = root.z * delta;

                    if (rx <= tx && tx < rx + delta
                        && ry <= ty && ty < ry + delta
                        && rz <= tz && tz < rz + delta)
                    {
                        var node = GetPosition(tx, ty, tz, targetDepth, root.Divisions[x, y, z]);
                        if (node != null)
                            return node;
                    }
                }
            }
        }
        return null;
    }

    public OctreeNode GetPosition(int x, int y, int z, int targetDepth)
    {
        return GetPosition(x, y, z, targetDepth, Root);
    }

    public OctreeNode GetPosition(int x, int y, int z)
    {
        return GetPosition(x, y, z, OctreeDepth, Root);
    }

    [System.Obsolete("Deprecated in favor of new algorithm")]
    public T OldClosest(T obj)
    {
        if (Root.Contained.Count > 0)
        {
            var entry = Entries[obj];
            var node = entry.Node;
            
            // Find closest node with more than 1 object
            while (node != null && node.Contained.Count == 1)
                node = node.Parent;

            // If we're the only thing that exists, return null
            if (node == null)
                return default(T);

            // Initialize
            T closest = default(T);
            float dist = float.PositiveInfinity;

            // Hashset to ensure that we don't do duplicate distance tests
            HashSet<T> tried = new HashSet<T>();
            
            // Make sure we don't try to return ourself
            tried.Add(obj);

            // TODO: optimize this loop to only look at the closest neighbor node by comparing sqr dist between bounding box centers
            // If we found something closer, look a level up to see if there's anything else closer
            bool foundClosest = true;
            while (foundClosest)
            {
                // If we just tested root, break
                if (node == null)
                    break;

                // Make sure that the loop will stop if we don't find anything
                foundClosest = false;
                foreach (var v in node.Contained)
                {
                    // If we've already tried this node, ignore it
                    if (tried.Contains(v))
                        continue;

                    tried.Add(v);
                    float mdist;
                    if ((mdist = Vector3.SqrMagnitude(Entries[v].position - entry.position)) < dist)
                    {
                        closest = v;
                        dist = mdist;
                        foundClosest = true;
                    }
                }

                // Test for the next level with an untested node
                int cnt = node.Contained.Count;
                while (node != null && node.Contained.Count == cnt)
                    node = node.Parent;
            }
            return closest;
        }
        return default(T);
    }

    struct ClosestReturn
    {
        public ClosestReturn(float d, OctreeNode close)
        {
            dist = d;
            closest = close;
        }

        public float dist;
        public OctreeNode closest;
    }
    private ClosestReturn ClosestChildRecursive(OctreeNode parent, OctreeNode target, HashSet<OctreeNode> tested)
    {
        if (tested.Contains(parent))
            return new ClosestReturn();

        if (parent.Divisions == null)
            return new ClosestReturn();

        OctreeNode child = null;
        float dist = float.PositiveInfinity;
        for (int z = 0; z < 2; ++z)
        {
            for (int y = 0; y < 2; ++y)
            {
                for (int x = 0; x < 2; ++x)
                {
                    var cur = parent.Divisions[x, y, z];
                    if (cur == target)
                        continue;

                    float mdist;
                    if ((mdist = Vector3.SqrMagnitude(target.NodeBounds.center - cur.NodeBounds.center)) < dist)
                    {
                        child = cur;
                        dist = mdist;
                    }
                }
            }
        }
        return new ClosestReturn(dist, child);
    }

    public T BruteforceClosest(T obj)
    {
        T closest = null;
        float dist = float.PositiveInfinity;

        var entry = Entries[obj];
        foreach(var v in Root.Contained)
        {
            if (v == obj)
                continue;
            float mdist;
            if ((mdist = Vector3.SqrMagnitude(entry.position - Entries[v].position)) < dist)
            {
                dist = mdist;
                closest = v;
            }   
        }
        return closest;
    }

    T RecursiveClosest(T obj)
    {
        var entry = Entries[obj];

        // Find next closest node with something inside
        var closest = ClosestChildRecursive(Root, entry.Node, new HashSet<OctreeNode>());

        T cobj = null;
        float dist = float.PositiveInfinity;
        if (entry.Node.Contained.Count > 1)
        {
            foreach (var v in entry.Node.Contained)
            {
                if (v == obj)
                    continue;
                float mdist;
                if ((mdist = Vector3.SqrMagnitude(entry.position - Entries[v].position)) < dist)
                {
                    dist = mdist;
                    cobj = v;  
                }
            }
        }

        if (closest.closest != null)
        {
            foreach (var v in closest.closest.Contained)
            {
                float mdist;
                if ((mdist = Vector3.SqrMagnitude(entry.position - Entries[v].position)) < dist)
                {
                    if (v == obj)
                        continue;

                    dist = mdist;
                    cobj = v;
                }
            }
        }

        return cobj;
    }

    public T Closest(T obj)
    {
        if (true)
            return OldClosest(obj);
        if (Root.Contained.Count == 1)
            return default(T);

        var entry = Entries[obj];
        Vector3 boundsCenter = entry.Node.NodeBounds.center;

        T closest = null;
        float dist = float.PositiveInfinity;
        if (entry.Node.Contained.Count > 1)
        {
            foreach (var v in entry.Node.Contained)
            {
                if (v == obj)
                    continue;

                float mdist;
                if ((mdist = Vector3.SqrMagnitude(Entries[v].position - entry.position)) < dist)
                {
                    closest = v;
                    dist = mdist;
                }
            }
        }

        var tested = new HashSet<OctreeNode>();
        tested.Add(entry.Node);
        
        int searchRadius = 1;

        bool isNodeAvailable = closest != null;
        bool foundNode = true;
        while (foundNode || !isNodeAvailable && (searchRadius * searchRadius + searchRadius * searchRadius + searchRadius * searchRadius) < 3)
        {
            foundNode = false;
            for (int z = -searchRadius; z <= searchRadius; ++z)
            {
                for (int y = -searchRadius; y <= searchRadius; ++y)
                {
                    for (int x = -searchRadius; x <= searchRadius; ++x)
                    {
                        var node = GetPosition(entry.Node.x + x, entry.Node.y + y, entry.Node.z + z);
                        if (!tested.Contains(node) && node != null && node.Contained.Count > 0)
                        {
                            foreach (var v in node.Contained)
                            {
                                float mdist;
                                if ((mdist = Vector3.SqrMagnitude(Entries[v].position - entry.position)) < dist)
                                {
                                    isNodeAvailable = true;
                                    foundNode = true;
                                    closest = v;
                                    dist = mdist;
                                }
                            }
                        }
                        tested.Add(node);
                    }
                }
            }
            ++searchRadius;
        }
        if (closest == null)
            return OldClosest(obj);

        return closest;
    }

    public OctreeNode Root { get; internal set; }
}
