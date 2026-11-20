using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Sharp_Clean_bin_obj
{
    public sealed class ArtifactScannerService
    {
        public static string FormatSize(long size)
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double value = Math.Max(size, 0);
            int unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:0.##} {units[unitIndex]}";
        }

        public Task<ArtifactTreeNode> ScanAsync(string rootPath, bool includeVsFolders, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
            string fullRootPath = Path.GetFullPath(rootPath);
            return Task.Run(() => ScanCoreAsync(fullRootPath, includeVsFolders, progress, cancellationToken), cancellationToken);
        }

        private static async Task<ArtifactTreeNode> ScanCoreAsync(string rootPath, bool includeVsFolders, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"The selected root directory does not exist: {rootPath}");
            }

            ArtifactTreeNode root = new(Path.GetFileName(Path.TrimEndingDirectorySeparator(rootPath)), rootPath, null);
            ConcurrentBag<ArtifactTreeNode> allNodes = new();
            allNodes.Add(root);
            List<ArtifactTreeNode> currentLevel = [root];
            int directoriesScanned = 0;
            int artifactsFound = 0;
            ParallelOptions options = new()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount * 2, 16))
            };

            while (currentLevel.Count > 0)
            {
                ConcurrentBag<ArtifactTreeNode> nextLevel = new();
                await Parallel.ForEachAsync(currentLevel, options, (parent, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    int scanned = Interlocked.Increment(ref directoriesScanned);
                    if ((scanned & 15) == 1)
                    {
                        progress?.Report(new ScanProgress(parent.FullPath, scanned, Volatile.Read(ref artifactsFound)));
                    }

                    foreach (string childPath in EnumerateDirectories(parent.FullPath))
                    {
                        token.ThrowIfCancellationRequested();
                        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(childPath));
                        if (!TryGetAttributes(childPath, out FileAttributes attributes) || (attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }

                        if (!includeVsFolders && name.Equals(".vs", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (IsArtifactFolder(name))
                        {
                            DirectoryMetrics metrics = MeasureArtifactFolder(childPath, token, path =>
                            {
                                int scannedCount = Interlocked.Increment(ref directoriesScanned);
                                if ((scannedCount & 63) == 0)
                                {
                                    progress?.Report(new ScanProgress(path, scannedCount, Volatile.Read(ref artifactsFound)));
                                }
                            });
                            parent.Artifacts.Add(new ArtifactFolder(name, childPath, metrics.Size, metrics.FileCount, metrics.LastModifiedUtc, parent));
                            int found = Interlocked.Increment(ref artifactsFound);
                            progress?.Report(new ScanProgress(childPath, Volatile.Read(ref directoriesScanned), found));
                            continue;
                        }

                        ArtifactTreeNode child = new(name, childPath, parent);
                        parent.Children.Add(child);
                        nextLevel.Add(child);
                        allNodes.Add(child);
                    }

                    return ValueTask.CompletedTask;
                });

                currentLevel = nextLevel.ToArray().ToList();
            }

            foreach (ArtifactTreeNode node in allNodes.OrderByDescending(node => node.FullPath.Length))
            {
                Recalculate(node);
            }

            foreach (ArtifactTreeNode node in allNodes.Where(node => node.Parent is not null && node.ArtifactCount == 0))
            {
                node.Parent!.Children.Remove(node);
            }

            foreach (ArtifactTreeNode node in allNodes)
            {
                node.Children.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
            }

            return root;
        }

        private static bool IsArtifactFolder(string name) =>
            name.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || name.Equals(".vs", StringComparison.OrdinalIgnoreCase);

        private static string[] EnumerateDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
            catch (PathTooLongException)
            {
                return [];
            }
            catch (IOException)
            {
                return [];
            }
        }

        private static string[] EnumerateFiles(string path)
        {
            try
            {
                return Directory.GetFiles(path);
            }
            catch (UnauthorizedAccessException)
            {
                return [];
            }
            catch (PathTooLongException)
            {
                return [];
            }
            catch (IOException)
            {
                return [];
            }
        }

        private static bool TryGetAttributes(string path, out FileAttributes attributes)
        {
            try
            {
                attributes = File.GetAttributes(path);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                attributes = default;
                return false;
            }
            catch (PathTooLongException)
            {
                attributes = default;
                return false;
            }
            catch (IOException)
            {
                attributes = default;
                return false;
            }
        }

        private static DirectoryMetrics MeasureArtifactFolder(string rootPath, CancellationToken cancellationToken, Action<string> directoryScanned)
        {
            Stack<string> pending = new();
            pending.Push(rootPath);
            long size = 0;
            long fileCount = 0;
            DateTime? lastModifiedUtc = null;

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string currentPath = pending.Pop();
                directoryScanned(currentPath);

                DateTime? directoryWriteTime = TryGetLastWriteTimeUtc(currentPath);
                if (directoryWriteTime > lastModifiedUtc)
                {
                    lastModifiedUtc = directoryWriteTime;
                }

                foreach (string filePath in EnumerateFiles(currentPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        FileInfo file = new(filePath);
                        if (!file.Exists)
                        {
                            continue;
                        }

                        size = SaturatingAdd(size, GetAllocatedFileSize(filePath));
                        fileCount++;
                        DateTime fileWriteTime = file.LastWriteTimeUtc;
                        if (lastModifiedUtc is null || fileWriteTime > lastModifiedUtc.Value)
                        {
                            lastModifiedUtc = fileWriteTime;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (PathTooLongException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                }

                foreach (string childPath in EnumerateDirectories(currentPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (TryGetAttributes(childPath, out FileAttributes attributes) && (attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        pending.Push(childPath);
                    }
                }
            }

            return new DirectoryMetrics(size, fileCount, lastModifiedUtc);
        }

        private static DateTime? TryGetLastWriteTimeUtc(string path)
        {
            try
            {
                return Directory.GetLastWriteTimeUtc(path);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (PathTooLongException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static long SaturatingAdd(long left, long right) => long.MaxValue - left < right ? long.MaxValue : left + right;

        private static long SaturatingSum(IEnumerable<long> values)
        {
            long total = 0;
            foreach (long value in values)
            {
                total = SaturatingAdd(total, value);
            }

            return total;
        }

        private static long GetAllocatedFileSize(string path)
        {
            uint lowSize = GetCompressedFileSize(path, out uint highSize);
            if (lowSize == uint.MaxValue)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != 0)
                {
                    Win32Exception error = new(errorCode);
                    if (errorCode == 5)
                    {
                        throw new UnauthorizedAccessException($"Access denied while reading allocated file size: {path}", error);
                    }

                    if (errorCode == 206)
                    {
                        throw new PathTooLongException($"The path is too long to read allocated file size: {path}", error);
                    }

                    throw new IOException($"Could not read allocated file size for {path}.", error);
                }
            }

            ulong allocatedBytes = ((ulong)highSize << 32) | lowSize;
            return allocatedBytes > long.MaxValue ? long.MaxValue : (long)allocatedBytes;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetCompressedFileSizeW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetCompressedFileSize(string fileName, out uint fileSizeHigh);

        private static void Recalculate(ArtifactTreeNode node)
        {
            node.TotalSize = SaturatingAdd(
                SaturatingSum(node.Artifacts.Select(artifact => artifact.Size)),
                SaturatingSum(node.Children.Select(child => child.TotalSize)));
            node.ArtifactCount = node.Artifacts.Count + node.Children.Sum(child => child.ArtifactCount);
            node.FileCount = SaturatingAdd(
                SaturatingSum(node.Artifacts.Select(artifact => artifact.FileCount)),
                SaturatingSum(node.Children.Select(child => child.FileCount)));
            node.LastModifiedUtc = node.Artifacts.Select(artifact => artifact.LastModifiedUtc)
                .Concat(node.Children.Select(child => child.LastModifiedUtc))
                .Max();
        }

        private sealed record DirectoryMetrics(long Size, long FileCount, DateTime? LastModifiedUtc);
    }

    public sealed class ArtifactTreeNode
    {
        public ArtifactTreeNode(string name, string fullPath, ArtifactTreeNode? parent)
        {
            Name = string.IsNullOrEmpty(name) ? fullPath : name;
            FullPath = fullPath;
            Parent = parent;
        }

        public string Name { get; }
        public string FullPath { get; }
        public ArtifactTreeNode? Parent { get; }
        public List<ArtifactTreeNode> Children { get; } = new();
        public List<ArtifactFolder> Artifacts { get; } = new();
        public long TotalSize { get; set; }
        public long FileCount { get; set; }
        public int ArtifactCount { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
        public string NameWithSize => $"{Name} ({ArtifactScannerService.FormatSize(TotalSize)})";
    }

    public sealed class ArtifactFolder
    {
        public ArtifactFolder(string name, string fullPath, long size, long fileCount, DateTime? lastModifiedUtc, ArtifactTreeNode parent)
        {
            Name = name;
            FullPath = fullPath;
            Size = size;
            FileCount = fileCount;
            LastModifiedUtc = lastModifiedUtc;
            Parent = parent;
        }

        public string Name { get; }
        public string FullPath { get; }
        public long Size { get; }
        public long FileCount { get; }
        public DateTime? LastModifiedUtc { get; }
        public ArtifactTreeNode Parent { get; }
    }

    public sealed record ScanProgress(string CurrentPath, int ScannedDirectories, int ArtifactFoldersFound);
}