using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public enum ItemMetadataKeys {
    IsFolder,
    Name,
    RelativePath
}


public partial class FileExplorer : Control
{
    [Signal]
    public delegate void OnFileOpenEventHandler(string filePath);

    private string testDirPath = "D:/DevStream/ElPatoDraw";
    private string testTargetFolder = "Front UI";

    private Tree? treeNode;
    private FileIconProvider _fileIconProvider = new FileIconProvider();
    private int iconRelativeSize = 5;
    private int fontSize = 20;

    private FileSystemWatcher _fileSystemWatcher = new();
    private GitIntegration _gitIntegration;

    private IReadOnlyList<FileStatus> _fileStatuses = new List<FileStatus>();

    public FileExplorer() {
        _gitIntegration = new(Path.Combine(testTargetFolder, testDirPath));

        Task.Run(() => {
            _fileIconProvider.LoadIconTheme(IconThemeKey.Mocha);
            CallDeferred(FileExplorer.MethodName.InitFileIcons);
        });

        Task.Run(async () => {
            try {
            _fileStatuses = await _gitIntegration
                .GetStatus(System.Threading.CancellationToken.None);
            CallDeferred(FileExplorer.MethodName.RenderGitStatus);
            } catch (Exception ex) {
                GD.Print(ex);
            }
        });
    }

    public void RenderGitStatus()
    {
        GD.Print(treeNode);
        if (treeNode == null) return;

        foreach (var status in _fileStatuses) {
            RenderGitStatusResultOnTreeItemRecursivelly(
                treeNode.GetRoot(),
                status
            );
        }
    }

    public bool RenderGitStatusResultOnTreeItemRecursivelly(
        TreeItem item,
        FileStatus status
    )
    {
        var relativePath = (item.GetMeta(ItemMetadataKeys.RelativePath.ToString())).AsString();
        var fullPath = Path.Combine(testDirPath, relativePath);
        if (fullPath == status.FullPath) {
            if (!string.IsNullOrEmpty(status.YDisplayCode.Code)){
                item.SetText(1, status.YDisplayCode.Code);
            }
            if (!string.IsNullOrEmpty(status.YDisplayCode.Color)){
                var color = Color.FromString(status.YDisplayCode.Color, Colors.White);
                item.SetSelectable(1, false);
                item.SetCustomColor(1, color);
                item.SetCustomColor(0, color);
                item.SetTextAlignment(1, HorizontalAlignment.Center);
            }
            return true;
        }

        foreach (var child in item.GetChildren()){
            var found = RenderGitStatusResultOnTreeItemRecursivelly(child, status);
            if (found) return true;
        }

        return false;
    }

    public void InitFileIcons()
    {
        if (treeNode == null) return;
        var root = treeNode.GetRoot();
        UpdateIconTextureRecursive(root, treeNode);
    }

    public override void _Ready()
    {
        treeNode = GetNode<Tree>("%FileTree");
        treeNode.SetColumnExpand(0, true);
        treeNode.SetColumnExpandRatio(0, 1);
        treeNode.SetColumnExpandRatio(1, 0);
        treeNode.SetColumnCustomMinimumWidth(1, 35);
        fontSize = treeNode.GetThemeFontSize("font_size");

        treeNode.Theme.Changed += () => {
            var changedFontSize = treeNode.GetThemeFontSize("font_size");
            if (changedFontSize == fontSize) return;
            fontSize = changedFontSize;
            UpdateIconSizeRecursive(treeNode.GetRoot());
        };

        treeNode.ItemCollapsed += (item) => {
            var isRoot = treeNode.GetRoot() == item;
            if (_fileIconProvider.HasLoaded)
            {
                var icon = _fileIconProvider.GetFolderIcon(
                    item.GetText(0),
                    !item.Collapsed,
                    isRoot
                );
                item.SetIcon(0, icon.Texture);
            }
        };

        treeNode.ItemSelected += () => {
            var selectedItem = treeNode.GetSelected();
            var isFolderVariant = selectedItem.GetMeta(ItemMetadataKeys.IsFolder.ToString());
            if (isFolderVariant.AsBool()) return;

            var filePath = selectedItem.GetMeta(ItemMetadataKeys.RelativePath.ToString());
            EmitSignal(SignalName.OnFileOpen, filePath.AsString());
        };

        CreateTreeFolder(testDirPath, testTargetFolder, null, treeNode);
        SubscribeToDirChanges(Path.GetFullPath(testDirPath));

        AddTreeClickEventListener();
    }

    public void AddTreeClickEventListener()
    {
        if (treeNode == null) return;
        treeNode.GuiInput += (e) => {
            if (e is not InputEventMouseButton) return;
            var buttonEvent = (InputEventMouseButton) e;
            if (buttonEvent.ButtonIndex != MouseButton.Right) return;
            var menu = new PopupMenu();
            menu.AddItem("Rename");
            AddChild(menu);
            menu.Popup();

            var targetPosition = GetWindow().Position + buttonEvent.GlobalPosition;
            var windowPosition = new Vector2I((int) targetPosition.X, (int) targetPosition.Y);
            GD.Print(windowPosition);

            menu.Position = windowPosition;
        };
    }

