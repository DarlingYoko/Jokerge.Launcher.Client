using System.Collections.Generic;
using Gml.Dto.Files;

namespace Gml.Client.Interfaces;

/// <summary>
/// The outcome of a single pre-launch file-integrity sync: which files were
/// (re)downloaded and which stale local files were removed.
/// </summary>
public sealed class FileSyncResult
{
    public IReadOnlyList<ProfileFileReadDto> UpdatedFiles { get; }
    public IReadOnlyList<ProfileFileReadDto> DeletedFiles { get; }

    public FileSyncResult(IReadOnlyList<ProfileFileReadDto> updatedFiles, IReadOnlyList<ProfileFileReadDto> deletedFiles)
    {
        UpdatedFiles = updatedFiles;
        DeletedFiles = deletedFiles;
    }
}
