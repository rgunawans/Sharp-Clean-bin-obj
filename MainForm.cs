using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;

namespace Sharp_Clean_bin_obj
{
    public partial class MainForm : Form
    {
        private readonly ArtifactScannerService _scanner = new();
        private readonly Dictionary<string, TreeNode> _treeNodesByPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ArtifactFolder> _selectedArtifacts = new(StringComparer.OrdinalIgnoreCase);
        private ArtifactTreeNode? _rootNode;
        private ArtifactTreeNode? _currentFolder;
        private CancellationTokenSource? _scanCancellation;
        private bool _updatingChecks;
        private bool _isDeleting;
        private int _lastProgressDirectories;
        private int _lastProgressArtifacts;
        private int _sortColumn = 2;
        private bool _sortAscending;

        public MainForm()
        {
            InitializeComponent();
            InitializeFolderIcon();
            buttonSelectRoot.Click += SelectRootFolder;
            buttonScan.Click += async (_, _) => await ScanRootAsync();
            buttonCancelScan.Click += CancelScan;
            buttonUp.Click += GoUpOneLevel;
            buttonSelectAll.Click += SelectAllArtifacts;
            buttonDeselectAll.Click += DeselectAllArtifacts;
            buttonDelete.Click += async (_, _) => await DeleteSelectedAsync();
            checkBoxIncludeVs.CheckedChanged += IncludeVsChanged;
            checkBoxRecycleBin.CheckedChanged += RecycleOptionChanged;
            artifactTreeView.AfterSelect += TreeSelectionChanged;
            artifactListView.ItemCheck += ArtifactItemCheckChanged;
            artifactListView.ItemActivate += OpenSelectedFolder;
            artifactListView.ColumnClick += ListColumnClicked;
            artifactListView.ListViewItemSorter = new ArtifactRowComparer(_sortColumn, _sortAscending);
            FormClosing += (_, _) => _scanCancellation?.Cancel();
            emptyStateLabel.BringToFront();
            UpdateEmptyState();
            UpdateSummary();
            UpdateCommandStates();
        }

        private void RecycleOptionChanged(object? sender, EventArgs e)
        {
            checkBoxRecycleBin.Text = checkBoxRecycleBin.Checked ? "Move to Recycle Bin" : "Permanent Delete";
        }

        private string? _rootPath;

        private void SelectRootFolder(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = "Choose the directory to scan for .NET build artifacts.",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false,
                SelectedPath = _rootPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _rootPath = dialog.SelectedPath;
            ClearScanResults();
            scanStatusLabel.Text = $"Ready to scan: {_rootPath}";
            UpdateCommandStates();
        }

