using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leayal.PSO2Launcher.Communication.BootstrapUpdater;
using System.Reflection;
using System.Diagnostics;

namespace Leayal.PSO2Launcher
{
    /// <summary>
    /// Main purpose is to check for component updates.
    /// </summary>
    public partial class Bootstrap : Form
    {
        #region | Fields |
        #endregion

        #region | Constructor |
        public Bootstrap()
        {
            InitializeComponent();
        }
        #endregion

        #region | Form Loads |
        private void Bootstrap_Load(object sender, EventArgs e)
        {
            // Placeholder, maybe not even be in using.
        }

        private async void Bootstrap_Shown(object sender, EventArgs e)
        {
            string exename, rootDirectory;
            using (var currentProc = Process.GetCurrentProcess())
            {
                exename = Path.GetFileName(currentProc.MainModule.FileName);
                rootDirectory = Path.GetDirectoryName(currentProc.MainModule.FileName);
            }

            var bootstrapUpdater = new AssemblyLoadContext("BootstrapUpdater", true);
            // bootstrapUpdater.LoadFromAssemblyPath(Path.Combine("bin", "SharpCompress.dll")); // Optional??
            Assembly netasm_bootstrapUpdater;
            try
            {
                using (var fs = File.OpenRead(Path.GetFullPath(Path.Combine("bin", "BootstrapUpdater.dll"), rootDirectory)))
                {
                    netasm_bootstrapUpdater = bootstrapUpdater.LoadFromStream(fs);
                }
            }
            catch (Exception ex)
            {
                bootstrapUpdater.Unload();
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.label1.Text = "Error occured while checking for updates.";
                return; // No need to continue.
            }

            try
            {
                // Invoke updater. Dynamic is slow.
                var class_bootstrapUpdater = netasm_bootstrapUpdater.CreateInstance("Leayal.PSO2Launcher.Updater.BootstrapUpdater");
                var medthod_bootstrapUpdater_CheckForUpdates = class_bootstrapUpdater.GetType().GetMethod("CheckForUpdatesAsync");
                var obj = medthod_bootstrapUpdater_CheckForUpdates.Invoke(class_bootstrapUpdater, new object[] { rootDirectory, exename });
                if (obj is Task<BootstrapUpdater_CheckForUpdates> task_data)
                {
                    var data = await task_data;

                    // Handle downloads and overwrite files.
                    if (data.Items.Count != 0)
                    {
                        if (data.RequireRestart)
                        {

                        }
                        else if (data.RequireReload)
                        {

                        }
                        else
                        {

                        }
                    }
                }
                else
                {
                    // Report error
                }
            }
            finally
            {
                netasm_bootstrapUpdater = null;
                bootstrapUpdater.Unload();
            }

            // Loads stuff

            // Updater component

            // Check for dependency updates

            // Lazy load the dependency

            // Load the Launcher's entry point and call it
        }
        #endregion
    }
}
