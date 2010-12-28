using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

namespace Rhino
{
  //public class ON_TensorProduct { } never seen this used
  //  public class ON_CageMorph { }
}
namespace Rhino.Geometry
{
  public class NurbsSurface : Surface
  {
    #region static create functions
    public static NurbsSurface Create(int dimension, bool isRational, int order0, int order1, int controlPointCount0, int controlPointCount1)
    {
      if (dimension < 1 || order0 < 2 || order1 < 2 || controlPointCount0 < order0 || controlPointCount1 < order1)
        return null;
      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_New(dimension, isRational, order0, order1, controlPointCount0, controlPointCount1);
      if (IntPtr.Zero == ptr)
        return null;
      return new NurbsSurface(ptr, null, null);
    }

    public static NurbsSurface CreateFromCone(Cone cone)
    {
      IntPtr pNurbsSurface = UnsafeNativeMethods.ON_Cone_GetNurbForm(ref cone);
      if (IntPtr.Zero == pNurbsSurface)
        return null;
      return new NurbsSurface(pNurbsSurface, null, null);
    }
    public static NurbsSurface CreateFromCylinder(Cylinder cylinder)
    {
      IntPtr pNurbsSurface = UnsafeNativeMethods.ON_Cylinder_GetNurbForm(ref cylinder);
      if (IntPtr.Zero == pNurbsSurface)
        return null;
      return new NurbsSurface(pNurbsSurface, null, null);
    }
    public static NurbsSurface CreateFromSphere(Sphere sphere)
    {
      IntPtr pNurbsSurface = UnsafeNativeMethods.ON_Sphere_GetNurbsForm(ref sphere);
      if (IntPtr.Zero == pNurbsSurface)
        return null;
      return new NurbsSurface(pNurbsSurface, null, null);
    }
    public static NurbsSurface CreateFromTorus(Torus torus)
    {
      IntPtr pNurbsSurface = UnsafeNativeMethods.ON_Torus_GetNurbForm(ref torus);
      if (IntPtr.Zero == pNurbsSurface)
        return null;
      return new NurbsSurface(pNurbsSurface, null, null);
    }

    /// <summary>
    /// Create a surface from control-points.
    /// </summary>
    /// <param name="points">Control point locations.</param>
    /// <param name="uCount">Number of points in U direction.</param>
    /// <param name="vCount">Number of points in V direction.</param>
    /// <param name="uDegree">Degree of surface in U direction.</param>
    /// <param name="vDegree">Degree of surface in V direction.</param>
    /// <returns>A NurbsSurface on success or null on failure.</returns>
    /// <remarks>uCount multiplied by vCount must equal the number of points supplied.</remarks>
    public static NurbsSurface CreateFromPoints(IEnumerable<Point3d> points, int uCount, int vCount, int uDegree, int vDegree)
    {
      if (null == points) { throw new ArgumentNullException("points"); }

      int total_count = 0;
      Point3d[] ptArray = Rhino.Collections.Point3dList.GetConstPointArray(points, out total_count);
      if (total_count < 4)
      {
        throw new InvalidOperationException("Insufficient points for a nurbs surface");
      }

      if ((uCount * vCount) != total_count)
      {
        throw new InvalidOperationException("Invalid U and V counts.");
      }

      uDegree = Math.Max(uDegree, 1);
      uDegree = Math.Min(uDegree, 11);
      uDegree = Math.Min(uDegree, uCount - 1);
      vDegree = Math.Max(vDegree, 1);
      vDegree = Math.Min(vDegree, 11);
      vDegree = Math.Min(vDegree, vCount - 1);

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_SurfaceFromPoints(ptArray, uCount, vCount, uDegree, vDegree);

      if (IntPtr.Zero == ptr) { return null; }
      return new NurbsSurface(ptr, null, null);
    }
    /// <summary>
    /// Create a surface from control-points.
    /// </summary>
    /// <param name="points">Control point locations.</param>
    /// <param name="uCount">Number of points in U direction.</param>
    /// <param name="vCount">Number of points in V direction.</param>
    /// <param name="uDegree">Degree of surface in U direction.</param>
    /// <param name="vDegree">Degree of surface in V direction.</param>
    /// <param name="uClosed">True if the surface should be closed in the U direction.</param>
    /// <param name="vClosed">True if the surface should be closed in the V direction.</param>
    /// <returns>A NurbsSurface on success or null on failure.</returns>
    /// <remarks>uCount multiplied by vCount must equal the number of points supplied.</remarks>
    public static NurbsSurface CreateThroughPoints(IEnumerable<Point3d> points, int uCount, int vCount, int uDegree, int vDegree, bool uClosed, bool vClosed)
    {
      if (null == points) { throw new ArgumentNullException("points"); }

      int total_count = 0;
      Point3d[] ptArray = Rhino.Collections.Point3dList.GetConstPointArray(points, out total_count);
      if (total_count < 4)
      {
        throw new InvalidOperationException("Insufficient points for a nurbs surface");
      }

      if ((uCount * vCount) != total_count)
      {
        throw new InvalidOperationException("Invalid U and V counts.");
      }

      uDegree = Math.Max(uDegree, 1);
      uDegree = Math.Min(uDegree, 11);
      uDegree = Math.Min(uDegree, uCount - 1);
      vDegree = Math.Max(vDegree, 1);
      vDegree = Math.Min(vDegree, 11);
      vDegree = Math.Min(vDegree, vCount - 1);

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_SurfaceThroughPoints(ptArray, uCount, vCount, uDegree, vDegree, uClosed, vClosed);
      if (IntPtr.Zero == ptr)
        return null;
      return new NurbsSurface(ptr, null, null);
    }

