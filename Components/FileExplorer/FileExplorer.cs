using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum ItemMetadataKeys {
    IsFolder,
    Name,
    FullPath
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

    public override void _Ready()
    {
        treeNode = GetNode<Tree>("%FileTree");
        fontSize = treeNode.GetThemeFontSize("font_size");

        treeNode.Theme.Changed += () => {
            var changedFontSize = treeNode.GetThemeFontSize("font_size");
            if (changedFontSize == fontSize) return;
            fontSize = changedFontSize;
            UpdateIconSizeRecursive(treeNode.GetRoot());
        };

        treeNode.ItemCollapsed += (item) => {
            var isRoot = treeNode.GetRoot() == item;
            var icon = _fileIconProvider.GetFolderIcon(
                item.GetText(0),
                !item.Collapsed,
                isRoot
            );
            item.SetIcon(0, icon.Texture);
        };

        treeNode.ItemSelected += () => {
            var selectedItem = treeNode.GetSelected();
            var isFolderVariant = selectedItem.GetMeta(ItemMetadataKeys.IsFolder.ToString());
            if (isFolderVariant.AsBool()) return;

            var filePath = selectedItem.GetMeta(ItemMetadataKeys.FullPath.ToString());
            EmitSignal(SignalName.OnFileOpen, filePath.AsString());
        };

        CreateTreeFolder(testDirPath, testTargetFolder, null, treeNode);
        SubscribeToDirChanges(Path.GetFullPath(testDirPath));

        HandleTreeClick();
    }

    public void HandleTreeClick()
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
        var watcher = new FileSystemWatcher(path);
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter =
             NotifyFilters.CreationTime
            //| NotifyFilters.Attributes
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            //| NotifyFilters.LastAccess
            | NotifyFilters.LastWrite
            //| NotifyFilters.Security
            //| NotifyFilters.Size
            ;

        watcher.Changed += (a,e) => {
            var fullPathToWatch = Path.GetFullPath(Path.Combine(path, testTargetFolder));
            if (!e.FullPath.StartsWith(fullPathToWatch)) return;
            //GD.Print("Changed event: ", e.FullPath);
            //GD.Print("Changed event: ", e.ChangeType);
        };

        watcher.Deleted += (a,e) => {
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

        watcher.Renamed += (_,e) => {
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

        watcher.Created += (_,e) => {
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
        var icon = isFolder ?
            _fileIconProvider.GetFolderIcon(newName, !node.Collapsed, isRoot) :
            _fileIconProvider.GetFileIcon(newName);

        node.SetIcon(0, icon.Texture);
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
        node.SetMeta(ItemMetadataKeys.FullPath.ToString(), newFullPath);

        foreach (var child in node.GetChildren())
        {
            UpdatePathRecursively(child, newFullPath);
        }
    }

    public TreeItem? FindItemInUI(string relativePath, TreeItem node)
    {
        var currentRelativePath = (node.GetMeta(ItemMetadataKeys.FullPath.ToString()))
            .AsString();
        //GD.Print(relativePath + " ", currentRelativePath);
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
        var icon = _fileIconProvider.GetFolderIcon(folder, isRoot, isRoot);

        var node = tree.CreateItem(parentItem, index);
        node.SetText(0, folder);
        node.SetIconMaxWidth(0, fontSize + iconRelativeSize);
        node.SetIcon(0, icon.Texture);
        node.SetSelectable(0, false);
        node.Collapsed = !isRoot;

        node.SetMeta(ItemMetadataKeys.IsFolder.ToString(), true);
        node.SetMeta(ItemMetadataKeys.FullPath.ToString(), currentPath);
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
        var fileIcon = _fileIconProvider.GetFileIcon(fileName);
        var fileNode = tree.CreateItem(parentNode, index);

        fileNode.SetText(0, fileName);
        fileNode.SetIconMaxWidth(0, fontSize + iconRelativeSize);
        fileNode.SetIcon(0, fileIcon.Texture);

        fileNode.SetMeta(ItemMetadataKeys.IsFolder.ToString(), false);
        fileNode.SetMeta(ItemMetadataKeys.FullPath.ToString(), filePath);
        fileNode.SetMeta(ItemMetadataKeys.Name.ToString(), fileName);

        return fileNode;
    }
}
