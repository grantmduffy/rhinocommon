using System;
using System.Collections.Generic;

namespace MethodGen
{
  class NativeMethodDeclares
  {
    readonly List<DeclarationList> m_declarations = new List<DeclarationList>();
    public bool Write(string path, string libname)
    {
      System.IO.StreamWriter sw = new System.IO.StreamWriter(path);
      sw.Write(
@"// !!!DO NOT EDIT THIS FILE BY HAND!!!
// Create this file by running MethodGen.exe in the rhinocommon directory
// MethodGen.exe parses the cpp files in rhcommon_c to create C# callable
// function declarations

using System;
using System.Runtime.InteropServices;
");
      if (Program.m_includeRhinoDeclarations)
      {
        sw.Write(
@"using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Collections;
using Rhino.Display;
using Rhino.Runtime.InteropWrappers;
");
      }

      foreach (string using_statement in Program.m_extra_usings)
      {
        sw.Write(using_statement);
      }

      sw.Write(
@"
// Atuomatically generated function declarations for calling into
// the support 'C' DLL (rhcommon_c.dll).
");

      if (!string.IsNullOrEmpty(Program.m_namespace))
      {
        sw.Write(string.Format("namespace {0}\r\n{{\r\n", Program.m_namespace));
      }
      sw.Write(
@"internal partial class UnsafeNativeMethods
{
");
      if( !libname.EndsWith("rdk"))
        sw.Write(
@"  private UnsafeNativeMethods(){}
");
      for (int i = 0; i < m_declarations.Count; i++)
      {
        if (m_declarations[i].Write(sw, libname))
        {
          sw.WriteLine();
          sw.WriteLine();
        }
      }
      sw.WriteLine("}");
      if (!string.IsNullOrEmpty(Program.m_namespace))
        sw.Write("}");

      sw.Close();
      return true;
    }

    public bool BuildDeclarations(string cppFilePath)
    {
      DeclarationList d = DeclarationList.Construct(cppFilePath);
      if (d!=null)
        m_declarations.Add(d);
      return (d != null);
    }
  }


  class DeclarationList
  {
    string m_source_filename;
    readonly List<Declaration> m_declarations = new List<Declaration>();
    readonly List<EnumDeclaration> m_enums = new List<EnumDeclaration>();

    public static DeclarationList Construct(string cppFileName)
    {
      const string EXPORT_DECLARE = "RH_C_FUNCTION";
      const string MANUAL = "/*MANUAL*/";
      DeclarationList d = new DeclarationList();
      d.m_source_filename = cppFileName;
      string sourceCode = System.IO.File.ReadAllText(cppFileName);
      // If you have multi-line comments that need to be dealt with, this would probably suffice:
      // sourceCode = System.Text.RegularExpressions.Regex.Replace(sourceCode, @"/\*.*?\*/", "");
      List<int> startIndices = new List<int>();
      int previous_index = -1;
      while (true)
      {
        int index = sourceCode.IndexOf(EXPORT_DECLARE, previous_index + 1);
        if (-1 == index)
          break;
        previous_index = index;
        // make sure this function is not commented out
        // walk backward to the newline and try to find a //
        if (index > 2)
        {
          bool skipThisDeclaration = false;
          int testIndex = index - 1;
          while (testIndex > 0)
          {
            if (sourceCode[testIndex] == '/' && sourceCode[testIndex - 1] == '/')
            {
              skipThisDeclaration = true;
              break;
            }
            if (sourceCode[testIndex] == '#')
            {
              skipThisDeclaration = true;
              break;
            }
            if (sourceCode[testIndex] == '\n')
              break;
            testIndex--;
          }
          if (skipThisDeclaration)
            continue;
        }
        //if (index > 2 && sourceCode[index - 1] == '/' && sourceCode[index - 2] == '/')
        //  continue;
        // make sure this function is not defined as a MANUAL definition
        if (index > MANUAL.Length)
        {
          int manStart = index - MANUAL.Length;
          if (sourceCode.Substring(manStart, MANUAL.Length) == MANUAL)
            continue;
        }
        startIndices.Add(index);
      }

      // add all of the c function declarations to the cdecls list
      for (int i = 0; i < startIndices.Count; i++)
      {
        int start = startIndices[i] + EXPORT_DECLARE.Length;
        int end = sourceCode.IndexOf(')', start) + 1;
        string decl = sourceCode.Substring(start, end - start);
        decl = decl.Trim();
        d.m_declarations.Add(new Declaration(decl));
      }

      // walk through file and attempt to find all enum declarations
      previous_index = -1;
      while (true)
      {
        int index = sourceCode.IndexOf("enum ", previous_index + 1);
        if (-1 == index)
          break;
        previous_index = index;

        // now see if the enum word is a declaration or inside a function declaration
        int colon_index = sourceCode.IndexOf(':', index);
        int brace_index = sourceCode.IndexOf('{', index);
        int paren_index = sourceCode.IndexOf(')', index);
        if (paren_index < colon_index || brace_index < colon_index)
          continue;

        int semi_colon = sourceCode.IndexOf(';', index);
        if (colon_index == -1 || semi_colon == -1 || brace_index == -1)
          continue;

        string enumdecl = sourceCode.Substring(index, semi_colon - index + 1);
        d.m_enums.Add(new EnumDeclaration(enumdecl));
      }
      return d;
    }

    public bool Write(System.IO.StreamWriter sw, string libname)
    {
      if (m_declarations.Count < 1)
        return false;

      string filename = System.IO.Path.GetFileName(m_source_filename);
      sw.WriteLine("  #region " + filename);
      for (int i = 0; i < m_declarations.Count; i++)
      {
        if (i > 0)
          sw.WriteLine();
        m_declarations[i].Write(sw, libname);
      }
      for (int i = 0; i < m_enums.Count; i++)
      {
        sw.WriteLine();
        m_enums[i].Write(sw);
      }
      sw.WriteLine("  #endregion");
      return true;
    }

    class Declaration
    {
      readonly string m_cdecl;
      public Declaration(string cdecl)
      {
        m_cdecl = cdecl;
      }

      public void Write(System.IO.StreamWriter sw, string libname)
      {
        sw.WriteLine("  //" + m_cdecl.Replace("\n","\n  //"));
        //If this function contains a "PROC" parameter, don't wrap for now.
        //These functions need to be addressed individually
        int parameterStart = m_cdecl.IndexOf('(');
        int parameterEnd = m_cdecl.IndexOf(')');
        string p = m_cdecl.Substring(parameterStart, parameterEnd - parameterStart);
        if (p.Contains("PROC"))
        {
          sw.WriteLine("  // SKIPPING - Contains a function pointer which needs to be written by hand");
          return;
        }


        sw.WriteLine("  [DllImport(Import."+libname+", CallingConvention=CallingConvention.Cdecl )]");
        string retType = GetReturnType(true);
        if (string.Compare(retType, "bool")==0)
          sw.WriteLine("  [return: MarshalAs(UnmanagedType.U1)]");
        sw.Write("  internal static extern ");
        sw.Write(retType);
        sw.Write(" ");
        sw.Write(GetFunctionName());
        sw.Write("(");
        int paramCount = GetParameterCount();
        for (int i = 0; i < paramCount; i++)
        {
          if (i > 0)
            sw.Write(", ");
          string paramType, paramName;
          GetParameter(i, true, out paramType, out paramName);
          if (paramType.Equals("bool"))
            sw.Write("[MarshalAs(UnmanagedType.U1)]");
          else if (paramType.Equals("string"))
            sw.Write("[MarshalAs(UnmanagedType.LPWStr)]");

          sw.Write(paramType);
          sw.Write(" ");
          sw.Write(paramName);
        }
        sw.WriteLine(");");
      }

      bool GetParameter(int which, bool asCSharp, out string paramType, out string paramName)
      {
        bool rc = false;
        paramType = null;
        paramName = null;
        int start = m_cdecl.IndexOf('(') + 1;
        int end = m_cdecl.IndexOf(')');
        string all_parameters = m_cdecl.Substring(start, end - start);
        if (all_parameters.Length > 0)
        {
          string[] p = all_parameters.Split(new char[] { ',' });
          string cparam = p[which].Trim();

          end = cparam.Length;
          start = cparam.LastIndexOf(' ');
          paramName = cparam.Substring(start, end - start);
          paramName = paramName.Trim();

          paramType = cparam.Substring(0, start);
          paramType = paramType.Trim();
          if (asCSharp)
          {
            bool isArray = paramType.StartsWith("/*ARRAY*/", StringComparison.OrdinalIgnoreCase);
            if( isArray )
              paramType = paramType.Substring("/*ARRAY*/".Length).Trim();
            paramType = ParameterTypeAsCSharp(paramType, isArray);
          }
          rc = true;
        }
        return rc;

      }

      static string ParameterTypeAsCSharp(string ctype, bool isArray)
      {
        if (ctype.StartsWith("enum ", StringComparison.InvariantCulture))
          return ctype.Substring("enum ".Length).Trim();

        // 2010-08-03, Brian Gillespie
        // Moved const check outside the if statements to support
        // const basic types: const int, const unsigned int, etc.
        bool isConst = false;
        string sType = ctype;
        if (sType.StartsWith("const "))
        {
          sType = sType.Substring("const ".Length).Trim();
          isConst = true;
        }

        if (sType.Contains("RHMONO_STRING"))
          return "string";

        if (sType.Equals("HWND") || sType.Equals("HBITMAP") || sType.Equals("HCURSOR") || sType.Equals("HICON") || sType.Equals("HBRUSH") || sType.Equals("HFONT") || sType.Equals("HMENU") || sType.Equals("HDC"))
          return "IntPtr";

        if (sType.EndsWith("**"))
          return "ref IntPtr";

        if (sType.EndsWith("*"))
        {
          string s = sType.Substring(0,sType.Length-1).Trim();
          //bool isConst = false;
          //if (s.StartsWith("const "))
          //{
          //  s = s.Substring("const ".Length).Trim();
          //  isConst = true;
          //}
          s = ParameterTypeAsCSharp(s, isArray);
          
          if (s.Equals("int") || s.Equals("uint") || s.Equals("double") || s.Equals("float") || s.Equals("Guid") ||
              s.Equals("short") || s.Equals("Int64") || s.Equals("byte"))
          {
            if (isArray)
            {
              if (isConst)
                return s + "[]";
              else
                return "[In,Out] " + s + "[]";
            }
            s = "ref " + s;
            return s;
          }

          if (s.Equals("bool"))
          {
            if (isArray)
            {
              if (isConst)
                return "[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1)] " + s + "[]";
              else
                return "[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.U1), In, Out] " + s + "[]";
            }
            s = "[MarshalAs(UnmanagedType.U1)]ref " + s;
            return s;
          }

          if (s.Equals("ON_Plane"))
            return "ERROR_DO_NOT_USE_ON_PLANE";
          if (s.Equals("ON_Circle"))
            return "ERROR_DO_NOT_USE_ON_CIRCLE";

          if (s.Equals("ON_Arc") ||
              s.Equals("ON_BoundingBox") ||
              s.Equals("ON_Sphere") ||
              s.Equals("ON_Line") ||
              s.Equals("ON_Interval") ||
              s.Equals("ON_Cylinder") ||
              s.Equals("ON_Cone") ||
              s.Equals("ON_Torus") ||
              s.Equals("ON_Ellipse") ||
              s.Equals("ON_Quaternion")
            )
          {
            if (isArray)
            {
              s = s.Substring("ON_".Length);
              if (isConst)
                return s+"[]";
              else
                return "[In,Out] "+s+"[]";
            }
            s = "ref " + s.Substring("ON_".Length);
            return s;
          }

          if (s.Equals("ON_COMPONENT_INDEX"))
            return "ref ComponentIndex";

          if (s.Equals("ON_Xform") || s.Equals("AR_Transform"))
            return "ref Transform";

          if (s.Equals("ON_2fPoint") || s.Equals("AR_2fPoint"))
            return "ref Point2f";

          if (s.Equals("PointF"))
          {
            if (isArray)
            {
              if (isConst)
                return "PointF[]";
              else
                return "[In,Out] PointF[]";
            }
          }

          if (s.Equals("ON_2dPoint") || s.Equals("AR_2dPoint"))
          {
            if (isArray)
            {
              if (isConst)
                return "Point2d[]";
              else
                return "[In,Out] Point2d[]";
            }
            return "ref Point2d";
          }

          if (s.Equals("ON_2dVector") || s.Equals("AR_2dVector"))
          {
            if (isArray)
            {
              if (isConst)
                return "Vector2d[]";
              else
                return "[In,Out] Vector2d[]";
            }
            return "ref Vector2d";
          }

          if (s.Equals("ON_3dPoint") || s.Equals("AR_3dPoint"))
          {
            if (isArray)
            {
              if (isConst)
                return "Point3d[]";
              else
                return "[In,Out] Point3d[]";
            }
            return "ref Point3d";
          }

          if (s.Equals("ON_3fPoint") || s.Equals("AR_3fPoint") || s.Equals("Point3f"))
            return "ref Point3f";

          if (s.Equals("ON_4dPoint") || s.Equals("AR_4dPoint"))
          {
            if (isArray)
            {
              if (isConst)
                return "Point4d[]";
              else
                return "[In,Out] Point4d[]";
            }
            return "ref Point4d";
          }

          if (s.Equals("ON_4fPoint") || s.Equals("AR_4fPoint"))
            return "ref Color4f";

          if (s.Equals("AR_4fColor") || s.Equals("Color4f"))
          {
            if (isArray)
            {
              if (isConst)
                return "Color4f[]";
              else
                return "[In,Out] Color4f[]";
            }
            return "ref Color4f";
          }

          if (s.Equals("AR_3fColor"))
          {
            if (isArray)
            {
              if (isConst)
                return "Point3f[]";
              else
                return "[In,Out] Point3f[]";
            }
            return "ref Point3f";
          }
          
          if (s.Equals("ON_3dVector") || s.Equals("AR_3dVector"))
          {
            if (isArray)
            {
              if (isConst)
                return "Vector3d[]";
              else
                return "[In,Out] Vector3d[]";
            }
            return "ref Vector3d";
          }

          if (s.Equals("ON_3fVector") || s.Equals("AR_3fVector") || s.Equals("Vector3f"))
          {
            if (isArray)
            {
              if (isConst)
                return "Vector3f[]";
              else
                return "[In,Out] Vector3f[]";
            }
            return "ref Vector3f";
          }

          if (s.Equals("ON_3dRay"))
            return "ref Ray3d";

          if (s.Equals("ON_MeshFace"))
            return "ref MeshFace";

          if (s.Equals("ON_X_EVENT"))
            return "ref CurveIntersect";

          if (s.Equals("AR_MeshFace") || s.Equals("MeshFace"))
          {
            if (isArray)
            {
              if (isConst)
                return "MeshFace[]";
              else
                return "[In,Out] MeshFace[]";
            }
            return "ref MeshFace";
          }

          if (s.Equals("Plane"))
          {
            if (isArray)
            {
              if (isConst)
                return "Plane[]";
              else
                return "[In,Out] Plane[]";
            }
            return "ref Plane";
          }

          if (s.Equals("Circle"))
            return "ref Circle";

          if (s.Equals("unsigned char"))
          {
            if (isArray)
            {
              if (isConst)
                return "byte[]";
              else
                return "[In,Out] byte[]";
            }
            return "ref byte";
          }

          if (s.Equals("ON_MESHPOINT_STRUCT"))
            return "ref MeshPointDataStruct";

          return "IntPtr";
        }


        if (sType.Equals("ON_XFORM_STRUCT") || sType.Equals("AR_Transform_Struct"))
            return "Transform";

        if( sType.Equals("ON_2DPOINT_STRUCT") )
          return "Point2d";

        if (sType.Equals("ON_2FPOINT_STRUCT"))
          return "PointF";

        if( sType.Equals("ON_2DVECTOR_STRUCT") )
          return "Vector2d";

        if (sType.Equals("ON_INTERVAL_STRUCT"))
          return "Interval";

        if (sType.Equals("ON_3FVECTOR_STRUCT"))
          return "Vector3f";

        if (sType.Equals("ON_4FVECTOR_STRUCT"))
          return "Color4f";

        if (sType.Equals("ON_3FPOINT_STRUCT"))
          return "Point3f";

        if (sType.Equals("ON_4FPOINT_STRUCT"))
          return "Point4f";

        if (sType.Equals("ON_4DPOINT_STRUCT"))
          return "Point4d";

        if (sType.Equals("ON_PLANEEQ_STRUCT"))
          return "PlaneEquation";

        if (sType.Equals("ON_PLANE_STRUCT"))
          return "Plane";

        if (sType.Equals("ON_CIRCLE_STRUCT"))
          return "Circle";

        if (sType.Equals("ON_3DPOINT_STRUCT"))
          return "Point3d";

        if (sType.Equals("ON_4DPOINT_STRUCT"))
          return "Point4d";

        if (sType.Equals("ON_3DVECTOR_STRUCT"))
          return "Vector3d";

        if (sType.Equals("ON_4DVECTOR_STRUCT"))
          return "Vector4d";

        if (sType.Equals("ON_LINE_STRUCT"))
          return "Line";

        if (sType.Equals("ON_2INTS"))
          return "ComponentIndex";

        if (sType.Equals("AR_3fColor"))
          return "Point3f";

        if (sType.Equals("AR_4fColor"))
          return "Color4f";

        if (sType.Equals("AR_3fPoint"))
          return "Point3f";

        if (sType.Equals("AR_3fVector"))
          return "Vector3f";

        if (sType.Equals("AR_3dPoint"))
          return "Point3d";

        if (sType.Equals("AR_MeshFace"))
          return "MeshFace";

        if (sType.Equals("unsigned int"))
          return "uint";

        if (sType.Equals("unsigned short"))
          return "ushort";

        if (sType.Equals("char"))
          return "byte";

        if (sType.Equals("ON__INT64"))
          return "Int64";

        if (sType.Equals("COleDateTime"))
          return "DateTime";

        if (sType.Equals("time_t"))
          return "Int64";

        if (sType.Equals("ON_UUID"))
          return "Guid";

        return sType;
      }

      int GetParameterCount()
      {
        int rc = 0;
        int start = m_cdecl.IndexOf('(') + 1;
        int end = m_cdecl.IndexOf(')');
        string parameters = m_cdecl.Substring(start, end - start);
        parameters = parameters.Trim();
        if (parameters.Length > 0)
        {
          string[] p = parameters.Split(new char[] { ',' });
          rc = p.Length;
        }
        return rc;
      }

      string GetFunctionName()
      {
        int end = m_cdecl.IndexOf('(');
        // walk backwards until we hit characters
        while (true)
        {
          if (char.IsWhiteSpace(m_cdecl, end - 1))
            end--;
          else
            break;
        }
        int start = m_cdecl.LastIndexOf(' ', end) + 1;
        return m_cdecl.Substring(start, end - start);
      }

      string GetReturnType(bool asCSharpCode)
      {
        string name = GetFunctionName();
        int end = m_cdecl.IndexOf(name)-1;
        string rc = m_cdecl.Substring(0, end);
        rc = rc.Trim();
        if (rc.StartsWith("const "))
          rc = rc.Substring(6);

        if (asCSharpCode)
        {
          if (rc.EndsWith("*"))
            rc = "IntPtr";
          else if (rc.Equals("unsigned int"))
            rc = "uint";
          else if (rc.Equals("unsigned short"))
            rc = "ushort";
          else if (rc.Equals("ON_UUID"))
            rc = "Guid";
          else if (rc.Equals("LPUNKNOWN") || rc.Equals("HBITMAP") || rc.Equals("HWND") || rc.Equals("HCURSOR") || rc.Equals("HICON") || rc.Equals("HBRUSH") || rc.Equals("HFONT") || rc.Equals("HMENU") || rc.Equals("HDC"))
            rc = "IntPtr";
          else if (rc.Equals("time_t"))
            rc = "Int64";
        }

        return rc;
      }
    }

    class EnumDeclaration
    {
      readonly string m_cdecl;
      public EnumDeclaration(string cdecl)
      {
        m_cdecl = cdecl;
      }

      public void Write(System.IO.StreamWriter sw)
      {
        var cs_decl = m_cdecl.TrimEnd(new char[] { ';' }).Split(new char[]{'\n'});
        for (int i = 0; i < cs_decl.Length; i++)
        {
          if (i == 0)
          {
            string name = cs_decl[i].Trim();
            sw.WriteLine("  internal " + name);
            continue;
          }
          if (i == (cs_decl.Length - 1) || i == 1)
          {
            sw.WriteLine("  " + cs_decl[i].Trim());
            continue;
          }

          string entry = cs_decl[i].Trim();
          // find first upper case character
          int prefix = 0;
          for (int j = 0; j < entry.Length; j++)
          {
            if (char.IsUpper(entry, j))
            {
              prefix = j;
              break;
            }
          }
          entry = entry.Substring(prefix);
          sw.WriteLine("    " + entry);
        }
      }
    }
  
  }
}
