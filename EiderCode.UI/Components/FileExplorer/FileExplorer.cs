using EiderCode.FileSystem;
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EiderCode.UI;

public enum ItemMetadataKeys
{
    IsFolder,
    Name,
    RelativePath
}

public record FileTreeItem
{
    public required bool IsOpen { get; set; }
    public required bool IsFolder { get; set; }
    public required bool HasAccess { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required int Depth { get; set; }
    public required FileStatusEnum GitStatus { get; set; }
    public required FileSystemItem TreeObject { get; set; }
}

// TODO
// * fix text alignment on different font sizes and paddings
// * fix git icons rendering on top of file names. File names should be trimmed
public partial class FileExplorer : Control
{
    [Signal]
    public delegate void OnFileOpenEventHandler(string filePath);

    private string testDirPath = "D:/";
    private string testTargetFolder = "editor";

    private FileSystemWatcher _fileSystemWatcher = new();
    private FileIconProvider _fileIconProvider = new FileIconProvider();

    // settings
    private int iconRelativeSize = 5;
    private int fontSize = 20;
    private int itemDepthOffset = 25;
    private int itemPadding = 5;
    private int itemMargin = 10;

    private TextServer _textServer;
    private Font _font;
    private Rid _canvasId;

    // what we are rendering in the exact order they appear on the ui
    // this is a flat structure
    private List<FileTreeItem> _treeItems = new();
    private FileSystemItem? _rootFolder;

    private CompressedTexture2D? _triangleIconOpen;
    private CompressedTexture2D? _triangleIconClose;

    private GitIntegration _gitIntegration;
    private Dictionary<string, FileStatus> _fileStatuses = new();

    private int? _itemIndexHovered = null;
    private string? _itemPathSelected = null;

    public FileExplorer()
    {
        _font = Theme.DefaultFont;

        Task.Run(() =>
        {
            LoadAsync();
            CallDeferred(FileExplorer.MethodName.QueueRedraw);
        });

        _textServer = TextServerManager.GetPrimaryInterface();

        _canvasId = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasId, GetCanvasItem());

        _gitIntegration = new(Path.Combine(testTargetFolder, testDirPath));
        Task.Run(async () =>
        {
            try
            {
                _fileStatuses = await _gitIntegration
                    .GetStatus(System.Threading.CancellationToken.None);
                CallDeferred(FileExplorer.MethodName.QueueRedraw);
            }
            catch (Exception ex)
            {
                GD.Print(ex);
            }
        });
    }

    private bool HasLoaded = false;

    public void LoadAsync()
    {
        var fs = new FileSystemManager(Path.Combine(testDirPath, testTargetFolder));
        _rootFolder = fs.GetDirectory();

        _fileIconProvider.LoadIconTheme(IconThemeKey.Mocha);

        _treeItems.Add(new()
        {
            Depth = 0,
            GitStatus = FileStatusEnum.Unmodified,
            HasAccess = true,
            IsOpen = true,
            IsFolder = _rootFolder.IsFolder,
            Name = _rootFolder.Name,
            Path = _rootFolder.Path,
            TreeObject = _rootFolder
        });
        _triangleIconClose = GD.Load<CompressedTexture2D>("uid://bat48y1dfgxxn");
        _triangleIconOpen = GD.Load<CompressedTexture2D>("uid://dmj0phpd2n4ck");

        OpenFolder(0);
        CallDeferred(FileExplorer.MethodName.QueueRedraw);
    }

