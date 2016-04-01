﻿//------------------------------------------------------------------------------
// <copyright file="RunCustomToolsPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Alphaleonis.Vsx;
using Alphaleonis.Vsx.IDE;
using System.Reflection;

namespace RunCustomToolsExtension
{
   [PackageRegistration(UseManagedResourcesOnly = true)]
   [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
   [Guid(RunCustomToolsPackage.PackageGuidString)]
   [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
   [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
   [ProvideMenuResource("Menus.ctmenu", 1)]
   public sealed class RunCustomToolsPackage : Package
   {
      private IToolkit m_toolkit;

      /// <summary>
      /// RunCustomToolsPackage GUID string.
      /// </summary>
      public const string PackageGuidString = "0334cb4e-9de0-4b7b-b263-2293e0958cf5";

      /// <summary>
      /// Initializes a new instance of the <see cref="RunCustomToolsPackage"/> class.
      /// </summary>
      public RunCustomToolsPackage()
      {
         // Inside this method you can place any initialization code that does not require
         // any Visual Studio service because at this point the package object is created but
         // not sited yet inside Visual Studio environment. The place to do all the other
         // initialization is the Initialize method.
      }

      #region Package Members

      /// <summary>
      /// Initialization of the package; this method is called right after the package is sited, so this is the place
      /// where you can put all the initialization code that rely on services provided by VisualStudio.
      /// </summary>
      protected override void Initialize()
      {
         base.Initialize();
         m_toolkit = Toolkit.Initialize(this, ServiceLocatorOptions.All);
         m_toolkit.CommandManager.AddAllCommandsFrom(Assembly.GetExecutingAssembly());         
      }

      

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);
         if (m_toolkit != null)
         {
            m_toolkit.Dispose();
            m_toolkit = null;
         }
      }
      #endregion
   }
}
