using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;
using Utilities;

namespace PositionBasedDynamics
{
  public class PBD
  {
    // epsilon (a value that determine if the changes is too small to change the position)
    public static float eps = 1e-6f;

    public static void UpdatePosition(MeshData md)
    {
      for (int i=0; i < md.particles.Length; i++)
      {
        md.particles[i].velocity = md.particles[i].predictedPos - md.particles[i].pos;
      }
    }

    public static bool ExternalForce(
      float dt,
      Vector3 p, Vector3 v, float w,
      Vector3 force,
      float damping,
      out Vector3 corr)
    {
      corr = Vector3.zero;
      if (w == 0.0f) return false;

      v += dt * w * force;
      v *= damping;
      corr = dt * v;
      return true;
    }

    public static void WindForce(
      float dt,
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      Vector3 p2, float w2,
      Vector3 windDir,
      float windForce,
      out Vector3 corr0,
      out Vector3 corr1,
      out Vector3 corr2)
    {
      Vector3 triDir = Vector3.Normalize(Vector3.Cross(p1-p0, p2-p0));
      float windFactor = Vector3.Dot(triDir, windDir);
      int dirCorr = windFactor > 0 ? 1 : -1;
      windFactor = windFactor > 0 ? windFactor : -windFactor;

      corr0 = triDir * dirCorr * windForce * windFactor * w0 * dt * dt;
      corr1 = triDir * dirCorr * windForce * windFactor * w1 * dt * dt;
      corr2 = triDir * dirCorr * windForce * windFactor * w2 * dt * dt;
    }

    public static bool DistanceConstraint(
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      float restLength,
      float stretchStiffness,
      float compressionStiffness,
      out Vector3 corr0,
      out Vector3 corr1)
    {
      corr0 = corr1 = Vector3.zero;
      float wSum = w0 + w1;
      if (wSum == 0.0f) return false;

      Vector3 n = p0 - p1;
      float d = n.magnitude;
      Vector3.Normalize(n);

      Vector3 corr;
      if (d < restLength)
      {
        corr = compressionStiffness * n * (d - restLength) / wSum;
      } else
      {
        corr = stretchStiffness * n * (d - restLength) / wSum;
      }

      corr0 = -w0 * corr;
      corr1 = w1 * corr;
      return true;
    }

    public static bool DihedralConstraint(
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      Vector3 p2, float w2,
      Vector3 p3, float w3,
      float restAngle,
      float stiffness,
      out Vector3 corr0,
      out Vector3 corr1,
      out Vector3 corr2,
      out Vector3 corr3)
    {
      corr0 = corr1 = corr2 = corr3 = Vector3.zero;
      if (w0 == 0.0f && w1 == 0.0f) return false;

      Vector3 e = p3 - p2;
      float elen = e.magnitude;
      if (elen < eps) return false;

      float invElen = 1 / elen;

      Vector3 n1 = Vector3.Cross((p2 - p0), (p3 - p0)); n1 /= n1.sqrMagnitude;
      Vector3 n2 = Vector3.Cross((p3 - p1), (p2 - p1)); n2 /= n2.sqrMagnitude;

      Vector3 d0 = elen * n1;
      Vector3 d1 = elen * n2;
      Vector3 d2 = Vector3.Dot((p0 - p3), e) * invElen * n1 + Vector3.Dot((p1-p3), e) * invElen * n2;
      Vector3 d3 = Vector3.Dot((p2 - p0), e) * invElen * n1 + Vector3.Dot((p2-p1), e) * invElen * n2;

      Vector3.Normalize(n1);
      Vector3.Normalize(n2);
      float dot = Vector3.Dot(n1, n2);

      if (dot < -1.0f) dot = -1.0f;
      if (dot >  1.0f) dot =  1.0f;
      float phi = Mathf.Acos(dot);	

      // float phi = (-0.6981317 * dot * dot - 0.8726646) * dot + 1.570796;	// fast approximation

      float lambda = 
        w0 * d0.sqrMagnitude +
        w1 * d1.sqrMagnitude +
        w2 * d2.sqrMagnitude +
        w3 * d3.sqrMagnitude;

      if (lambda == 0.0f) return false;	

      // stability
      // 1.5 is the largest magic number I found to be stable in all cases :-)
      //if (stiffness > 0.5 && fabs(phi - b.restAngle) > 1.5)		
      //	stiffness = 0.5;

      lambda = (phi - restAngle) / lambda * stiffness;

      if (Vector3.Dot(Vector3.Cross(n1, n2), e) > 0.0f) lambda = -lambda;	

      corr0 = - w0 * lambda * d0;
      corr1 = - w1 * lambda * d1;
      corr2 = - w2 * lambda * d2;
      corr3 = - w3 * lambda * d3;
      return true;
    }