    public void SubscribeToDirChanges(string path)
    {
        _fileSystemWatcher.Path = path;
        _fileSystemWatcher.IncludeSubdirectories = true;
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.NotifyFilter =
             NotifyFilters.CreationTime
            //| NotifyFilters.Attributes
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            //| NotifyFilters.LastAccess
            | NotifyFilters.LastWrite
            //| NotifyFilters.Security
            //| NotifyFilters.Size
            ;

        _fileSystemWatcher.Changed += (a,e) => {
            var fullPathToWatch = Path.GetFullPath(Path.Combine(path, testTargetFolder));
            if (!e.FullPath.StartsWith(fullPathToWatch)) return;
            GD.Print("Changed event: ", e.Name);
            GD.Print("Changed event: ", e.FullPath);
            GD.Print("Changed event: ", e.ChangeType);
        };

        _fileSystemWatcher.Deleted += (a,e) => {
            var fullPathToWatch = Path.GetFullPath(Path.Combine(path, testTargetFolder));
            // TODO - handle delete root folder
            if (!e.FullPath.StartsWith(fullPathToWatch)) return;
            GD.Print("Deleted: ", e.FullPath);
            var relativePath = Path.GetFullPath(e.FullPath);
            // TODO - what happends if null
            if (treeNode == null) return;
            var found = FindItemInUI(relativePath, treeNode.GetRoot());
            if (found == null) return;
            found.GetParent().CallDeferred(Node.MethodName.RemoveChild, found);
        };

        _fileSystemWatcher.Renamed += (_,e) => {
            var fullPathToWatch = Path.GetFullPath(Path.Combine(path, testTargetFolder));
            if (
                !e.FullPath.StartsWith(fullPathToWatch) &&
                e.OldFullPath != fullPathToWatch
            ) return;
            GD.Print("Renamed: ", e.FullPath);

            var relativePath = Path.GetFullPath(e.OldFullPath);
            // TODO - what happends if null
            if (treeNode == null) return;

            var found = FindItemInUI(relativePath, treeNode.GetRoot());
            if (found == null) return;

            var newName = Path.GetFileName(e.Name)!;
            var itemDirectory = Path.GetDirectoryName(e.FullPath)!;
            CallDeferred(
                FileExplorer.MethodName.UpdateTreeItemName,
                found,
                newName,
                itemDirectory
            );
        };

        _fileSystemWatcher.Created += (_,e) => {
            if (!e.FullPath.StartsWith(path)) return;
            GD.Print("Created Path: ", e.FullPath);
            var parentFolderPath = Path.GetDirectoryName(e.FullPath);
            var itemName = Path.GetFileName(e.FullPath);

            var isFile = Godot.FileAccess.FileExists(e.FullPath);
            var isFolder = DirAccess.DirExistsAbsolute(e.FullPath);
            if (treeNode == null) return;

            var foundItem = FindItemInUI(parentFolderPath, treeNode.GetRoot());
            if (foundItem == null) return;

            var parentFolder = DirAccess.Open(parentFolderPath);

            if (isFile) {
                var files = parentFolder
                    .GetFiles()
                    .OrderBy(f => f)
                    .ToArray();

                var index = Array.BinarySearch(files, 0, files.Count(), itemName);
                index += parentFolder.GetDirectories().Count();

                CallDeferred(
                    FileExplorer.MethodName.CreateTreeFile,
                    parentFolderPath,
                    itemName,
                    foundItem,
                    treeNode,
                    index
                );
            }

            if (isFolder) {
                var files = parentFolder
                    .GetDirectories()
                    .OrderBy(f => f)
                    .ToArray();

                var index = Array.BinarySearch(files, 0, files.Count(), itemName);

                CallDeferred(
                    FileExplorer.MethodName.CreateTreeFolder,
                    parentFolderPath,
                    itemName,
                    foundItem,
                    treeNode,
                    index
                );
            }
        };
    }

    public void UpdateTreeItemName(TreeItem node, string newName, string itemDirectory)
    {
        node.SetText(0, newName);

        var isRoot = treeNode?.GetRoot() == node;
        var isFolder = (node.GetMeta(ItemMetadataKeys.IsFolder.ToString(), newName)).AsBool();

        if (_fileIconProvider.HasLoaded){
            var icon = isFolder ?
                _fileIconProvider.GetFolderIcon(newName, !node.Collapsed, isRoot) :
                _fileIconProvider.GetFileIcon(newName);
            node.SetIcon(0, icon.Texture);
        }
        node.SetMeta(ItemMetadataKeys.Name.ToString(), newName);

        if (isRoot) {
            testTargetFolder = newName;
        } else {
            var parentNode = node.GetParent();
            var children = parentNode.GetChildren();

            var files =  children.Where(item =>
                !(item.GetMeta(ItemMetadataKeys.IsFolder.ToString())).AsBool()
            );

            var folders = children.Where(item =>
                (item.GetMeta(ItemMetadataKeys.IsFolder.ToString())).AsBool()
            );

            var childrenSorted = folders
                .OrderBy(p => p.GetText(0))
                .Concat(
                    files
                        .OrderBy(p => p.GetText(0))
                        .ToList()
                )
                .ToList();

            var foundIndex = childrenSorted.FindIndex(0, children.Count, (item) => (
                item == node
            )) - 1;

            if (foundIndex < 0) {
                var firstChild = children[0];
                node.MoveBefore(firstChild);
            } else {
                var item = childrenSorted[foundIndex];
                node.MoveAfter(item);
            }
        }

        UpdatePathRecursively(node, itemDirectory);
    }