    /// <summary>
    /// Create a Ruled surface between two curves. Curves must share the same knot-vector.
    /// </summary>
    /// <param name="curveA">First curve.</param>
    /// <param name="curveB">Second curve.</param>
    /// <returns>A ruled surface on success or null on failure.</returns>
    public static NurbsSurface CreateRuledSurface(Curve curveA, Curve curveB)
    {
      if (curveA == null) { throw new ArgumentNullException("curveA"); }
      if (curveB == null) { throw new ArgumentNullException("curveB"); }

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_CreateRuledSurface(curveA.ConstPointer(), curveB.ConstPointer());

      if (ptr == IntPtr.Zero)
        return null;
      return new NurbsSurface(ptr, null, null);
    }

    /// <summary>
    /// Make a surface from 4 corner points
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    /// <param name="corner3"></param>
    /// <param name="corner4"></param>
    /// <returns>the resulting surface or null on error</returns>
    public static NurbsSurface CreateFromCorners(Point3d corner1, Point3d corner2, Point3d corner3, Point3d corner4)
    {
      return CreateFromCorners(corner1, corner2, corner3, corner4, 0.0);
    }
    /// <summary>
    /// Make a surface from 4 corner points
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    /// <param name="corner3"></param>
    /// <param name="corner4"></param>
    /// <param name="tolerance">minimum edge length without collapsing to a singularity</param>
    /// <returns>the resulting surface or null on error</returns>
    public static NurbsSurface CreateFromCorners(Point3d corner1, Point3d corner2, Point3d corner3, Point3d corner4, double tolerance)
    {
      IntPtr pSurface = UnsafeNativeMethods.RHC_RhinoCreateSurfaceFromCorners(corner1, corner2, corner3, corner4, tolerance);
      if (IntPtr.Zero == pSurface)
        return null;
      return new NurbsSurface(pSurface, null, null);
    }
    /// <summary>
    /// Make a surface from 3 corner points
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    /// <param name="corner3"></param>
    /// <returns>the resulting surface or null on error</returns>
    public static NurbsSurface CreateFromCorners(Point3d corner1, Point3d corner2, Point3d corner3)
    {
      return CreateFromCorners(corner1, corner2, corner3, corner3, 0.0);
    }

