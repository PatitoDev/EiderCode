using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EiderCode.FileSystem;

public record FileSystemItem {
  public required bool IsRoot { get; init; }
  public required bool IsFolder { get; init; }
  public required string Path { get; init; }
  public required string Name { get; init; }
  public required IList<FileSystemItem> SubItems { get; init; }
}

public class FileSystemManager
{
    private FileSystemWatcher _fsWatcher;
    private string FolderPath;

    public FileSystemManager(string folderPath)
    {
      FolderPath = Path.GetFullPath(folderPath);
      _fsWatcher = new FileSystemWatcher();
      //SubscribeToDirChanges(folderPath);
    }

    public FileSystemItem GetDirectory()
    {
      var separator = Path.DirectorySeparatorChar;
      var root = new FileSystemItem()
      {
          IsFolder = true,
          IsRoot = true,
          Name = FolderPath.Split(separator).Last(),
          Path = FolderPath,
          SubItems = new List<FileSystemItem>()
      };

      var directoryMap = new Dictionary<string, FileSystemItem> {
          {  FolderPath, root }
      };

      var files = Directory.EnumerateFiles(FolderPath, "*.*", SearchOption.AllDirectories);
      var directories = Directory.EnumerateDirectories(FolderPath, "*.*", SearchOption.AllDirectories);


      var directoriesOrderedByDepth = directories
        .Select(path => {
          //var relativePath = Path.GetFullPath(path).Replace(FolderPath, "");
          var pathSplitted = path
            .Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
          return new {
              FullPath = Path.GetFullPath(path),
              PathSplitted = pathSplitted
          };
        }).OrderBy(s => s.PathSplitted.Length);

      foreach (var directoryPaths in directoriesOrderedByDepth) {
        var folderName = directoryPaths.PathSplitted.Last();
        if (string.IsNullOrEmpty(folderName)) continue;

        var parentFolderPathList = directoryPaths.PathSplitted.SkipLast(1);

        var dir = new FileSystemItem(){
          IsFolder = true,
          IsRoot = false,
          Name = folderName,
          Path = directoryPaths.FullPath,
          SubItems = new List<FileSystemItem>()
        };

        if (parentFolderPathList.Any()){
          var parentFolderPath = Path.Combine(parentFolderPathList.ToArray());
          var parentFolder = directoryMap[parentFolderPath];
          parentFolder.SubItems.Add(dir);
        }

        directoryMap.Add(directoryPaths.FullPath, dir);
      }

      foreach (var filePath in files){
        var filePathSplitted = filePath.Split(separator);
        var fileName = filePathSplitted.Last();
        var parentFolderPath = Path.Join(filePathSplitted.SkipLast(1).ToArray());

        var parentFolderO = directoryMap[parentFolderPath];
        parentFolderO.SubItems.Add(new(){
          IsFolder = false,
          IsRoot = false,
          Name = fileName,
          Path = filePath,
          SubItems = new List<FileSystemItem>()
        });
      }

      return root;
    }

    private void SubscribeToDirChanges(string folderPath)
    {
      var pathSplitted = folderPath.Split(Path.DirectorySeparatorChar);
      var folderName = pathSplitted.Last();
      var parentFolder = pathSplitted.ElementAt(-2);

      // we watch the parent folder so that we can track changes to the actual folder
        _fsWatcher.Path = parentFolder;
        _fsWatcher.IncludeSubdirectories = true;
        _fsWatcher.EnableRaisingEvents = true;
        _fsWatcher.NotifyFilter =
             NotifyFilters.CreationTime
            //| NotifyFilters.Attributes
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            //| NotifyFilters.LastAccess
            | NotifyFilters.LastWrite
            //| NotifyFilters.Security
            //| NotifyFilters.Size
            ;

        _fsWatcher.Changed += (a,e) => {
            if (!e.FullPath.StartsWith(folderPath)) return;
            // handle change ev
        };

        _fsWatcher.Deleted += (a,e) => {
            // TODO - handle delete root folder
            if (!e.FullPath.StartsWith(folderPath)) return;
            /*
            GD.Print("Deleted: ", e.FullPath);
            var relativePath = Path.GetFullPath(e.FullPath);
            // TODO - what happends if null
            if (treeNode == null) return;
            var found = FindItemInUI(relativePath, treeNode.GetRoot());
            if (found == null) return;
            found.GetParent().CallDeferred(Node.MethodName.RemoveChild, found);
            */
        };

        _fsWatcher.Renamed += (_,e) => {
            if (
                !e.FullPath.StartsWith(folderPath) &&
                e.OldFullPath != folderPath
            ) return;

            /*
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
            */
        };

        _fsWatcher.Created += (_,e) => {
            if (!e.FullPath.StartsWith(folderPath)) return;
            /*
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
          */
        };
    }
}