        private async Task ScanRootAsync()
        {
            if (_rootPath is null || _scanCancellation is not null || _isDeleting)
            {
                return;
            }

            using CancellationTokenSource cancellation = new();
            _scanCancellation = cancellation;
            _lastProgressDirectories = 0;
            _lastProgressArtifacts = 0;
            scanProgressBar.Style = ProgressBarStyle.Marquee;
            scanProgressBar.MarqueeAnimationSpeed = 25;
            scanStatusLabel.Text = $"Preparing scan: {_rootPath}";
            UpdateCommandStates();
            bool scanFinished = false;

            try
            {
                IProgress<ScanProgress> progress = new Progress<ScanProgress>(update =>
                {
                    if (scanFinished || cancellation.IsCancellationRequested || IsDisposed)
                    {
                        return;
                    }

                    _lastProgressDirectories = Math.Max(_lastProgressDirectories, update.ScannedDirectories);
                    _lastProgressArtifacts = Math.Max(_lastProgressArtifacts, update.ArtifactFoldersFound);
                    scanStatusLabel.Text = $"Scanning {update.CurrentPath} | {_lastProgressDirectories:N0} directories | {_lastProgressArtifacts:N0} artifact folders found";
                });
                ArtifactTreeNode result = await _scanner.ScanAsync(_rootPath, checkBoxIncludeVs.Checked, progress, cancellation.Token);
                scanFinished = true;
                _rootNode = result;
                _currentFolder = result;
                _selectedArtifacts.Clear();
                PopulateTree(result);
                SelectTreeFolder(result);
                scanStatusLabel.Text = result.ArtifactCount == 0
                    ? "Scan complete. No build artifact folders were found."
                    : $"Scan complete. Found {result.ArtifactCount:N0} artifact folders.";
            }
            catch (OperationCanceledException)
            {
                scanFinished = true;
                scanStatusLabel.Text = _rootNode is null ? "Scan cancelled." : "Scan cancelled. Previous results were retained.";
            }
            catch (UnauthorizedAccessException exception)
            {
                scanFinished = true;
                scanStatusLabel.Text = "Scan failed because access was denied.";
                MessageBox.Show(this, exception.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException exception)
            {
                scanFinished = true;
                scanStatusLabel.Text = "Scan failed because the directory could not be read.";
                MessageBox.Show(this, exception.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                scanFinished = true;
                _scanCancellation = null;
                scanProgressBar.MarqueeAnimationSpeed = 0;
                scanProgressBar.Style = ProgressBarStyle.Blocks;
                scanProgressBar.Value = 0;
                UpdateSummary();
                UpdateCommandStates();
            }
        }

        private void CancelScan(object? sender, EventArgs e)
        {
            if (_scanCancellation is null)
            {
                return;
            }

            scanStatusLabel.Text = "Cancelling scan...";
            _scanCancellation.Cancel();
        }

        private void IncludeVsChanged(object? sender, EventArgs e)
        {
            if (_rootNode is not null && _scanCancellation is null)
            {
                scanStatusLabel.Text = "The .vs option changed. Scan again to update the results.";
            }
        }

        private void GoUpOneLevel(object? sender, EventArgs e)
        {
            if (_currentFolder?.Parent is not null)
            {
                SelectTreeFolder(_currentFolder.Parent);
            }
        }

        private void TreeSelectionChanged(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is ArtifactTreeNode folder)
            {
                _currentFolder = folder;
                PopulateList(folder);
                UpdateCommandStates();
            }
        }

        private void OpenSelectedFolder(object? sender, EventArgs e)
        {
            if (artifactListView.SelectedItems.Count == 0 || artifactListView.SelectedItems[0].Tag is not ArtifactRow row || row.Folder is null)
            {
                return;
            }

            SelectTreeFolder(row.Folder);
        }

        private void PopulateTree(ArtifactTreeNode root)
        {
            _treeNodesByPath.Clear();
            artifactTreeView.BeginUpdate();
            artifactTreeView.Nodes.Clear();
            TreeNode rootTreeNode = CreateTreeNode(root);
            artifactTreeView.Nodes.Add(rootTreeNode);
            rootTreeNode.Expand();
            artifactTreeView.EndUpdate();
        }

        private TreeNode CreateTreeNode(ArtifactTreeNode folder)
        {
            TreeNode treeNode = new(folder.NameWithSize)
            {
                Tag = folder,
                ImageKey = "folder",
                SelectedImageKey = "folder",
                ToolTipText = folder.FullPath
            };
            _treeNodesByPath[folder.FullPath] = treeNode;
            foreach (ArtifactTreeNode child in folder.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        private void SelectTreeFolder(ArtifactTreeNode folder)
        {
            if (_treeNodesByPath.TryGetValue(folder.FullPath, out TreeNode? treeNode))
            {
                artifactTreeView.SelectedNode = treeNode;
                treeNode?.EnsureVisible();
            }
        }

        private void PopulateList(ArtifactTreeNode folder)
        {
            artifactListView.BeginUpdate();
            _updatingChecks = true;
            try
            {
                artifactListView.Items.Clear();
                foreach (ArtifactFolder artifact in folder.Artifacts)
                {
                    AddArtifactRow(artifact);
                }

                foreach (ArtifactTreeNode child in folder.Children)
                {
                    AddFolderRow(child);
                }

                artifactListView.Sort();
            }
            finally
            {
                _updatingChecks = false;
                artifactListView.EndUpdate();
            }

            UpdateEmptyState();
            UpdateCommandStates();
        }

        private void AddArtifactRow(ArtifactFolder artifact)
        {
            ArtifactRow row = new(artifact.Name, GetRelativePath(artifact.FullPath), artifact.Size, artifact.FileCount, artifact.LastModifiedUtc, artifact, null);
            AddListRow(row);
        }

        private void AddFolderRow(ArtifactTreeNode folder)
        {
            ArtifactRow row = new(folder.Name, GetRelativePath(folder.FullPath), folder.TotalSize, folder.FileCount, folder.LastModifiedUtc, null, folder);
            AddListRow(row);
        }

        private void AddListRow(ArtifactRow row)
        {
            ListViewItem item = new(row.Name)
            {
                Tag = row,
                ImageKey = "folder",
                Checked = IsRowFullySelected(row)
            };
            item.SubItems.Add(row.RelativePath);
            item.SubItems.Add(ArtifactScannerService.FormatSize(row.Size));
            item.SubItems.Add(row.FileCount.ToString("N0", System.Globalization.CultureInfo.CurrentCulture));
            item.SubItems.Add(row.LastModifiedUtc?.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty);
            artifactListView.Items.Add(item);
        }

        private void ArtifactItemCheckChanged(object? sender, ItemCheckEventArgs e)
        {
            if (_updatingChecks || e.Index < 0 || e.Index >= artifactListView.Items.Count || artifactListView.Items[e.Index].Tag is not ArtifactRow row)
            {
                return;
            }

            bool isChecked = e.NewValue == CheckState.Checked;
            BeginInvoke(new Action(() =>
            {
                if (artifactListView.IsDisposed)
                {
                    return;
                }

                foreach (ArtifactFolder artifact in EnumerateArtifacts(row))
                {
                    if (isChecked)
                    {
                        _selectedArtifacts[artifact.FullPath] = artifact;
                    }
                    else
                    {
                        _selectedArtifacts.Remove(artifact.FullPath);
                    }
                }

                RefreshVisibleChecks();
                UpdateSummary();
                UpdateCommandStates();
            }));
        }

        private void RefreshVisibleChecks()
        {
            _updatingChecks = true;
            try
            {
                foreach (ListViewItem item in artifactListView.Items)
                {
                    if (item.Tag is ArtifactRow row)
                    {
                        item.Checked = IsRowFullySelected(row);
                    }
                }
            }
            finally
            {
                _updatingChecks = false;
            }
        }

        private bool IsRowFullySelected(ArtifactRow row)
        {
            ArtifactFolder[] artifacts = EnumerateArtifacts(row).ToArray();
            return artifacts.Length > 0 && artifacts.All(artifact => _selectedArtifacts.ContainsKey(artifact.FullPath));
        }

        private static IEnumerable<ArtifactFolder> EnumerateArtifacts(ArtifactRow row)
        {
            if (row.Artifact is not null)
            {
                yield return row.Artifact;
                yield break;
            }

            if (row.Folder is null)
            {
                yield break;
            }

            Stack<ArtifactTreeNode> pending = new();
            pending.Push(row.Folder);
            while (pending.Count > 0)
            {
                ArtifactTreeNode folder = pending.Pop();
                foreach (ArtifactFolder artifact in folder.Artifacts)
                {
                    yield return artifact;
                }

                foreach (ArtifactTreeNode child in folder.Children)
                {
                    pending.Push(child);
                }
            }
        }

        private static IEnumerable<ArtifactFolder> EnumerateArtifacts(ArtifactTreeNode root)
        {
            Stack<ArtifactTreeNode> pending = new();
            pending.Push(root);
            while (pending.Count > 0)
            {
                ArtifactTreeNode folder = pending.Pop();
                foreach (ArtifactFolder artifact in folder.Artifacts)
                {
                    yield return artifact;
                }

                foreach (ArtifactTreeNode child in folder.Children)
                {
                    pending.Push(child);
                }
            }
        }

        private void SelectAllArtifacts(object? sender, EventArgs e)
        {
            if (_rootNode is null)
            {
                return;
            }

            foreach (ArtifactFolder artifact in EnumerateArtifacts(_rootNode))
            {
                _selectedArtifacts[artifact.FullPath] = artifact;
            }

            RefreshVisibleChecks();
            UpdateSummary();
            UpdateCommandStates();
        }

        private void DeselectAllArtifacts(object? sender, EventArgs e)
        {
            _selectedArtifacts.Clear();
            RefreshVisibleChecks();
            UpdateSummary();
            UpdateCommandStates();
        }

        private void ListColumnClicked(object? sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = e.Column;
                _sortAscending = e.Column != 2;
            }

            artifactListView.ListViewItemSorter = new ArtifactRowComparer(_sortColumn, _sortAscending);
            artifactListView.Sort();
        }

        private async Task DeleteSelectedAsync()
        {
            if (_selectedArtifacts.Count == 0 || _rootPath is null || _isDeleting || _scanCancellation is not null)
            {
                return;
            }

            ArtifactFolder[] selected = _selectedArtifacts.Values.OrderByDescending(artifact => artifact.FullPath.Length).ToArray();
            long totalSize = SaturatingSum(selected.Select(artifact => artifact.Size));
            bool recycle = checkBoxRecycleBin.Checked;
            string deletionMode = recycle ? "move to the Recycle Bin" : "permanently delete";
            string permanentWarning = recycle ? string.Empty : Environment.NewLine + Environment.NewLine + "Permanent deletion cannot be undone.";
            DialogResult confirmation = MessageBox.Show(
                this,
                $"{selected.Length:N0} artifact folders totaling {ArtifactScannerService.FormatSize(totalSize)} will be {deletionMode}.{permanentWarning}{Environment.NewLine}{Environment.NewLine}Continue?",
                "Confirm artifact deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            _isDeleting = true;
            scanProgressBar.Style = ProgressBarStyle.Marquee;
            scanProgressBar.MarqueeAnimationSpeed = 25;
            scanStatusLabel.Text = $"Deleting {selected.Length:N0} artifact folders...";
            UpdateCommandStates();

            try
            {
                DeletionResult result = await Task.Run(() => DeleteArtifacts(selected, _rootPath, recycle));
                ApplyDeletion(result.Deleted);
                scanStatusLabel.Text = $"Deleted {result.Deleted.Count:N0} artifact folders.";
                if (result.Failures.Count > 0)
                {
                    string details = string.Join(Environment.NewLine, result.Failures.Take(10).Select(failure => $"{failure.Path}: {failure.Message}"));
                    string remaining = result.Failures.Count > 10 ? $"{Environment.NewLine}...and {result.Failures.Count - 10:N0} more." : string.Empty;
                    MessageBox.Show(this,
                        $"{result.Deleted.Count:N0} folders were deleted; {result.Failures.Count:N0} could not be deleted.{Environment.NewLine}{Environment.NewLine}{details}{remaining}",
                        "Some folders could not be deleted",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _isDeleting = false;
                scanProgressBar.MarqueeAnimationSpeed = 0;
                scanProgressBar.Style = ProgressBarStyle.Blocks;
                scanProgressBar.Value = 0;
                UpdateSummary();
                UpdateCommandStates();
            }
        }

        private static DeletionResult DeleteArtifacts(IEnumerable<ArtifactFolder> artifacts, string rootPath, bool recycle)
        {
            List<ArtifactFolder> deleted = new();
            List<DeletionFailure> failures = new();
            string normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
            foreach (ArtifactFolder artifact in artifacts)
            {
                string path = Path.GetFullPath(artifact.FullPath);
                string name = Path.GetFileName(path);
                bool isArtifactName = name.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
                    || name.Equals(".vs", StringComparison.OrdinalIgnoreCase);
                if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !isArtifactName)
                {
                    failures.Add(new DeletionFailure(path, "The path is outside the selected root or is not a recognized artifact folder."));
                    continue;
                }

                try
                {
                    if (recycle)
                    {
                        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    else
                    {
                        Directory.Delete(path, recursive: true);
                    }

                    deleted.Add(artifact);
                }
                catch (UnauthorizedAccessException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (PathTooLongException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (IOException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (System.Security.SecurityException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (ArgumentException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (InvalidOperationException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (System.ComponentModel.Win32Exception exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
                catch (COMException exception)
                {
                    failures.Add(new DeletionFailure(path, exception.Message));
                }
            }

            return new DeletionResult(deleted, failures);
        }

        private void ApplyDeletion(IReadOnlyCollection<ArtifactFolder> deleted)
        {
            foreach (ArtifactFolder artifact in deleted)
            {
                _selectedArtifacts.Remove(artifact.FullPath);
                artifact.Parent.Artifacts.Remove(artifact);
                RecalculateAncestors(artifact.Parent);
                RemoveEmptyBranches(artifact.Parent);
            }

            if (_rootNode is not null)
            {
                ArtifactTreeNode? destination = _currentFolder;
                while (destination is not null && !_treeNodesByPath.ContainsKey(destination.FullPath))
                {
                    destination = destination.Parent;
                }

                _currentFolder = destination ?? _rootNode;
                SelectTreeFolder(_currentFolder);
            }

            if (_currentFolder is not null)
            {
                PopulateList(_currentFolder);
            }

            UpdateSummary();
        }

        private void RecalculateAncestors(ArtifactTreeNode node)
        {
            ArtifactTreeNode? current = node;
            while (current is not null)
            {
                current.TotalSize = SaturatingSum(current.Artifacts.Select(artifact => artifact.Size)
                    .Concat(current.Children.Select(child => child.TotalSize)));
                current.ArtifactCount = current.Artifacts.Count + current.Children.Sum(child => child.ArtifactCount);
                current.FileCount = SaturatingSum(current.Artifacts.Select(artifact => artifact.FileCount)
                    .Concat(current.Children.Select(child => child.FileCount)));
                current.LastModifiedUtc = current.Artifacts.Select(artifact => artifact.LastModifiedUtc)
                    .Concat(current.Children.Select(child => child.LastModifiedUtc))
                    .Max();

                if (artifactTreeView.Nodes.Count > 0 && _treeNodesByPath.TryGetValue(current.FullPath, out TreeNode? treeNode))
                {
                    treeNode.Text = current.NameWithSize;
                    treeNode.ToolTipText = current.FullPath;
                }

                current = current.Parent;
            }
        }

        private void RemoveEmptyBranches(ArtifactTreeNode node)
        {
            ArtifactTreeNode? current = node;
            while (current is not null && current.Parent is not null && current.ArtifactCount == 0)
            {
                ArtifactTreeNode parent = current.Parent;
                parent.Children.Remove(current);
                if (artifactTreeView.Nodes.Count > 0 && _treeNodesByPath.Remove(current.FullPath, out TreeNode? treeNode))
                {
                    treeNode.Remove();
                }

                current = parent;
                RecalculateAncestors(current);
            }
        }

        private void ClearScanResults()
        {
            _rootNode = null;
            _currentFolder = null;
            _selectedArtifacts.Clear();
            _treeNodesByPath.Clear();
            artifactTreeView.Nodes.Clear();
            artifactListView.Items.Clear();
            emptyStateLabel.Text = "Select a folder to inspect its artifact folders.";
            UpdateEmptyState();
            UpdateSummary();
        }

        private void UpdateEmptyState()
        {
            emptyStateLabel.Visible = artifactListView.Items.Count == 0;
            if (artifactListView.Items.Count > 0)
            {
                emptyStateLabel.SendToBack();
            }
            else
            {
                emptyStateLabel.BringToFront();
                if (_rootNode is not null)
                {
                    emptyStateLabel.Text = _rootNode.ArtifactCount == 0
                        ? "No .NET build artifact folders were found under this root."
                        : "This folder has no direct artifacts or branches containing artifacts.";
                }
            }
        }

        private void UpdateSummary()
        {
            int folderCount = _rootNode?.ArtifactCount ?? 0;
            long totalSize = _rootNode?.TotalSize ?? 0;
            long selectedSize = SaturatingSum(_selectedArtifacts.Values.Select(artifact => artifact.Size));
            totalSummaryLabel.Text = $"Total Artifact Space Found: {ArtifactScannerService.FormatSize(totalSize)} across {folderCount:N0} folders";
            selectedSummaryLabel.Text = $"Selected for Deletion: {_selectedArtifacts.Count:N0} folders | Space to be freed: {ArtifactScannerService.FormatSize(selectedSize)}";
        }

        private void UpdateCommandStates()
        {
            bool busy = _scanCancellation is not null || _isDeleting;
            buttonSelectRoot.Enabled = !busy;
            buttonScan.Enabled = !busy && !string.IsNullOrWhiteSpace(_rootPath);
            buttonCancelScan.Enabled = _scanCancellation is not null;
            checkBoxIncludeVs.Enabled = !busy;
            checkBoxRecycleBin.Enabled = !busy;
            buttonUp.Enabled = !busy && _currentFolder?.Parent is not null;
            buttonSelectAll.Enabled = !busy && _rootNode?.ArtifactCount > 0;
            buttonDeselectAll.Enabled = !busy && _selectedArtifacts.Count > 0;
            buttonDelete.Enabled = !busy && _selectedArtifacts.Count > 0;
            artifactTreeView.Enabled = !busy;
            artifactListView.Enabled = !busy;
        }

        private string GetRelativePath(string path) => _rootPath is null ? path : Path.GetRelativePath(_rootPath, path);

        private static long SaturatingSum(IEnumerable<long> values)
        {
            long total = 0;
            foreach (long value in values)
            {
                total = long.MaxValue - total < value ? long.MaxValue : total + value;
            }

            return total;
        }

        private void InitializeFolderIcon()
        {
            const uint fileAttributeDirectory = 0x10;
            const uint shGetFileInfoIcon = 0x100;
            const uint shGetFileInfoSmallIcon = 0x1;
            const uint shGetFileInfoUseFileAttributes = 0x10;
            SHFileInfo info = new();
            IntPtr result = SHGetFileInfo("folder", fileAttributeDirectory, out info, (uint)Marshal.SizeOf<SHFileInfo>(),
                shGetFileInfoIcon | shGetFileInfoSmallIcon | shGetFileInfoUseFileAttributes);
            Icon? folderIcon = null;
            if (result != IntPtr.Zero && info.IconHandle != IntPtr.Zero)
            {
                try
                {
                    folderIcon = (Icon)Icon.FromHandle(info.IconHandle).Clone();
                }
                finally
                {
                    DestroyIcon(info.IconHandle);
                }
            }

            folderImageList.Images.Add("folder", folderIcon ?? SystemIcons.WinLogo);
            folderIcon?.Dispose();
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string path, uint fileAttributes, out SHFileInfo fileInfo, uint fileInfoSize, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr iconHandle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFileInfo
        {
            public IntPtr IconHandle;
            public int IconIndex;
            public uint Attributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string? DisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string? TypeName;
        }

        private sealed record ArtifactRow(string Name, string RelativePath, long Size, long FileCount, DateTime? LastModifiedUtc, ArtifactFolder? Artifact, ArtifactTreeNode? Folder);
        private sealed record DeletionFailure(string Path, string Message);
        private sealed record DeletionResult(List<ArtifactFolder> Deleted, List<DeletionFailure> Failures);

        private sealed class ArtifactRowComparer(int column, bool ascending) : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem left || y is not ListViewItem right || left.Tag is not ArtifactRow leftRow || right.Tag is not ArtifactRow rightRow)
                {
                    return 0;
                }

                int comparison = column switch
                {
                    1 => StringComparer.CurrentCultureIgnoreCase.Compare(leftRow.RelativePath, rightRow.RelativePath),
                    2 => leftRow.Size.CompareTo(rightRow.Size),
                    3 => leftRow.FileCount.CompareTo(rightRow.FileCount),
                    4 => Nullable.Compare(leftRow.LastModifiedUtc, rightRow.LastModifiedUtc),
                    _ => StringComparer.CurrentCultureIgnoreCase.Compare(leftRow.Name, rightRow.Name)
                };
                return ascending ? comparison : -comparison;
            }
        }
    }

    internal sealed class BufferedTreeView : TreeView
    {
        public BufferedTreeView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }

    internal sealed class BufferedListView : ListView
    {
        public BufferedListView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}