    public void UpdatePathRecursively(TreeItem node, string parentFolderPath)
    {
        var name = (node.GetMeta(ItemMetadataKeys.Name.ToString())).AsString();
        var newFullPath = Path.Combine(parentFolderPath, name);
        node.SetMeta(ItemMetadataKeys.RelativePath.ToString(), newFullPath);

        foreach (var child in node.GetChildren())
        {
            UpdatePathRecursively(child, newFullPath);
        }
    }

    public TreeItem? FindItemInUI(string relativePath, TreeItem node)
    {
        var currentRelativePath = (node.GetMeta(ItemMetadataKeys.RelativePath.ToString()))
            .AsString();
        if (currentRelativePath == relativePath) return node;

        foreach (var child in node.GetChildren()) {
            var foundChild = FindItemInUI(relativePath, child);
            if (foundChild != null) return foundChild;
        }

        return null;
    }

    public void UpdateIconSizeRecursive(TreeItem treeItem)
    {
        treeItem.SetIconMaxWidth(0, fontSize + iconRelativeSize);

        foreach (var child in treeItem.GetChildren()){
            UpdateIconSizeRecursive(child);
        }
    }

    public void UpdateIconTextureRecursive(TreeItem treeItem, Tree tree)
    {
        var isFolder = (treeItem.GetMeta(ItemMetadataKeys.IsFolder.ToString())).AsBool();
        var isOpen = !treeItem.Collapsed;
        var isRoot = tree.GetRoot() == treeItem;

        var icon = isFolder ?
            _fileIconProvider.GetFolderIcon(treeItem.GetText(0), isOpen, isRoot) :
            _fileIconProvider.GetFileIcon(treeItem.GetText(0));
        treeItem.SetIcon(0, icon.Texture);

        foreach (var child in treeItem.GetChildren())
        {
            UpdateIconTextureRecursive(child, tree);
        }
    }

    public TreeItem CreateTreeFolder(
      string path,
      string folder,
      TreeItem? parentItem,
      Tree tree,
      int index = -1
    )
    {
        var currentPath = Path.GetFullPath(Path.Combine(path, folder));
        var isRoot = parentItem == null;

        var node = tree.CreateItem(parentItem, index);
        node.SetText(0, folder);
        node.SetIconMaxWidth(0, fontSize + iconRelativeSize);
        node.SetSelectable(0, false);
        node.SetSelectable(1, false);
        node.Collapsed = !isRoot;

        if (_fileIconProvider.HasLoaded)
        {
            var icon = _fileIconProvider.GetFolderIcon(folder, isRoot, isRoot);
            node.SetIcon(0, icon.Texture);
        }

        node.SetMeta(ItemMetadataKeys.IsFolder.ToString(), true);
        node.SetMeta(ItemMetadataKeys.RelativePath.ToString(), currentPath);
        node.SetMeta(ItemMetadataKeys.Name.ToString(), folder);

        var dir = DirAccess.Open(currentPath);
        if (dir == null) return node;
        dir.IncludeHidden = true;

        var subDirectoryNames = dir
            .GetDirectories()
            .OrderBy(name => name);

        var files = dir
            .GetFiles()
            .OrderBy(name => name);

        foreach (var subDirectoryName in subDirectoryNames)
        {
            CreateTreeFolder(currentPath, subDirectoryName, node, tree);
        }

        foreach (var fileName in files)
        {
            CreateTreeFile(currentPath, fileName, node, tree);
        }

        return node;
    }

    public TreeItem CreateTreeFile(
        string currentPath,
        string fileName,
        TreeItem parentNode,
        Tree tree,
        int index = -1
    ){
        var filePath = Path.GetFullPath(Path.Combine(currentPath, fileName));
        var fileNode = tree.CreateItem(parentNode, index);

        fileNode.SetText(0, fileName);
        fileNode.SetIconMaxWidth(0, fontSize + iconRelativeSize);
        fileNode.SetSelectable(1, false);

        if (_fileIconProvider.HasLoaded) {
            var fileIcon = _fileIconProvider.GetFileIcon(fileName);
            fileNode.SetIcon(0, fileIcon.Texture);
        }

        fileNode.SetMeta(ItemMetadataKeys.IsFolder.ToString(), false);
        fileNode.SetMeta(ItemMetadataKeys.RelativePath.ToString(), filePath);
        fileNode.SetMeta(ItemMetadataKeys.Name.ToString(), fileName);

        return fileNode;
    }
}
