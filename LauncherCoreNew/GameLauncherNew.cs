using Leayal.SharedInterfaces;
using System;
using System.Diagnostics;
using System.IO;
using Leayal.PSO2Launcher.Interfaces;
using System.Windows.Forms;
using Leayal.PSO2Launcher.Helper;
using System.Runtime.Loader;
using System.Reflection;

namespace Leayal.PSO2Launcher.Core
{
#nullable enable
    public class GameLauncherNew : LauncherProgram
    {
        private readonly App _app;
        
        private readonly int _bootstrapversion;

        public GameLauncherNew(int bootstrapversion) : base(true, true)
        {
            this._bootstrapversion = bootstrapversion;

            if (AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) is AssemblyLoadContext context)
            {
                var assembly_probing_path = Path.GetFullPath("bin", LauncherController.RootDirectory);

                // Yup, has to pre-load it because it doesn't do well with custom probing or different load contexts.
                SolveTheUncoolAssembliesThatDoesNotDoWellWithLoadContexts(context, Path.Combine(assembly_probing_path, "LauncherCore.dll"), null);
                SolveTheUncoolAssembliesThatDoesNotDoWellWithLoadContexts(context, Path.Combine(assembly_probing_path, "MahApps.Metro.IconPacks.FontAwesome.dll"), null);
            }

            var form = new DummyForm();
            form.Show();
            Application.DoEvents();
            this._app = new App(this._bootstrapversion, form);
            // var adminClient = new Leayal.PSO2Launcher.AdminProcess.AdminClient();
            // this.isLightMode = false;
        }

        private static void SolveTheUncoolAssembliesThatDoesNotDoWellWithLoadContexts(AssemblyLoadContext context, string assemblyPath, AssemblyDependencyResolver? resolver)
        {
            if (resolver == null)
            {
                resolver = new AssemblyDependencyResolver(assemblyPath);
            }
            if (context.FromFileWithNative(assemblyPath) is Assembly asm)
            {
                foreach (var referenced in asm.GetReferencedAssemblies())
                {
                    if (referenced != null)
                    {
                        var path = resolver.ResolveAssemblyToPath(referenced);
                        if (path != null)
                        {
                            SolveTheUncoolAssembliesThatDoesNotDoWellWithLoadContexts(context, path, new AssemblyDependencyResolver(path));
                        }
                    }
                }
            }
        }

        protected override int OnExit()
        {
            var code = Environment.ExitCode;
            this._app?.Shutdown(code);
            return code;
        }

        protected override void OnFirstInstance(string[] args)
        {
            this.OnInitialized();
            if (Debugger.IsAttached)
            {
                Environment.ExitCode = this._app.Run();
            }
            else
            {
                try
                {
                    Environment.ExitCode = this._app.Run();
                }
                catch (Exception ex)
                {
                    using (var sw = new StreamWriter(Path.Combine(RuntimeValues.RootDirectory, "unhandled_error_wpf2.txt"), true, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine(ex.ToString());
                        sw.Flush();
                    }
                }
            }

            if (AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) is AssemblyLoadContext context)
            {
                if (context.IsCollectible)
                {
                    context.Unload();
                }
            }
        }

        protected override void OnSubsequentInstance(string[] args)
        {
            if (this._app is App app)
            {
                app.Dispatcher.InvokeAsync(delegate
                {
                    var modal = app.GetModalOrNull();
                    if (modal != null)
                    {
                        if (modal.WindowState == System.Windows.WindowState.Minimized)
                        {
                            System.Windows.SystemCommands.RestoreWindow(modal);
                        }
                        modal.Activate();
                    }
                    else
                    {
                        if (app.MainWindow is Windows.MainMenuWindow window)
                        {
                            if (window.IsMinimizedToTray)
                            {
                                window.IsMinimizedToTray = false;
                            }
                            else if (window.WindowState == System.Windows.WindowState.Minimized)
                            {
                                System.Windows.SystemCommands.RestoreWindow(window);
                            }
                            window.Activate();
                        }
                    }
                });
            }
        }

        class DummyForm : Form
        {
            public DummyForm() : base()
            {
                //*
                this.Icon = BootstrapResources.ExecutableIcon;
                this.Text = "PSO2 Launcher by Dramiel Leayal";
                this.DoubleBuffered = false;
                this.MinimizeBox = false;
                this.MaximizeBox = false;
                var panel = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 1
                };
                panel.RowStyles.Add(new RowStyle() { SizeType = SizeType.Percent, Height = 100 });
                panel.ColumnStyles.Add(new ColumnStyle() { SizeType = SizeType.Percent, Width = 100 });
                var lb = new Label()
                {
                    BorderStyle = BorderStyle.None,
                    AutoSize = true,
                    Anchor = AnchorStyles.None,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Text = "Loading the launcher UI..." + Environment.NewLine + "Please wait"
                };
                panel.Controls.Add(lb);
                this.Controls.Add(panel);
                var size = new System.Drawing.Size(this.Width, lb.Height);
                this.ClientSize = size;
                panel.Size = size;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.StartPosition = FormStartPosition.CenterScreen;
                //*/
            }

            // protected override bool ShowWithoutActivation => true;
        }
    }
#nullable restore
}
