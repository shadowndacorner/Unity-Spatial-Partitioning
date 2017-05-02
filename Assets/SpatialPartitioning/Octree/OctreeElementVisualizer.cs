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

[RequireComponent(typeof(OctreeElementComponent))]
public class OctreeElementVisualizer : MonoBehaviour
{
    public int x, y, z;

    OctreeElementComponent _el;
    public OctreeElementComponent Element
    {
        get
        {
            if (!_el)
                _el = GetComponent<OctreeElementComponent>();

            return _el;
        }
    }

    // This is a slow, bruteforce test
    void VerifyIsClosest(OctreeElementComponent comp)
    {
        OctreeElementComponent closest = null;
        float dist = float.PositiveInfinity;
        foreach (var v in FindObjectsOfType<OctreeElementComponent>())
        {
            if (v == Element)
                continue;

            float mdist;
            if ((mdist = Vector3.SqrMagnitude(v.transform.position - transform.position)) < dist)
            {
                closest = v;
                dist = mdist;
            }
        }
        if (closest != comp)
        {
            Debug.LogError("Incorrect closest calculation: got " + comp.name + ", was really " + closest.name, this);
            //Debug.Break();
        }
    }

    void LateUpdate()
    {
        if (Element.CurrentNode != null && Element.CurrentNode.Tree != null)
        {
            var observe = Element.CurrentNode.Parent.Parent;
            x = observe.x * (int)Mathf.Pow(2, observe.Tree.OctreeDepth - observe.Depth);
            y = observe.y * (int)Mathf.Pow(2, observe.Tree.OctreeDepth - observe.Depth);
            z = observe.z * (int)Mathf.Pow(2, observe.Tree.OctreeDepth - observe.Depth);
            var closest = Element.CurrentNode.Tree.Closest(Element);
            VerifyIsClosest(closest);

            if (closest)
            {
                Debug.DrawLine(closest.transform.position, transform.position, Color.green);
            }
            else
            {
                Debug.LogError("No closest node found");
            }
        }
    }

    void OnDrawGizmos()
    {
        var node = Element.CurrentNode;
        while (node != null)
        {
            Gizmos.DrawWireCube(node.NodeBounds.center, node.NodeBounds.size);
            node = node.Parent;
        }
    }
}