    public override void _Draw()
    {
        base._Draw();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        RenderTree();
        ResizeContainer();
        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 100)
        {
            GD.Print("Rendered Tree in: ", stopwatch.ElapsedMilliseconds);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (@event is InputEventMouseMotion)
        {
            var mouseMotionEvent = (InputEventMouseMotion)@event;
            var itemIndex = GetTreeItemFromMousePosition(mouseMotionEvent.Position);
            _itemIndexHovered = itemIndex;
            QueueRedraw();
        }

        if (@event is InputEventMouseButton)
        {
            var buttonEvent = (InputEventMouseButton)@event;
            if (
                buttonEvent.ButtonIndex == MouseButton.Left &&
                buttonEvent.Pressed
            )
            {

                var itemIndex = GetTreeItemFromMousePosition(buttonEvent.Position);
                var treeItem = _treeItems[itemIndex];

                if (treeItem.IsFolder)
                {
                    if (treeItem.IsOpen)
                    {
                        CloseFolder(itemIndex);
                    }
                    else
                    {
                        OpenFolder(itemIndex);
                    }

                    ResizeContainer();
                    QueueRedraw();
                    return;
                }

                _itemPathSelected = treeItem.Path;
                EmitSignal(SignalName.OnFileOpen, treeItem.Path);
            }
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationMouseExit)
        {
            _itemIndexHovered = null;
            QueueRedraw();
        }
    }

    public void ResizeContainer()
    {
        var itemSize = (itemPadding * 2) + fontSize;
        Size = new Vector2(Size.X, itemSize * (_treeItems.Count + 5));
        CustomMinimumSize = new Vector2(0, itemSize * (_treeItems.Count + 5));
    }

    public void CloseFolder(int treeIndex)
    {
        var item = _treeItems[treeIndex]!;
        item.IsOpen = false;
        var currentDepth = item.Depth;

        var fileTreeItemsToRemove = new List<FileTreeItem>();
        for (var i = treeIndex + 1; i < _treeItems.Count; i++)
        {
            var subItem = _treeItems[i];
            if (
                subItem.Depth <= currentDepth
            )
            {
                // has finished traversing sub items so return
                break;
            }

            // depth is greater
            fileTreeItemsToRemove.Add(subItem);
        }

        foreach (var itemToRemove in fileTreeItemsToRemove)
        {
            _treeItems.Remove(itemToRemove);
        }
    }

    public void OpenFolder(int treeIndex)
    {
        var item = _treeItems[treeIndex]!;
        var subItemDepth = item.Depth + 1;

        item.IsOpen = true;

        var subItems = item
            .TreeObject
            .SubItems
            .OrderBy(subItem => subItem.IsFolder)
            .ThenByDescending(subItem => subItem.Name)
            .ToArray();

        foreach (var subItem in subItems)
        {
            _treeItems.Insert(treeIndex + 1, new()
            {
                Depth = subItemDepth,
                GitStatus = FileStatusEnum.Unmodified,
                IsOpen = false,
                HasAccess = true,
                IsFolder = subItem.IsFolder,
                Name = subItem.Name,
                Path = subItem.Path,
                TreeObject = subItem
            });
        }

        CallDeferred(FileExplorer.MethodName.QueueRedraw);
    }

    private int GetTreeItemFromMousePosition(Vector2 mousePos)
    {
        var itemHeight = fontSize + (itemPadding * 2);
        var index = (int)Math.Floor(mousePos.Y / itemHeight);
        return index;
    }

    private Rect2 GetTreeItemContainerSize(int index, int depth = 0)
    {
        var height = fontSize + (itemPadding * 2);
        var x = (depth * itemDepthOffset);
        var y = (index * height);

        // top left
        var containerRect = new Rect2(
            new Vector2(x, y),
            new Vector2(Size.X, height)
        );

        return containerRect;
    }

    public void RenderTree()
    {
        RenderingServer.CanvasItemClear(_canvasId);

        var index = 0;

        foreach (var item in _treeItems)
        {
            var containerRect = GetTreeItemContainerSize(index, item.Depth);


            RenderItemBackground(item, index, containerRect);

            if (item.IsFolder)
            {
                RenderFolderTriangle(item, containerRect);
            }

            if (_fileIconProvider.HasLoaded)
            {
                RenderItemFileIcon(item, containerRect);
            }

            RenderItemName(item, containerRect);
            RenderGitStatus(item, containerRect);

            index += 1;
        }
    }

    public void RenderItemBackground(FileTreeItem item, int index, Rect2 containerRect)
    {
        // selection box
        if (
            _itemIndexHovered != index &&
            _itemPathSelected != item.Path
        ) return;

        var box = new Rect2(
            new Vector2(0, containerRect.Position.Y),
            new Vector2(Size.X, containerRect.Size.Y)
            );

        RenderingServer.CanvasItemAddRect(
            _canvasId,
            box,
            Color.FromString(
                _itemIndexHovered == index ? "#64437017" : "#64437047"
                , Colors.White)
        );
    }

