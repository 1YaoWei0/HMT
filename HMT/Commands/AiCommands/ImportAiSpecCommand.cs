using HMT.Models.AiSchemas;
using HMT.Services.AiIntegration;
using Microsoft.Dynamics.Framework.Tools.MetaModel.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace HMT.Commands.AiCommands
{
    /// <summary>
    /// VS command that imports an AI-generated JSON metadata spec file
    /// and creates D365 X++ objects via the VSIX SDK.
    /// </summary>
    internal sealed class ImportAiSpecCommand
    {
        public const int CommandId = 0x1220;

        public static readonly Guid CommandSet = new Guid("194ef7a6-070b-47e5-b084-193c13aa350a");

        private readonly AsyncPackage package;

        private ImportAiSpecCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ImportAiSpecCommand Instance { get; private set; }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ImportAiSpecCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Open file dialog to select JSON spec
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select AI Metadata Spec File";
                    openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    string filePath = openFileDialog.FileName;

                    var builderService = new JsonMetadataBuilderService();
                    AiMetadataSpec spec = builderService.LoadSpec(filePath);

                    // Validate and show preview before executing
                    var validator = new MetadataValidator(new Kernel.AxHelper());
                    var validationResult = validator.ValidateSpec(spec);

                    if (validationResult.HasErrors)
                    {
                        string errorMsg = $"Validation errors found:\n\n{validationResult.GetSummary()}\n\nPlease fix these errors and try again.";
                        MessageBox.Show(errorMsg, "HMT - AI Spec Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Show confirmation with warnings
                    string confirmMsg = $"Ready to create {spec.Objects.Count} object(s) from spec.";
                    if (validationResult.HasWarnings)
                    {
                        confirmMsg += $"\n\nWarnings:\n{validationResult.GetSummary()}";
                    }
                    confirmMsg += "\n\nProceed?";

                    if (MessageBox.Show(confirmMsg, "HMT - Import AI Spec", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    // Execute
                    BuildResult result = builderService.ExecuteSpec(spec);

                    if (result.Success)
                    {
                        CoreUtility.DisplayInfo($"AI Spec import completed successfully!\n\n{result.GetDisplayMessage()}");
                    }
                    else
                    {
                        MessageBox.Show($"AI Spec import completed with errors:\n\n{result.GetDisplayMessage()}",
                            "HMT - Import Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                CoreUtility.HandleExceptionWithErrorMessage(ex);
            }
        }
    }
}
