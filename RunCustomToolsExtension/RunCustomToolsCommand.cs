using System;
using Alphaleonis.Vsx.IDE;
using EnvDTE80;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Alphaleonis.Vsx;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Text;
using VSLangProj;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace RunCustomToolsExtension
{
   [Command(PackageIds.CommandSetString, PackageIds.cmdidDynamicCustomToolsCommand)]
   internal class RunCustomToolsCommand : IDynamicCommandImplementation
   {
      #region Private Fields

      private readonly CacheItemPolicy m_cachePolicy = new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromSeconds(2) };
      private const string m_cacheKey = "Alphaleonis.E559FF80-07CE-4821-9DAF-C434AE8FC517";

      private readonly DTE2 m_dte;
      private readonly IServiceProvider m_serviceProvider;
      private readonly IOutputWindowPane m_winPane;

      #endregion

      #region Constructor

      public RunCustomToolsCommand(IServiceProvider serviceProvider, EnvDTE.DTE dte, IOutputWindow outputWin)
      {
         m_winPane = outputWin.General;
         m_dte = dte as DTE2;
         m_serviceProvider = serviceProvider;
      }

      #endregion


      #region Public Methods

      public void Execute(IMenuCommand command)
      {
         string cacheKey;
         using (TextWriter writer = m_winPane.CreateTextWriter())
         {

            IEnumerable<ProjectItemWithCustomTool> projectItems = GetProjectItems(out cacheKey);

            if (command.CommandIndex > 0)
            {
               var tools = GetTools();
               if (command.CommandIndex > tools.Count)
               {
                  writer.WriteLine("new tool with index found.");
                  return;
               }

               projectItems = projectItems.Where(pi => pi.CustomTool == tools[command.CommandIndex - 1]);
            }

            projectItems = projectItems.ToArray();

            Toolkit.RunWithProgress((progress, ct) => System.Threading.Tasks.Task.Run(async () =>
            {                 
               foreach (var projectItemEntry in projectItems.AsSmartEnumerable())
               {
                  var projectItem = projectItemEntry.Value;
                  await ThreadHelper.Generic.InvokeAsync(() => m_winPane.WriteLine($"Running custom tool {projectItem.CustomTool} on {projectItem.ProjectItem.Name}"));
                  progress.Report(new ProgressInfo() { CurrentStep = projectItemEntry.Index, TotalSteps = projectItems.Count(), ProgressText = "ProgressText", WaitText = "WaitText" });
                  await System.Threading.Tasks.Task.Delay(1000, ct);
               }
            }), "Title");                    
         }
      }

      public int GetDynamicCommandCount()
      {
         return GetTools().Count + 1;
      }

      public void QueryStatus(IMenuCommand command)
      {
         var tools = GetTools();

         int index = command.CommandIndex;

         if (index == 0)
         {
            command.Visible = true;
            command.Enabled = tools.Count > 0;
            command.Text = tools.Count > 0 ? "All" : "<no custom tools available>";
         }
         else
         {
            index = index - 1;
            command.Visible = tools.Count > index;
            command.Enabled = tools.Count > index;
            command.Text = command.Enabled ? tools[index] : "Disabled";
         }
      }

      #endregion

      #region Private Methods

      private IEnumerable<ProjectItemWithCustomTool> GetProjectItems(out string cacheKey)
      {
         IEnumerable<ProjectItem> projectItems;
         IEnumerable<SelectedItem> selectedItems = m_dte.SelectedItems.Cast<SelectedItem>();

         StringBuilder cacheKeyBuilder = new StringBuilder(m_cacheKey);
         if (selectedItems.Any(item => item.Project == null))
         {
            // The solution node was selected.
            projectItems = m_dte.Solution.DescendantProjectItems();
            cacheKeyBuilder.Append("Solution");
         }
         else
         {
            // Ignore any project items selected. (Only projects selected are interesting here).
            selectedItems = selectedItems.Where(item => item.ProjectItem == null);

            foreach (var selItem in selectedItems)
               cacheKeyBuilder.Append(selItem.Name);

            projectItems = selectedItems.SelectMany(item => item.Project.DescendantProjectItems()).Where(item => item.Object is VSProjectItem);
         }

         cacheKey = cacheKeyBuilder.ToString();

         projectItems = projectItems.Where(item => item.Object is VSProjectItem);

         var result = projectItems.Select(pi => new ProjectItemWithCustomTool(pi.TryGetProperty<string>("CustomTool"), pi)).Where(pi => !String.IsNullOrEmpty(pi.CustomTool));
         return result;
      }

      private IList<string> GetTools()
      {
         string cacheKey;
         var projectItems = GetProjectItems(out cacheKey);

         string[] toolsArray = MemoryCache.Default.Get(cacheKey) as string[];
         if (toolsArray == null)
         {
            SortedSet<string> tools = new SortedSet<string>();

            foreach (var item in projectItems)
               tools.Add(item.CustomTool);

            toolsArray = tools.ToArray();
            MemoryCache.Default.Add(cacheKey.ToString(), toolsArray, m_cachePolicy);
         }

         return toolsArray;
      }

      #endregion

      #region Nested Types

      private struct ProjectItemWithCustomTool
      {
         public ProjectItemWithCustomTool(string customTool, ProjectItem projectItem)
         {
            CustomTool = customTool;
            ProjectItem = projectItem;
         }

         public string CustomTool { get; }

         public ProjectItem ProjectItem { get; }

         public void RunCustomTool()
         {
            VSProjectItem vsProjectItem = ProjectItem.Object as VSProjectItem;

            if (vsProjectItem != null)
            {
               vsProjectItem.RunCustomTool();
            }
         }
      }

      #endregion
   }
}
