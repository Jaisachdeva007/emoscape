using UnityEngine;

namespace EmoScape.Shared
{
    /// <summary>
    /// Direct port of THREE.CatmullRomCurve3(points, closed=false, curveType='catmullrom', tension=0.5)
    /// as used by frontend/index.html and frontend/room.html to build the emotional spline.
    /// </summary>
    public class CatmullRomSpline3D
    {
        readonly Vector3[] points;
        readonly float tension;

        public CatmullRomSpline3D(Vector3[] points, float tension = 0.5f)
        {
            this.points = points;
            this.tension = tension;
        }

        public Vector3 GetPoint(float t)
        {
            int l = points.Length;
            if (l == 1) return points[0];

            float p = (l - 1) * Mathf.Clamp01(t);
            int intPoint = Mathf.FloorToInt(p);
            float weight = p - intPoint;

            if (Mathf.Approximately(weight, 0f) && intPoint == l - 1)
            {
                intPoint = l - 2;
                weight = 1f;
            }

            Vector3 p0 = intPoint > 0 ? points[intPoint - 1] : points[0] - points[1] + points[0];
            Vector3 p1 = points[intPoint];
            Vector3 p2 = points[Mathf.Min(intPoint + 1, l - 1)];
            Vector3 p3 = intPoint + 2 < l ? points[intPoint + 2] : points[l - 1] - points[l - 2] + points[l - 1];

            return new Vector3(
                CatmullRomComponent(weight, p0.x, p1.x, p2.x, p3.x),
                CatmullRomComponent(weight, p0.y, p1.y, p2.y, p3.y),
                CatmullRomComponent(weight, p0.z, p1.z, p2.z, p3.z)
            );
        }

        float CatmullRomComponent(float t, float p0, float p1, float p2, float p3)
        {
            float v0 = (p2 - p0) * tension;
            float v1 = (p3 - p1) * tension;
            float t2 = t * t;
            float t3 = t * t2;
            return (2 * p1 - 2 * p2 + v0 + v1) * t3 + (-3 * p1 + 3 * p2 - 2 * v0 - v1) * t2 + v0 * t + p1;
        }

        /// <summary>Samples `samples+1` points evenly between global parameters tStart and tEnd.</summary>
        public static Vector3[] SampleRange(Vector3[] controlPoints, float tStart, float tEnd, int samples, float tension = 0.5f)
        {
            var spline = new CatmullRomSpline3D(controlPoints, tension);
            var result = new Vector3[samples + 1];
            for (int j = 0; j <= samples; j++)
            {
                float t = Mathf.Lerp(tStart, tEnd, j / (float)samples);
                result[j] = spline.GetPoint(Mathf.Min(t, 1f));
            }
            return result;
        }
    }
}
