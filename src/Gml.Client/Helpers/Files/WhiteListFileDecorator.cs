
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gml.Dto.Files;
using Gml.Dto.Profile;

namespace Gml.Client.Helpers.Files;

public class WhiteListFileDecorator : IFileUpdateHandler
{
    private readonly IFileUpdateHandler _handler;

    public WhiteListFileDecorator(IFileUpdateHandler handler)
    {
        _handler = handler;
    }

    public async Task<FileValidationResult> ValidateFilesAsync(ProfileReadInfoDto profileInfo, string rootDirectory)
    {
        var result = await _handler.ValidateFilesAsync(profileInfo, rootDirectory);

        result.FilesToDelete = result.FilesToDelete.Where(file => !ExistsInWhiteList(profileInfo, file));
        result.FilesToUpdate = result.FilesToUpdate.Where(file => ShouldKeepForUpdate(profileInfo, file, rootDirectory));

        return result;
    }

    private static bool ExistsInWhiteList(ProfileReadInfoDto profileInfo, ProfileFileReadDto file)
    {
        return profileInfo.WhiteListFiles.Any(w =>
            SystemIoProcedures.NormalizePath(w.Directory).Equals(
                SystemIoProcedures.NormalizePath(file.Directory),
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldKeepForUpdate(ProfileReadInfoDto profileInfo, ProfileFileReadDto file, string rootDirectory)
    {
        var fullPath = Path.Combine(rootDirectory, SystemIoProcedures.NormalizePath(file.Directory));
        var exists = File.Exists(fullPath) || File.Exists(fullPath + ".disabled");

        // Missing locally -> download.
        if (!exists) return true;

        // Exists and whitelisted -> never touch.
        // Exists and not whitelisted -> keep whatever the upstream handlers already decided (re-download).
        return !ExistsInWhiteList(profileInfo, file);
    }
}