    public static bool VolumeConstraint(
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      Vector3 p2, float w2,
      Vector3 p3, float w3,
      float restVolume,
      float negVolumeStiffness,
      float posVolumeStiffness,
      out Vector3 corr0,
      out Vector3 corr1,
      out Vector3 corr2,
      out Vector3 corr3)
    {
      corr0 = corr1 = corr2 = corr3 = Vector3.zero;
      float volume = (1 / 6) * Vector3.Dot(Vector3.Cross((p1 - p0), (p2 - p0)), (p3 - p0));

      if (posVolumeStiffness == 0.0f && volume > 0.0f) return false;
      if (negVolumeStiffness == 0.0f && volume < 0.0f) return false;

      Vector3 grad0 = Vector3.Cross((p1 - p2), (p3 - p2));
      Vector3 grad1 = Vector3.Cross((p2 - p0), (p3 - p0));
      Vector3 grad2 = Vector3.Cross((p0 - p1), (p3 - p1));
      Vector3 grad3 = Vector3.Cross((p1 - p0), (p2 - p0));

      float lambda = 
        w0 * grad0.sqrMagnitude +
        w1 * grad1.sqrMagnitude +
        w2 * grad2.sqrMagnitude +
        w3 * grad3.sqrMagnitude;

      if (Mathf.Abs(lambda) < eps) return false;

      if (volume < 0.0f) lambda = negVolumeStiffness * (volume - restVolume) / lambda;
      else lambda = posVolumeStiffness * (volume - restVolume) / lambda;

      corr0 = -lambda * w0 * grad0;
      corr1 = -lambda * w1 * grad1;
      corr2 = -lambda * w2 * grad2;
      corr3 = -lambda * w3 * grad3;

      return true;
    }

    public static bool EdgePointDistanceConstraint(
      Vector3 p, float w,
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      float restDist,
      float compressionStiffness,
      float stretchStiffness,
      out Vector3 corr,
      out Vector3 corr0,
      out Vector3 corr1)
    {
      corr = corr0 = corr1 = Vector3.zero;

      Vector3 d = p1 - p0;
      float t;
      if ((p0 - p1).sqrMagnitude < eps * eps) t = 0.5f;
      else
      {
        float d2 = Vector3.Dot(d, d);
        t = Vector3.Dot(d, (p - p1)) / d2;
        if (t < 0.0f) t = 0.0f;
        else if (t > 1.0f) t = 1.0f;
      }

      // closest point on edge
      Vector3 q = p0 + d*t;
      Vector3 n = p - q;
      float dist = n.magnitude;
      Vector3.Normalize(n);
      float C = dist - restDist;
      float b0 = 1.0f - t;
      float b1 = t;

      Vector3 grad = n;
      Vector3 grad0 = -n * b0;
      Vector3 grad1 = -n * b1;

      float s = w + w0 * b0 * b0 + w1 * b1 * b1;
      if (s == 0.0f) return false;

      s = C / s;
      if (C < 0.0f) s *= compressionStiffness;
      else s *= stretchStiffness;

      if (s == 0.0f) return false;

      corr = -s * w * grad;
      corr0 = -s * w0 * grad0;
      corr1 = -s * w1 * grad1;

      return true;
    }

    public static bool TrianglePointDistanceConstraint(
      Vector3 p, float w,
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      Vector3 p2, float w2,
      float restDist,
      float compressionStiffness,
      float stretchStiffness,
      out Vector3 corr,
      out Vector3 corr0,
      out Vector3 corr1,
      out Vector3 corr2)
    {
      corr = corr0 = corr1 = corr2 = Vector3.zero;
      // find barycentric coordinates of closest point on triangle

      // for singular case
      float b0 = 1.0f / 3.0f;
      float b1 = b0;
      float b2 = b0;

      float a, b, c, d, e, f;
      float det;

      Vector3 d1 = p1 - p0;
      Vector3 d2 = p2 - p0;
      Vector3 pp0 = p - p0;
      a = Vector3.Dot(d1, d1);
      b = Vector3.Dot(d2, d1);
      c = Vector3.Dot(pp0, d1);
      d = b;
      e = Vector3.Dot(d2, d2);
      f = Vector3.Dot(pp0, d2);
      det = a*e - b*d;

      float s, t;
      Vector3 dist;
      float dist2;
      if (det != 0.0f)
      {
        s = (c*e - b*f) / det;
        t = (a*f - c*d) / det;
        // inside triangle
        b0 = 1.0f - s - t;
        b1 = s;
        b2 = t;
        if (b0 < 0.0)
        {
          // on edge 1-2
          dist = p2 - p1;
          dist2 = Vector3.Dot(dist, dist);
          t = (dist2 == 0.0f) ? 0.5f : Vector3.Dot(dist, (p - p1)) / dist2;
          if (t < 0.0) t = 0.0f;	// on point 1
          if (t > 1.0) t = 1.0f;	// on point 2
          b0 = 0.0f;
          b1 = (1.0f - t);
          b2 = t;
        }
        else if (b1 < 0.0)
        {
          // on edge 2-0
          dist = p0 - p2;
          dist2 = Vector3.Dot(dist, dist);
          t = (dist2 == 0.0f) ? 0.5f : Vector3.Dot(dist, (p - p2)) / dist2;
          if (t < 0.0) t = 0.0f;	// on point 2
          if (t > 1.0) t = 1.0f; // on point 0
          b1 = 0.0f;
          b2 = (1.0f - t);
          b0 = t;
        }
        else if (b2 < 0.0)
        {
          // on edge 0-1
          dist = p1 - p0;
          dist2 = Vector3.Dot(dist, dist);
          t = (dist2 == 0.0f) ? 0.5f : Vector3.Dot(dist, (p - p0)) / dist2;
          if (t < 0.0) t = 0.0f;	// on point 0
          if (t > 1.0) t = 1.0f;	// on point 1
          b2 = 0.0f;
          b0 = (1.0f - t);
          b1 = t;
        }
      }
      Vector3 q = p0 * b0 + p1 * b1 + p2 * b2;
      Vector3 n = p - q;
      float l = n.magnitude;
      Vector3.Normalize(n);
      float C = l - restDist;
      Vector3 grad = n;
      Vector3 grad0 = -n * b0;
      Vector3 grad1 = -n * b1;
      Vector3 grad2 = -n * b2;

      s = w + w0 * b0*b0 + w1 * b1*b1 + w2 * b2*b2;
      if (s == 0.0f)
        return false;

      s = C / s;
      if (C < 0.0f) s *= compressionStiffness;
      else s *= stretchStiffness;

      if (s == 0.0f) return false;

      corr = -s * w * grad;
      corr0 = -s * w0 * grad0;
      corr1 = -s * w1 * grad1;
      corr2 = -s * w2 * grad2;

      return true;
    }

