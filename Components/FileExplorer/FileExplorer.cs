using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    //private GitIntegration _gitIntegration;

    private IReadOnlyList<FileStatus> _fileStatuses = new List<FileStatus>();

    public FileExplorer() {
        /*
        _gitIntegration = new(Path.Combine(testTargetFolder, testDirPath));
        Task.Run(async () => {
            try {
            _fileStatuses = await _gitIntegration
                .GetStatus(System.Threading.CancellationToken.None);
            CallDeferred(FileExplorer.MethodName.RenderGitStatus);
            } catch (Exception ex) {
                GD.Print(ex);
            }
        });
        */
    }

    public override void _Ready()
    {
        treeNode = GetNode<Tree>("%FileTree");
        treeNode.SetColumnExpand(0, true);
        treeNode.SetColumnExpandRatio(0, 1);
        treeNode.SetColumnExpandRatio(1, 0);
        treeNode.SetColumnCustomMinimumWidth(1, 35);

        fontSize = treeNode.GetThemeFontSize("font_size");

        /*
        treeNode.Theme.Changed += () => {
            var changedFontSize = treeNode.GetThemeFontSize("font_size");
            if (changedFontSize == fontSize) return;
            fontSize = changedFontSize;
            UpdateIconSizeRecursive(treeNode.GetRoot());
        };
        */

        treeNode.ItemCollapsed += (item) => {
            var isRoot = treeNode.GetRoot() == item;
            if (!_fileIconProvider.HasLoaded) return;

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

            var filePath = selectedItem.GetMeta(ItemMetadataKeys.RelativePath.ToString());
            EmitSignal(SignalName.OnFileOpen, filePath.AsString());
        };

        var t = new Stopwatch();
        t.Start();
        var fs = new FileSystemManager(Path.Combine(testDirPath, testTargetFolder));
        var root = fs.GetDirectory();
        _fileIconProvider.LoadIconTheme(IconThemeKey.Mocha);
        //CallDeferred(FileExplorer.MethodName.InitFileIcons)

        RenderTreeItemFolder(root, treeNode, null);
        t.Stop();
        GD.Print("Loaded tree in: ",t.ElapsedMilliseconds);

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


    /*
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
    */
    /*
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
    */

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


    public void InitFileIcons()
    {
        if (treeNode == null) return;
        var root = treeNode.GetRoot();
        UpdateIconTextureRecursive(root, treeNode);
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

    public void RenderTreeItemFolder(FileSystemItem fsItem, Tree tree, TreeItem? parentItem, int index = -1, int depth = 0) {
        var node = tree.CreateItem(parentItem, index);
        node.SetText(0, fsItem.Name);
        node.SetIconMaxWidth(0, fontSize + iconRelativeSize);
        node.SetSelectable(1, false);
        node.SetMeta(ItemMetadataKeys.IsFolder.ToString(), fsItem.IsFolder);
        node.SetMeta(ItemMetadataKeys.RelativePath.ToString(), fsItem.Path);
        node.SetMeta(ItemMetadataKeys.Name.ToString(), fsItem.Name);

        if (fsItem.IsFolder){
            node.Collapsed = !fsItem.IsRoot;
            node.SetSelectable(0, false);
        } else {
            node.DisableFolding = true;
        }

        if (_fileIconProvider.HasLoaded)
        {
            var icon = fsItem.IsFolder ?_fileIconProvider.GetFolderIcon(fsItem.Name, fsItem.IsRoot, fsItem.IsRoot)
                : _fileIconProvider.GetFileIcon(fsItem.Name);

            node.SetIcon(0, icon.Texture);
        }

        foreach(var subItem in fsItem.SubItems)
        {
            RenderTreeItemFolder(subItem, tree, node, -1, subItem.IsFolder ? depth + 1 : 0);
        }

    }


    /*
    public void RenderGitStatus()
    {
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
    */
}
