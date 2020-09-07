#include "./Utilities.cginc"

/*
functions that handles:
 - all contraints
 - position based dynamics algorithms
*/
bool ExternalForce(
  float dt,
  float3 p, float3 v, float w,
  float3 force,
  float damping,
  out float3 corr)
{
  corr = float3(0, 0, 0);
  if (w < EPSILON) return false;
  
  v += dt * w * force;
  v *= damping;
  corr = dt * v;
  return true;
}

void WindForce(
  float dt,
  float3 p0, float w0,
  float3 p1, float w1,
  float3 p2, float w2,
  float3 windDir,
  float windForce,
  out float3 corr0,
  out float3 corr1,
  out float3 corr2)
{
  float3 triDir = normalize(cross(p1-p0, p2-p0));
  float windFactor = dot(triDir, windDir);
  int dirCorr = windFactor > 0 ? 1 : -1;
  windFactor = windFactor > 0 ? windFactor : -windFactor;

  corr0 = triDir * dirCorr * windForce * windFactor * w0 * dt * dt;
  corr1 = triDir * dirCorr * windForce * windFactor * w1 * dt * dt;
  corr2 = triDir * dirCorr * windForce * windFactor * w2 * dt * dt;
}

bool DistanceConstraint(
  float3 p0, float w0,
  float3 p1, float w1,
  float restLength,
  float stretchStiffness,
  float compressionStiffness,
  out float3 corr0,
  out float3 corr1)
{
  corr0 = corr1 = float3(0, 0, 0);
  float wSum = w0 + w1;
  if (wSum < EPSILON) return false;

  float3 n = p0 - p1;
  float d = length(n);
  n = normalize(n);

  float3 corr;
  if (d < restLength) corr = compressionStiffness * n * (d - restLength) / wSum;
  else corr = stretchStiffness * n * (d - restLength) / wSum;

  corr0 = -w0 * corr;
  corr1 = w1 * corr;
  return true;
}

bool DihedralConstraint(
  float3 p0, float w0,
  float3 p1, float w1,
  float3 p2, float w2,
  float3 p3, float w3,
  float restAngle,
  float stiffness,
  out float3 corr0,
  out float3 corr1,
  out float3 corr2,
  out float3 corr3)
{
  corr0 = corr1 = corr2 = corr3 = float3(0, 0, 0);
  if (w0 == 0.0f && w1 == 0.0f) return false;

  float3 e = p3 - p2;
  float elen = length(e);
  if (elen < EPSILON) return false;

  float invElen = 1 / elen;

  float3 n1 = cross((p2 - p0), (p3 - p0)); n1 /= dot(n1, n1);
  float3 n2 = cross((p3 - p1), (p2 - p1)); n2 /= dot(n2, n2);

  float3 d0 = elen * n1;
  float3 d1 = elen * n2;
  float3 d2 = dot((p0 - p3), e) * invElen * n1 + dot((p1-p3), e) * invElen * n2;
  float3 d3 = dot((p2 - p0), e) * invElen * n1 + dot((p2-p1), e) * invElen * n2;

  n1 = normalize(n1);
  n2 = normalize(n2);
  float n1n2 = dot(n1, n2);

  if (n1n2 < -1.0f) n1n2 = -1.0f;
  if (n1n2 >  1.0f) n1n2 =  1.0f;
  float phi = acos(n1n2);	

  // float phi = (-0.6981317 * dot * dot - 0.8726646) * dot + 1.570796;	// fast approximation

  float lambda = 
    w0 * dot(d0, d0) +
    w1 * dot(d1, d1) +
    w2 * dot(d2, d2) +
    w3 * dot(d3, d3);

  if (lambda > -EPSILON && lambda < EPSILON) return false;	

  // stability
  // 1.5 is the largest magic number I found to be stable in all cases :-)
  //if (stiffness > 0.5 && fabs(phi - b.restAngle) > 1.5)		
  //	stiffness = 0.5;

  lambda = (phi - restAngle) / lambda * stiffness;

  if (dot(cross(n1, n2), e) > EPSILON) lambda = -lambda;	

  corr0 = - w0 * lambda * d0;
  corr1 = - w1 * lambda * d1;
  corr2 = - w2 * lambda * d2;
  corr3 = - w3 * lambda * d3;
  return true;
}

bool VolumeConstraint(
  float3 p0, float w0,
  float3 p1, float w1,
  float3 p2, float w2,
  float3 p3, float w3,
  float restVolume,
  float negVolumeStiffness,
  float posVolumeStiffness,
  out float3 corr0,
  out float3 corr1,
  out float3 corr2,
  out float3 corr3)
{
  corr0 = corr1 = corr2 = corr3 = float3(0, 0, 0);
  float volume = (1 / 6) * dot(cross((p1 - p0), (p2 - p0)), (p3 - p0));

  if (posVolumeStiffness == 0.0f && volume > 0.0f) return false;
  if (negVolumeStiffness == 0.0f && volume < 0.0f) return false;

  float3 grad0 = cross((p1 - p2), (p3 - p2));
  float3 grad1 = cross((p2 - p0), (p3 - p0));
  float3 grad2 = cross((p0 - p1), (p3 - p1));
  float3 grad3 = cross((p1 - p0), (p2 - p0));

  float lambda = 
    w0 * dot(grad0, grad0) +
    w1 * dot(grad1, grad1) +
    w2 * dot(grad2, grad2) +
    w3 * dot(grad3, grad3);

  float l = lambda < 0.0f ? -lambda : lambda;
  if (l < EPSILON) return false;

  if (volume < 0.0f) lambda = negVolumeStiffness * (volume - restVolume) / lambda;
  else lambda = posVolumeStiffness * (volume - restVolume) / lambda;

  corr0 = -lambda * w0 * grad0;
  corr1 = -lambda * w1 * grad1;
  corr2 = -lambda * w2 * grad2;
  corr3 = -lambda * w3 * grad3;

  return true;
}