    public void RenderFolderTriangle(FileTreeItem item, Rect2 containerSize)
    {
        if (_triangleIconClose == null || _triangleIconOpen == null) return;

        var triangleIconSize = (fontSize + iconRelativeSize) * 2;

        var triangleCenter = triangleIconSize / 2;
        var containerCenter = containerSize.Size.Y / 2;
        var centerDiff = containerCenter - triangleCenter;

        var triangleIconPosition = new Vector2(
            containerSize.Position.X,
            containerSize.Position.Y + centerDiff
        );

        RenderingServer.CanvasItemAddTextureRect(
            _canvasId,
            new Rect2(
                triangleIconPosition,
                new Vector2(triangleIconSize, triangleIconSize)
            ),
            item.IsOpen ? _triangleIconOpen.GetRid() : _triangleIconClose.GetRid()
        );
    }

    public void RenderItemFileIcon(FileTreeItem item, Rect2 containerRect)
    {
        var icon = item.IsFolder ?
            _fileIconProvider.GetFolderIcon(item.Name, item.IsOpen, item.Depth == 0) :
            _fileIconProvider.GetFileIcon(item.Name);

        var itemIconSize = new Vector2I(
            fontSize + iconRelativeSize,
            fontSize + iconRelativeSize
        );

        var x = containerRect.Position.X + (fontSize * 2);

        var iconCenter = itemIconSize.Y / 2;
        var diff = (containerRect.Size.Y / 2) - iconCenter;
        var y = containerRect.Position.Y + diff;

        var itemIconPosition = new Vector2(x, y);

        RenderingServer.CanvasItemAddTextureRect(
            _canvasId,
            new Rect2(itemIconPosition, itemIconSize),
            icon.Texture.GetRid()
        );

        x += itemIconSize.X + itemMargin;

    }

    public void RenderItemName(FileTreeItem item, Rect2 containerRect)
    {
        var textRid = _textServer.CreateShapedText();

        _textServer.ShapedTextAddString(
            textRid,
            item.Name,
            _font.GetRids(),
            fontSize
        );

        var x = (int) (containerRect.Position.X + (fontSize * 3.5));

        var textBounds = _textServer.ShapedTextGetSize(textRid);
        var textCenter = textBounds.Y / 2;
        var containerCenter = containerRect.Size.Y / 2;
        var centerDiff = containerCenter - textCenter;

        var des = _textServer.ShapedTextGetDescent(textRid);
        var y = (int)(containerRect.Position.Y + textCenter + des);

        var isSelected = _itemPathSelected == item.Path;

        _textServer.ShapedTextDraw(
            textRid,
            _canvasId,
            new Vector2(x, y),
            -1,
            -1,
            Color.FromString(
                isSelected ?
                "#b9bed4" :
                "#878ca5"
            , Colors.White)
        );

        _textServer.FreeRid(textRid);
    }

    public void RenderGitStatus(FileTreeItem item, Rect2 containerRect)
    {
        var status = _fileStatuses.GetValueOrDefault(item.Path);
        if (status == null) return;

        var labelRid = _textServer.CreateShapedText();

        _textServer.ShapedTextAddString(
            labelRid,
            status.YDisplayCode.Code,
            _font.GetRids(),
            fontSize
        );

        var textBounds = _textServer.ShapedTextGetSize(labelRid);
        var textCenter = textBounds.Y / 2;
        var containerCenter = containerRect.Size.Y / 2;
        var centerDiff = containerCenter - textCenter;

        var des = _textServer.ShapedTextGetDescent(labelRid);
        var y = (int)(containerRect.Position.Y + textCenter + des);

        var marginFromGutter = 10;
        var x = Size.X - textBounds.X - marginFromGutter;

        _textServer.ShapedTextDraw(
            labelRid,
            _canvasId,
            new Vector2(x, y),
            -1,
            -1,
            Color.FromString(status.YDisplayCode.Color, Colors.White)
        );

        _textServer.FreeRid(labelRid);
    }
}