    /// <summary>
    /// Creates a railed Surface-of-Revolution.
    /// </summary>
    /// <param name="profile">Profile curve for revolution.</param>
    /// <param name="rail">Rail curve for revolution.</param>
    /// <param name="axis">Axis of revolution.</param>
    /// <param name="scaleHeight">If true, surface will be locally scaled.</param>
    /// <returns>A NurbsSurface or null on failure.</returns>
    public static NurbsSurface CreateRailRevolvedSurface(Curve profile, Curve rail, Line axis, bool scaleHeight)
    {
      IntPtr pConstProfile = profile.ConstPointer();
      IntPtr pConstRail = rail.ConstPointer();
      IntPtr pNurbsSurface = UnsafeNativeMethods.RHC_RhinoRailRevolve(pConstProfile, pConstRail, ref axis, scaleHeight);
      if (IntPtr.Zero == pNurbsSurface)
        return null;
      return new NurbsSurface(pNurbsSurface, null, null);
    }
    #endregion

    #region constructors

    internal NurbsSurface(IntPtr ptr, Rhino.DocObjects.RhinoObject parent_object, Rhino.DocObjects.ObjRef obj_ref)
      : base(ptr, parent_object, obj_ref)
    { }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new NurbsSurface(IntPtr.Zero, null, null);
    }
    #endregion

    #region properties
    private Collections.NurbsSurfaceKnotList m_KnotsU;
    private Collections.NurbsSurfaceKnotList m_KnotsV;
    /// <summary>
    /// The U direction knot vector
    /// </summary>
    public Collections.NurbsSurfaceKnotList KnotsU
    {
      get
      {
        if (m_KnotsU == null)
          m_KnotsU = new Rhino.Geometry.Collections.NurbsSurfaceKnotList(this, 0);
        return m_KnotsU;
      }
    }

    /// <summary>
    /// The V direction knot vector
    /// </summary>
    public Collections.NurbsSurfaceKnotList KnotsV
    {
      get
      {
        if (m_KnotsV == null)
          m_KnotsV = new Rhino.Geometry.Collections.NurbsSurfaceKnotList(this, 1);
        return m_KnotsV;
      }
    }

    private Collections.NurbsSurfacePointList m_Points;
    public Collections.NurbsSurfacePointList Points
    {
      get
      {
        if (m_Points == null)
          m_Points = new Rhino.Geometry.Collections.NurbsSurfacePointList(this);
        return m_Points;
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the nurbs surface is rational.
    /// </summary>
    public bool IsRational
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_GetBool(ptr, idxIsRational);
      }
    }

    #endregion

    // GetBool indices
    const int idxIsRational = 0;
    const int idxIsClampedStart = 1;
    const int idxIsClampedEnd = 2;
    const int idxZeroCVs = 3;
    const int idxClampStart = 4;
    const int idxClampEnd = 5;
    const int idxMakeRational = 6;
    const int idxMakeNonRational = 7;
    const int idxHasBezierSpans = 8;

    // GetInt indices
    const int idxCVSize = 0;
    const int idxOrder = 1;
    internal const int idxCVCount = 2;
    internal const int idxKnotCount = 3;
    const int idxCVStyle = 4;

    internal int GetIntDir(int which, int direction)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_GetIntDir(ptr, which, direction);
    }

  }
  //  public class ON_NurbsCage : ON_Geometry { }
  //  public class ON_MorphControl : ON_Geometry { }
}


namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to the control points of a nurbs surface.
  /// </summary>
  public sealed class NurbsSurfacePointList
  {
    private NurbsSurface m_surface;

    #region constructors
    internal NurbsSurfacePointList(NurbsSurface ownerSurface)
    {
      m_surface = ownerSurface;
    }
    #endregion

    public void EnsurePrivateCopy()
    {
      m_surface.EnsurePrivateCopy();
    }

    #region properties
    /// <summary>
    /// Gets the number of control points in the 'U' direction of this surface.
    /// </summary>
    public int CountU
    {
      get
      {
        return m_surface.GetIntDir(NurbsSurface.idxCVCount, 0);
      }
    }

    /// <summary>
    /// Gets the number of control points in the 'V' direction of this surface.
    /// </summary>
    public int CountV
    {
      get
      {
        return m_surface.GetIntDir(NurbsSurface.idxCVCount, 1);
      }
    }
    #endregion

    /// <summary>
    /// Get the Greville point (u, v) coordinates associated with the control point at the given indices.
    /// </summary>
    /// <param name="u">Index of control-point along surface 'U' direction.</param>
    /// <param name="v">Index of control-point along surface 'U' direction.</param>
    /// <returns>A Surface UV coordinate on success, Point2d.Unset on failure.</returns>
    public Point2d GetGrevillePoint(int u, int v)
    {
      if (u < 0) { throw new IndexOutOfRangeException("u must be larger than or equal to zero."); }
      if (v < 0) { throw new IndexOutOfRangeException("v must be larger than or equal to zero."); }
      if (u >= CountU) { throw new IndexOutOfRangeException("u must be less than CountU."); }
      if (v >= CountV) { throw new IndexOutOfRangeException("v must be less than CountV."); }

      IntPtr ptr = m_surface.ConstPointer();
      Point2d gp = Point2d.Unset;

      UnsafeNativeMethods.ON_NurbsSurface_GetGrevillePoint(ptr, u, v, ref gp);
      return gp;
    }

    /// <summary>
    /// Gets the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control-point along surface 'U' direction.</param>
    /// <param name="v">Index of control-point along surface 'U' direction.</param>
    /// <returns>The control point at the given (u, v) index.</returns>
    public ControlPoint GetControlPoint(int u, int v)
    {
      if (u < 0) { throw new IndexOutOfRangeException("u must be larger than or equal to zero."); }
      if (v < 0) { throw new IndexOutOfRangeException("v must be larger than or equal to zero."); }
      if (u >= CountU) { throw new IndexOutOfRangeException("u must be less than CountU."); }
      if (v >= CountV) { throw new IndexOutOfRangeException("v must be less than CountV."); }

      Point4d pt = new Point4d();
      IntPtr ptr = m_surface.ConstPointer();
      if (UnsafeNativeMethods.ON_NurbsSurface_GetCV(ptr, u, v, ref pt))
        return new ControlPoint(pt);

      return ControlPoint.Unset;
    }

    /// <summary>
    /// Sets the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control-point along surface 'U' direction.</param>
    /// <param name="v">Index of control-point along surface 'U' direction.</param>
    /// <param name="cp">The control point location to set (weight is assumed to be 1.0).</param>
    /// <returns>True on success, false on failure.</returns>
    public bool SetControlPoint(int u, int v, Point3d cp)
    {
      return SetControlPoint(u, v, new ControlPoint(cp));
    }

    /// <summary>
    /// Sets the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control-point along surface 'U' direction.</param>
    /// <param name="v">Index of control-point along surface 'U' direction.</param>
    /// <param name="cp">The control point to set.</param>
    /// <returns>True on success, false on failure.</returns>
    public bool SetControlPoint(int u, int v, ControlPoint cp)
    {
      if (u < 0) { throw new IndexOutOfRangeException("u must be larger than or equal to zero."); }
      if (v < 0) { throw new IndexOutOfRangeException("v must be larger than or equal to zero."); }
      if (u >= CountU) { throw new IndexOutOfRangeException("u must be less than CountU."); }
      if (v >= CountV) { throw new IndexOutOfRangeException("v must be less than CountV."); }

      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_SetCV(ptr, u, v, ref cp.m_vertex);
    }
  }
  /// <summary>
  /// Provides access to the knot vector of a nurbs surface.
  /// </summary>
  public sealed class NurbsSurfaceKnotList : IEnumerable<double>
  {
    private NurbsSurface m_surface;
    private int m_direction;

    #region constructors
    internal NurbsSurfaceKnotList(NurbsSurface ownerSurface, int direction)
    {
      m_surface = ownerSurface;
      m_direction = direction;
    }
    #endregion

    #region properties

    /// <summary>Total number of knots in this curve.</summary>
    public int Count
    {
      get
      {
        return m_surface.GetIntDir(NurbsSurface.idxKnotCount, m_direction);
      }
    }

    /// <summary>
    /// Gets or sets the knot vector value at the given index.
    /// </summary>
    /// <param name="index">Index of knot to access.</param>
    /// <returns>The knot value at [index]</returns>
    public double this[int index]
    {
      get
      {
        if (index < 0) { throw new IndexOutOfRangeException("Index must be larger than or equal to zero."); }
        if (index >= Count) { throw new IndexOutOfRangeException("Index must be less than the number of knots."); }
        IntPtr ptr = m_surface.ConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_Knot(ptr, m_direction, index);
      }
      set
      {
        if (index < 0) { throw new IndexOutOfRangeException("Index must be larger than or equal to zero."); }
        if (index >= Count) { throw new IndexOutOfRangeException("Index must be less than the number of knots."); }
        IntPtr ptr = m_surface.NonConstPointer();
        UnsafeNativeMethods.ON_NurbsSurface_SetKnot(ptr, m_direction, index, value);
      }
    }
    #endregion

    #region knot utility methods

    public void EnsurePrivateCopy()
    {
      m_surface.EnsurePrivateCopy();
    }

    /// <summary>
    /// Insert a knot and update control point locations.
    /// Does not change parameterization or locus of curve.
    /// </summary>
    /// <param name="value">Knot value to insert.</param>
    /// <returns>True on success, false on failure.</returns>
    public bool InsertKnot(double value)
    {
      return InsertKnot(value, 1);
    }

    /// <summary>
    /// Insert a knot and update control point locations.
    /// Does not change parameterization or locus of curve.
    /// </summary>
    /// <param name="value">Knot value to insert.</param>
    /// <param name="multiplicity">Multiplicity of knot to insert.</param>
    /// <returns>True on success, false on failure.</returns>
    public bool InsertKnot(double value, int multiplicity)
    {
      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_InsertKnot(ptr, m_direction, value, multiplicity);
    }

    /// <summary>Get knot multiplicity</summary>
    /// <param name="index">Index of knot to query.</param>
    /// <returns>The multiplicity (valence) of the knot.</returns>
    public int KnotMultiplicity(int index)
    {
      IntPtr ptr = m_surface.ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_KnotMultiplicity(ptr, m_direction, index);
    }

    /// <summary>
    /// Compute a clamped, uniform knot vector based on the current
    /// degree and control point count. Does not change values of control
    /// vertices.
    /// </summary>
    /// <param name="knotSpacing">Spacing of subsequent knots.</param>
    /// <returns>True on success, False on failure.</returns>
    public bool CreateUniformKnots(double knotSpacing)
    {
      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_MakeUniformKnotVector(ptr, m_direction, knotSpacing, true);
    }

    /// <summary>
    /// Compute a clamped, uniform, periodic knot vector based on the current
    /// degree and control point count. Does not change values of control
    /// vertices.
    /// </summary>
    /// <param name="knotSpacing">Spacing of subsequent knots.</param>
    /// <returns>True on success, False on failure.</returns>
    public bool CreatePeriodicKnots(double knotSpacing)
    {
      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_MakeUniformKnotVector(ptr, m_direction, knotSpacing, false);
    }
    #endregion

    #region IEnumerable<double> Members
    IEnumerator<double> IEnumerable<double>.GetEnumerator()
    {
      return new KVEnum(this);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return new KVEnum(this);
    }

    private class KVEnum : IEnumerator<double>
    {
      #region members
      private NurbsSurfaceKnotList m_surface_kv;
      int position = -1;
      #endregion

      #region constructor
      public KVEnum(NurbsSurfaceKnotList surface_cv)
      {
        m_surface_kv = surface_cv;
      }
      #endregion

      #region enumeration logic
      public bool MoveNext()
      {
        position++;
        return (position < m_surface_kv.Count);
      }
      public void Reset()
      {
        position = -1;
      }

      public double Current
      {
        get
        {
          try
          {
            return m_surface_kv[position];
          }
          catch (IndexOutOfRangeException)
          {
            throw new InvalidOperationException();
          }
        }
      }
      object IEnumerator.Current
      {
        get
        {
          try
          {
            return m_surface_kv[position];
          }
          catch (IndexOutOfRangeException)
          {
            throw new InvalidOperationException();
          }
        }
      }
      #endregion

      #region IDisposable logic
      private bool m_disposed; // = false; <- set by framework
      public void Dispose()
      {
        if (m_disposed) { return; }
        m_disposed = true;

        GC.SuppressFinalize(this);
      }
      #endregion
    }
    #endregion
  }
}