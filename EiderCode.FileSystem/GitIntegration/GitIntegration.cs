using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace EiderCode.FileSystem;

public enum FileStatusEnum
{
    Unmodified,
    Untracked,
    Modified,
    TypeChanged,
    Added,
    Deleted,
    Renamed,
    Copied,
    Updated,
    Ignored
}

public record FileStatus
{
    public required string FullPath { get; init; }
    // X status of the index
    public required FileStatusEnum X { get; init; }
    public required FileStatusDisplayCode XDisplayCode { get; init; }
    // Y working tree in a merge scenario with no conflicts
    public required FileStatusEnum Y { get; init; }
    public required FileStatusDisplayCode YDisplayCode { get; init; }
}

public record FileStatusDisplayCode
{
    public required string Code { get; init; }
    public required string Color { get; init; }
}

public class GitIntegration
{
    private IReadOnlyDictionary<FileStatusEnum, FileStatusDisplayCode> _statusToDisplayCode =
        new Dictionary<FileStatusEnum, FileStatusDisplayCode>(){
            { FileStatusEnum.Unmodified, new FileStatusDisplayCode(){ Code = "", Color = "" } },
            { FileStatusEnum.Untracked, new FileStatusDisplayCode(){ Code = "U", Color = "#a6e3a1" } },
            { FileStatusEnum.Ignored, new FileStatusDisplayCode(){ Code = "", Color = "" } },
            { FileStatusEnum.Modified, new FileStatusDisplayCode(){ Code = "M", Color = "#fab387" } },
            { FileStatusEnum.TypeChanged, new FileStatusDisplayCode(){ Code = "T", Color = "#a6e3a1" } },
            { FileStatusEnum.Added, new FileStatusDisplayCode(){ Code = "A", Color = "#a6e3a1" } },
            { FileStatusEnum.Deleted, new FileStatusDisplayCode(){ Code = "D", Color = "#f38ba8" } },
            { FileStatusEnum.Renamed, new FileStatusDisplayCode(){ Code = "R", Color = "#a6e3a1" } },
            { FileStatusEnum.Copied, new FileStatusDisplayCode(){ Code = "C", Color = "#a6e3a1" } },
            { FileStatusEnum.Updated, new FileStatusDisplayCode(){ Code = "U", Color = "#a6e3a1" } },
    };

    private IReadOnlyDictionary<char, FileStatusEnum> _charToFileStatusMap =
        new Dictionary<char, FileStatusEnum>(){
            { ' ', FileStatusEnum.Unmodified  },
            { '?', FileStatusEnum.Untracked  },
            { '!', FileStatusEnum.Ignored  },
            { 'M', FileStatusEnum.Modified  },
            { 'T', FileStatusEnum.TypeChanged  },
            { 'A', FileStatusEnum.Added  },
            { 'D', FileStatusEnum.Deleted  },
            { 'R', FileStatusEnum.Renamed  },
            { 'C', FileStatusEnum.Copied  },
            { 'U', FileStatusEnum.Updated  },
    };

    private readonly string _workingDirectory;

    public GitIntegration(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    async private Task<string> ExecuteGitCommandAsync(
        string arguments,
        CancellationToken cancellationToken
    )
    {
        var process = new Process();
        process.StartInfo.WorkingDirectory = _workingDirectory;
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = arguments;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        string err = "";
        string output = "";

        process.ErrorDataReceived += (_, e) =>
        {
            if (
                e.Data == null ||
                e.Data.StartsWith("warning")
            ) return;
            err += e.Data;
        };
        process.OutputDataReceived += (_, e) =>
        {
            output += e.Data;
        };

        var result = process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return output;
    }

    async private Task<string> GetProjectFullPathAsync(CancellationToken cancellationToken)
    {
        var arguments = "rev-parse --show-toplevel";
        var output = await ExecuteGitCommandAsync(arguments, cancellationToken);
        return output;
    }

    async public Task<Dictionary<string, FileStatus>> GetStatus(CancellationToken cancellationToken)
    {
        var projectPath = await GetProjectFullPathAsync(cancellationToken);

        var arguments = "status --porcelain=1 -z --ignored";
        var output = await ExecuteGitCommandAsync(arguments, cancellationToken);

        var status = output.Split("\0", false);

        var fileStatuses = status.Select(s =>
        {
            var statusCodes = s.Substring(0, 2);
            var x = statusCodes[0];
            var y = statusCodes[1];

            var filePath = s.Substring(3);

            var fullPath = Path.GetFullPath(Path.Combine(projectPath, filePath));
            if (fullPath.LastIndexOf("\\") == fullPath.Length - 1){
                fullPath = fullPath.Substring(0, fullPath.Length - 1);
            }

            var xCode = _charToFileStatusMap[x];
            var yCode = _charToFileStatusMap[y];

            return new FileStatus()
            {
                X = xCode,
                XDisplayCode = _statusToDisplayCode[xCode],
                Y = yCode,
                YDisplayCode = _statusToDisplayCode[yCode],
                FullPath = fullPath
            };
        })
        .ToDictionary(s => s.FullPath, s => s);

        return fileStatuses;
    }
}