bool EdgePointDistanceConstraint(
  float3 p, float w,
  float3 p0, float w0,
  float3 p1, float w1,
  float restDist,
  float compressionStiffness,
  float stretchStiffness,
  out float3 corr,
  out float3 corr0,
  out float3 corr1)
{
  corr = corr0 = corr1 = float3(0, 0, 0);

  float3 d = p1 - p0;
  float t;
  if (dot((p0 - p1), (p0 - p1)) < EPSILON * EPSILON) t = 0.5f;
  else
  {
    float d2 = dot(d, d);
    t = dot(d, (p - p1)) / d2;
    if (t < 0.0f) t = 0.0f;
    else if (t > 1.0f) t = 1.0f;
  }

  // closest point on edge
  float3 q = p0 + d*t;
  float3 n = p - q;
  float dist = length(n);
  n = normalize(n);
  float C = dist - restDist;
  float b0 = 1.0f - t;
  float b1 = t;

  float3 grad = n;
  float3 grad0 = -n * b0;
  float3 grad1 = -n * b1;

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

bool TrianglePointDistanceConstraint(
  float3 p, float w,
  float3 p0, float w0,
  float3 p1, float w1,
  float3 p2, float w2,
  float restDist,
  float compressionStiffness,
  float stretchStiffness,
  out float3 corr,
  out float3 corr0,
  out float3 corr1,
  out float3 corr2)
{
  corr = corr0 = corr1 = corr2 = float3(0, 0, 0);
  // find barycentric coordinates of closest point on triangle

  // for singular case
  float b0 = 1.0f / 3.0f;
  float b1 = b0;
  float b2 = b0;

  float a, b, c, d, e, f;
  float det;

  float3 d1 = p1 - p0;
  float3 d2 = p2 - p0;
  float3 pp0 = p - p0;
  a = dot(d1, d1);
  b = dot(d2, d1);
  c = dot(pp0, d1);
  d = b;
  e = dot(d2, d2);
  f = dot(pp0, d2);
  det = a*e - b*d;

  float s, t;
  float3 dist;
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
      dist2 = dot(dist, dist);
      t = (dist2 == 0.0f) ? 0.5f : dot(dist, (p - p1)) / dist2;
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
      dist2 = dot(dist, dist);
      t = (dist2 == 0.0f) ? 0.5f : dot(dist, (p - p2)) / dist2;
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
      dist2 = dot(dist, dist);
      t = (dist2 == 0.0f) ? 0.5f : dot(dist, (p - p0)) / dist2;
      if (t < 0.0) t = 0.0f;	// on point 0
      if (t > 1.0) t = 1.0f;	// on point 1
      b2 = 0.0f;
      b0 = (1.0f - t);
      b1 = t;
    }
  }
  float3 q = p0 * b0 + p1 * b1 + p2 * b2;
  float3 n = p - q;
  float l = length(n);
  n = normalize(n);
  float C = l - restDist;
  float3 grad = n;
  float3 grad0 = -n * b0;
  float3 grad1 = -n * b1;
  float3 grad2 = -n * b2;

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

bool EdgeEdgeDistanceConstraint(
  float3 p0, float w0,
  float3 p1, float w1,
  float3 p2, float w2,
  float3 p3, float w3,
  float restDist,
  float compressionStiffness,
  float stretchStiffness,
  out float3 corr0,
  out float3 corr1,
  out float3 corr2,
  out float3 corr3)
{
  corr0 = corr1 = corr2 = corr3 = float3(0, 0, 0);
  float3 d0 = p1 - p0;
  float3 d1 = p3 - p2;

  float a, b, c, d, e, f;

  a = dot(d0, d0);
  b = -dot(d0, d1);
  c = dot(d0, d1);
  d = -dot(d1, d1);
  e = dot((p2 - p0), d0);
  f = dot((p2 - p0), d1);
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
    float s0 = dot(p0, d0);
    float s1 = dot(p1, d0);
    float t0 = dot(p2, d0);
    float t1 = dot(p3, d0);
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

  float3 q0 = p0 * b0 + p1 * b1;
  float3 q1 = p2 * b2 + p3 * b3;
  float3 n = q0 - q1;
  float dist = length(n);
  n = normalize(n);
  float C = dist - restDist;
  float3 grad0 = n * b0;
  float3 grad1 = n * b1;
  float3 grad2 = -n * b2;
  float3 grad3 = -n * b3;

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