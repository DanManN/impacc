using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

public class Simplex
{

    // TODO: Not yet implemented 3D version

    public List<Vector3> vertices;
    private Vector3 ORIGIN = Vector3.zero;
    // public GameObject simplexObject;

    public Simplex()
    {
        vertices = new List<Vector3>();
    }

    public void add(Vector3 point)
    {
        vertices.Add(point);
        Debug.Log("Add Point" + point);
    }

    public Vector3 get(int i)
    {
        return vertices[i];
    }

    public Vector3 getB()
    {
        return vertices[vertices.Count - 2];
    }
    public Vector3 getC()
    {
        return vertices[vertices.Count - 3];
    }

    public Vector3 getLast()
    {
        return vertices[vertices.Count - 1];
        // return vertices.Last();
    }

    // public bool contains(Vector3 point)
    // {
    //     return vertices.Contains(point);
    // }

    private Vector3 tripleProduct(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(Vector3.Cross(a, b), c);
    }

    // Determine if the origin is in the current simplex
    public bool containsOrigin(Vector3 d)
    {
        // get the last point added to the simplex
        var a = this.getLast();
        // compute AO (same thing as -A)
        var ao = -a;

        if (this.vertices.Count == 3) 
        {
            // then its the triangle case
            // get b and c
            var b = this.getB();
            var c = this.getC();
            // compute the edges
            var ab = b - a;
            var ac = c - a;
            // compute the normals
            var abPerp = this.tripleProduct(ac, ab, ab);
            var acPerp = this.tripleProduct(ab, ac, ac);
            // is the origin in R4
            if (Vector3.Dot(abPerp,ao) > 0) 
            {
                // remove point c
                // this.remove(c);
                this.vertices.RemoveAt(vertices.Count - 3);
                // set the new direction to abPerp
                d = abPerp;
            } 
            else 
            {
                // is the origin in R3
                if (Vector3.Dot(acPerp,ao) > 0) 
                {
                    // remove point b
                    this.vertices.RemoveAt(vertices.Count - 2);
                    // this.remove(b);
                    // set the new direction to acPerp
                    d = acPerp;
                } 
                else
                {
                    // otherwise we know its in R5 so we can return true
                    return true;
                }
            }
        }
        else 
        {
            // then its the line segment case
            var b = this.getB();
            // compute AB
            var ab = b - a;
            // get the perp to AB in the direction of the origin
            var abPerp = this.tripleProduct(ab, ao, ab);
            // set the direction to abPerp
            d= abPerp;
        }
        return false;
    }

    // Find the edge who is closest to the origin 
    // and use its normal (in the direction of the origin) 
    // as the new d and continue the loop
    // Ref: https://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
    // Ref: http://www.java2s.com/Code/CSharp/Development-Class/DistanceFromPointToLine.htm
    
    public Vector3 getDirection()
    {
        Vector3 edge_lhs = vertices[0];
        Vector3 edge_rhs = vertices[1];
        Vector3 l1 = vertices[0];
        Vector3 l2 = vertices[1];
        double closertDistance = double.PositiveInfinity;

        for (int i = 0; i < vertices.Count; i++)
        {
            l1 = vertices[i];
            if (i == vertices.Count - 1)
            {
                l2 = vertices[0];
            }
            else
            {
                l2 = vertices[i+1];
            }

            // Note that Vector3's y is 0 for 2D version.
            var tmp = Math.Abs((l2.x - l1.x)*(l1.z - ORIGIN.z) - (l1.x - ORIGIN.x)*(l2.z - l1.z))/
                    Math.Sqrt(Math.Pow(l2.x - l1.x, 2) + Math.Pow(l2.z - l1.z, 2));
            
            if (tmp < closertDistance)
            {
                closertDistance = tmp;
                
                edge_lhs = l1;
                edge_rhs = l2;
            }
        }

        // Calculate normal and return
        Vector3 normal = Vector3.zero;
        normal.x = edge_rhs.z - edge_lhs.z;
        normal.z = -(edge_rhs.x - edge_lhs.x);

        // TODO: Directly use math equation
        // https://en.wikipedia.org/wiki/Vector_projection
        // normal = X2 - X1
        // p = X1 - Origin
        // Project p to normal will gives |p| as the distance

        return normal;
    }
}