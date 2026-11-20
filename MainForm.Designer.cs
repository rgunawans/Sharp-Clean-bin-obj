#nullable enable

namespace Sharp_Clean_bin_obj
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer? components;
        private TableLayoutPanel rootLayout = null!;
        private TableLayoutPanel toolbarLayout = null!;
        private FlowLayoutPanel primaryCommandsPanel = null!;
        private FlowLayoutPanel selectionCommandsPanel = null!;
        private Button buttonSelectRoot = null!;
        private Button buttonScan = null!;
        private Button buttonCancelScan = null!;
        private CheckBox checkBoxIncludeVs = null!;
        private Button buttonUp = null!;
        private Button buttonSelectAll = null!;
        private Button buttonDeselectAll = null!;
        private CheckBox checkBoxRecycleBin = null!;
        private Button buttonDelete = null!;
        private TableLayoutPanel progressLayout = null!;
        private ProgressBar scanProgressBar = null!;
        private Label scanStatusLabel = null!;
        private SplitContainer splitContainer = null!;
        private BufferedTreeView artifactTreeView = null!;
        private Panel listPanel = null!;
        private BufferedListView artifactListView = null!;
        private Label emptyStateLabel = null!;
        private TableLayoutPanel summaryLayout = null!;
        private Label totalSummaryLabel = null!;
        private Label selectedSummaryLabel = null!;
        private ImageList folderImageList = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            rootLayout = new TableLayoutPanel();
            toolbarLayout = new TableLayoutPanel();
            primaryCommandsPanel = new FlowLayoutPanel();
            selectionCommandsPanel = new FlowLayoutPanel();
            buttonSelectRoot = new Button();
            buttonScan = new Button();
            buttonCancelScan = new Button();
            checkBoxIncludeVs = new CheckBox();
            buttonUp = new Button();
            buttonSelectAll = new Button();
            buttonDeselectAll = new Button();
            checkBoxRecycleBin = new CheckBox();
            buttonDelete = new Button();
            progressLayout = new TableLayoutPanel();
            scanProgressBar = new ProgressBar();
            scanStatusLabel = new Label();
            splitContainer = new SplitContainer();
            artifactTreeView = new BufferedTreeView();
            listPanel = new Panel();
            artifactListView = new BufferedListView();
            emptyStateLabel = new Label();
            summaryLayout = new TableLayoutPanel();
            totalSummaryLabel = new Label();
            selectedSummaryLabel = new Label();
            folderImageList = new ImageList(components);
            rootLayout.SuspendLayout();
            toolbarLayout.SuspendLayout();
            primaryCommandsPanel.SuspendLayout();
            selectionCommandsPanel.SuspendLayout();
            progressLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            listPanel.SuspendLayout();
            summaryLayout.SuspendLayout();
            SuspendLayout();
            // Native system colors and Explorer folder icons keep the utility familiar; the selection color marks only the active item.
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Padding = new Padding(8);
            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            rootLayout.Controls.Add(toolbarLayout, 0, 0);
            rootLayout.Controls.Add(progressLayout, 0, 1);
            rootLayout.Controls.Add(splitContainer, 0, 2);
            rootLayout.Controls.Add(summaryLayout, 0, 3);
            toolbarLayout.ColumnCount = 1;
            toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbarLayout.Dock = DockStyle.Fill;
            toolbarLayout.RowCount = 2;
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            toolbarLayout.Controls.Add(primaryCommandsPanel, 0, 0);
            toolbarLayout.Controls.Add(selectionCommandsPanel, 0, 1);
            ConfigureCommandPanel(primaryCommandsPanel);
            ConfigureCommandPanel(selectionCommandsPanel);
            ConfigureButton(buttonSelectRoot, "Select Root Folder", 150);
            ConfigureButton(buttonScan, "Scan / Refresh", 125);
            ConfigureButton(buttonCancelScan, "Cancel Scan", 115);
            checkBoxIncludeVs.AutoSize = false;
            checkBoxIncludeVs.Margin = new Padding(8, 7, 3, 3);
            checkBoxIncludeVs.Size = new Size(210, 28);
            checkBoxIncludeVs.Text = "Include hidden .vs folders";
            checkBoxIncludeVs.UseVisualStyleBackColor = true;
            primaryCommandsPanel.Controls.Add(buttonSelectRoot);
            primaryCommandsPanel.Controls.Add(buttonScan);
            primaryCommandsPanel.Controls.Add(buttonCancelScan);
            primaryCommandsPanel.Controls.Add(checkBoxIncludeVs);
            ConfigureButton(buttonUp, "Up One Level", 120);
            ConfigureButton(buttonSelectAll, "Select All Artifacts", 155);
            ConfigureButton(buttonDeselectAll, "Deselect All", 115);
            checkBoxRecycleBin.AutoSize = false;
            checkBoxRecycleBin.Checked = true;
            checkBoxRecycleBin.CheckState = CheckState.Checked;
            checkBoxRecycleBin.Margin = new Padding(8, 7, 3, 3);
            checkBoxRecycleBin.Size = new Size(170, 28);
            checkBoxRecycleBin.Text = "Move to Recycle Bin";
            checkBoxRecycleBin.UseVisualStyleBackColor = true;
            ConfigureButton(buttonDelete, "Delete Selected", 140);
            selectionCommandsPanel.Controls.Add(buttonUp);
            selectionCommandsPanel.Controls.Add(buttonSelectAll);
            selectionCommandsPanel.Controls.Add(buttonDeselectAll);
            selectionCommandsPanel.Controls.Add(checkBoxRecycleBin);
            selectionCommandsPanel.Controls.Add(buttonDelete);
            progressLayout.ColumnCount = 2;
            progressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            progressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            progressLayout.Dock = DockStyle.Fill;
            progressLayout.Padding = new Padding(2, 4, 2, 4);
            progressLayout.RowCount = 1;
            progressLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            scanProgressBar.Dock = DockStyle.Fill;
            scanProgressBar.Margin = new Padding(3, 6, 8, 4);
            scanProgressBar.Style = ProgressBarStyle.Blocks;
            progressLayout.Controls.Add(scanProgressBar, 0, 0);
            scanStatusLabel.Dock = DockStyle.Fill;
            scanStatusLabel.AutoEllipsis = true;
            scanStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            scanStatusLabel.Margin = new Padding(2, 0, 2, 0);
            scanStatusLabel.Text = "Choose a root folder to scan for build artifacts.";
            progressLayout.Controls.Add(scanStatusLabel, 1, 0);
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.Location = new Point(11, 143);
            splitContainer.Size = new Size(1000, 500);
            splitContainer.MinimumSize = new Size(700, 300);
            splitContainer.Panel1MinSize = 220;
            splitContainer.Panel2MinSize = 440;
            splitContainer.SplitterDistance = 320;
            splitContainer.TabIndex = 0;
            artifactTreeView.Dock = DockStyle.Fill;
            artifactTreeView.HideSelection = false;
            artifactTreeView.ImageList = folderImageList;
            artifactTreeView.FullRowSelect = true;
            artifactTreeView.ShowNodeToolTips = true;
            splitContainer.Panel1.Controls.Add(artifactTreeView);
            listPanel.Dock = DockStyle.Fill;
            listPanel.Controls.Add(artifactListView);
            listPanel.Controls.Add(emptyStateLabel);
            artifactListView.CheckBoxes = true;
            artifactListView.Dock = DockStyle.Fill;
            artifactListView.FullRowSelect = true;
            artifactListView.GridLines = true;
            artifactListView.HideSelection = false;
            artifactListView.SmallImageList = folderImageList;
            artifactListView.UseCompatibleStateImageBehavior = false;
            artifactListView.View = View.Details;
            artifactListView.Columns.Add("Folder Name", 190, HorizontalAlignment.Left);
            artifactListView.Columns.Add("Relative Path", 280, HorizontalAlignment.Left);
            artifactListView.Columns.Add("Allocated Size", 115, HorizontalAlignment.Right);
            artifactListView.Columns.Add("File Count", 90, HorizontalAlignment.Right);
            artifactListView.Columns.Add("Last Modified", 145, HorizontalAlignment.Left);
            artifactListView.AccessibleName = "Build artifact folders";
            artifactTreeView.AccessibleName = "Folder tree";
            scanProgressBar.AccessibleName = "Scan progress";
            emptyStateLabel.BackColor = SystemColors.Window;
            emptyStateLabel.Dock = DockStyle.Fill;
            emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyStateLabel.Padding = new Padding(20);
            emptyStateLabel.Text = "Select a folder to inspect its artifact folders.";
            splitContainer.Panel2.Controls.Add(listPanel);
            summaryLayout.ColumnCount = 2;
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            summaryLayout.Dock = DockStyle.Fill;
            summaryLayout.Padding = new Padding(0, 8, 0, 0);
            summaryLayout.RowCount = 1;
            summaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ConfigureSummaryLabel(totalSummaryLabel);
            ConfigureSummaryLabel(selectedSummaryLabel);
            totalSummaryLabel.Text = "Total Artifact Space Found: 0 B across 0 folders";
            selectedSummaryLabel.Text = "Selected for Deletion: 0 folders | Space to be freed: 0 B";
            summaryLayout.Controls.Add(totalSummaryLabel, 0, 0);
            summaryLayout.Controls.Add(selectedSummaryLabel, 1, 0);
            folderImageList.ColorDepth = ColorDepth.Depth32Bit;
            folderImageList.ImageSize = new Size(16, 16);
            folderImageList.TransparentColor = Color.Transparent;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 820);
            MinimumSize = new Size(1000, 680);
            Controls.Add(rootLayout);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sharp Clean | Build Artifact Cleanup";
            rootLayout.ResumeLayout(false);
            toolbarLayout.ResumeLayout(false);
            primaryCommandsPanel.ResumeLayout(false);
            selectionCommandsPanel.ResumeLayout(false);
            progressLayout.ResumeLayout(false);
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            listPanel.ResumeLayout(false);
            summaryLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        private static void ConfigureCommandPanel(FlowLayoutPanel panel)
        {
            panel.Dock = DockStyle.Fill;
            panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.Padding = new Padding(0, 2, 0, 0);
        }

        private static void ConfigureButton(Button button, string text, int width)
        {
            button.AutoSize = false;
            button.Margin = new Padding(3, 3, 5, 3);
            button.Size = new Size(width, 31);
            button.Text = text;
            button.UseVisualStyleBackColor = true;
        }

        private static void ConfigureSummaryLabel(Label label)
        {
            label.AutoEllipsis = true;
            label.Dock = DockStyle.Fill;
            label.Font = new Font(SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont, FontStyle.Bold);
            label.Padding = new Padding(10, 0, 10, 0);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }
    }
}