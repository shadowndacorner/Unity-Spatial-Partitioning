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

public class OctreeElementComponent : MonoBehaviour
{
    private OctreeComponent _cont;
    public OctreeComponent Container
    {
        get
        {
            if (!_cont)
            {
                foreach (var v in FindObjectsOfType<OctreeComponent>())
                {
                    if (v.Tree != null && v.Tree.OctreeBounds.Contains(transform.position))
                    {
                        _cont = v;
                        CurrentNode = _cont.Tree.Insert(this, transform.position);
                        break;
                    }
                }
            }
            return _cont;
        }
    }

    SparseOctree<OctreeElementComponent>.OctreeNode _node;
    public SparseOctree<OctreeElementComponent>.OctreeNode CurrentNode
    {
        get
        {
            return _node;
        }
        set
        {
            _node = value;
            CurBounds = _node.NodeBounds;
        }
    }
    public Bounds CurBounds;

    bool RecursiveVerify(HashSet<SparseOctree<OctreeElementComponent>.OctreeNode> set, SparseOctree<OctreeElementComponent>.OctreeNode node)
    {
        if (set.Contains(node) != node.Contained.Contains(this))
        {
            Debug.LogError("ERROR: Octree contained mismatch!  Failed with node @ " + node.NodeBounds);
            return false;
        }

        if (node.Divisions != null)
        {
            for (int z = 0; z < 2; ++z)
            {
                for (int y = 0; y < 2; ++y)
                {
                    for (int x = 0; x < 2; ++x)
                    {
                        if (!RecursiveVerify(set, node.Divisions[x, y, z]))
                            return false;
                    }
                }
            }
        }

        return true;
    }

    void Verify()
    {
        var valid = new HashSet<SparseOctree<OctreeElementComponent>.OctreeNode>();
        var tg = CurrentNode;
        while(tg != null)
        {
            valid.Add(tg);
            tg = tg.Parent;
        }

        Debug.Log("Verify status: " + RecursiveVerify(valid, CurrentNode.Tree.Root));
    }

    void OnEnable()
    {
        if (Container)
        {
            Debug.Log("Successful entry");
        }
            //CurrentNode = Container.Tree.Insert(this, transform.position);
    }

    void Update()
    {
        if (Container)
        {
            // Debug the octree
            //Verify();
            CurrentNode = Container.Tree.Update(this, CurrentNode, transform.position);
        }
    }

    void LateUpdate()
    {
        if (Container)
            CurrentNode = Container.Tree.Update(this, CurrentNode, transform.position);
    }

    void OnDisable()
    {
        if (Container)
            CurrentNode.RecursiveRemove(this);
    }
}