    public static bool EdgeEdgeDistanceConstraint(
      Vector3 p0, float w0,
      Vector3 p1, float w1,
      Vector3 p2, float w2,
      Vector3 p3, float w3,
      float restDist,
      float compressionStiffness,
      float stretchStiffness,
      out Vector3 corr0,
      out Vector3 corr1,
      out Vector3 corr2,
      out Vector3 corr3)
    {
      corr0 = corr1 = corr2 = corr3 = Vector3.zero;
      Vector3 d0 = p1 - p0;
      Vector3 d1 = p3 - p2;

      float a, b, c, d, e, f;

      a = d0.sqrMagnitude;
      b = -Vector3.Dot(d0, d1);
      c = Vector3.Dot(d0, d1);
      d = -d1.sqrMagnitude;
      e = Vector3.Dot((p2 - p0), d0);
      f = Vector3.Dot((p2 - p0), d1);
      float det = a*d - b*c;
      float s, t;
      if (det != 0.0f)
      {
        det = 1.0f / det;
        s = (e*d - b*f) * det;
        t = (a*f - e*c) * det;
      }
      else
      {
        // d0 and d1 parallel
        float s0 = Vector3.Dot(p0, d0);
        float s1 = Vector3.Dot(p1, d0);
        float t0 = Vector3.Dot(p2, d0);
        float t1 = Vector3.Dot(p3, d0);
        bool flip0 = false;
        bool flip1 = false;

        if (s0 > s1) {f = s0; s0 = s1; s1 = f; flip0 = true;}
        if (t0 > t1) {f = t0; t0 = t1; t1 = f; flip1 = true;}

        if (s0 >= t1)
        {
          s = !flip0 ? 0.0f : 1.0f;
          t = !flip1 ? 1.0f : 0.0f;
        } else if (t0 >= s1)
        {
          s = !flip0 ? 1.0f : 0.0f;
          t = !flip1 ? 0.0f : 1.0f;
        } else
        {
          // overlap
          float mid = (s0 > t0) ? (s0 + t1) * 0.5f : (t0 + s1) * 0.5f;
          s = (s0 == s1) ? 0.5f : (mid - s0) / (s1 - s0);
          t = (t0 == t1) ? 0.5f : (mid - t0) / (t1 - t0);
        }
      }
      if (s < 0.0) s = 0.0f;
      if (s > 1.0) s = 1.0f;
      if (t < 0.0) t = 0.0f;
      if (t > 1.0) t = 1.0f;

      float b0 = 1.0f - s;
      float b1 = s;
      float b2 = 1.0f - t;
      float b3 = t;

      Vector3 q0 = p0 * b0 + p1 * b1;
      Vector3 q1 = p2 * b2 + p3 * b3;
      Vector3 n = q0 - q1;
      float dist = n.magnitude;
      Vector3.Normalize(n);
      float C = dist - restDist;
      Vector3 grad0 = n * b0;
      Vector3 grad1 = n * b1;
      Vector3 grad2 = -n * b2;
      Vector3 grad3 = -n * b3;

      s = w0 * b0*b0 + w1 * b1*b1 + w2 * b2*b2 + w3 * b3*b3;
      if (s == 0.0) return false;

      s = C / s;
      if (C < 0.0) s *= compressionStiffness;
      else s *= stretchStiffness;

      if (s == 0.0) return false;

      corr0 = -s * w0 * grad0;
      corr1 = -s * w1 * grad1;
      corr2 = -s * w2 * grad2;
      corr3 = -s * w3 * grad3;

      return true;
    }

  }
}