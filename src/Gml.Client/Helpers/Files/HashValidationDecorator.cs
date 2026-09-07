using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Gml.Dto.Files;
using Gml.Dto.Profile;

namespace Gml.Client.Helpers.Files;

public class HashValidationDecorator : IFileUpdateHandler
{
    private readonly IFileUpdateHandler _handler;

    public HashValidationDecorator(IFileUpdateHandler handler)
    {
        _handler = handler;
    }

    public async Task<FileValidationResult> ValidateFilesAsync(ProfileReadInfoDto profileInfo, string rootDirectory)
    {
        var result = await _handler.ValidateFilesAsync(profileInfo, rootDirectory);
        var filesToUpdate = new ConcurrentBag<ProfileFileReadDto>();
        var filesToDelete = new ConcurrentDictionary<string, ProfileFileReadDto>(
            result.FilesToDelete.ToDictionary(
                f => SystemIoProcedures.NormalizePath(f.Directory),
                f => f
            )
        );
        var files = result.FilesToUpdate.ToList();
        var hashCache = FileHashCache.Load(rootDirectory);

        // Runs on a pool thread: the body below is fully synchronous (blocking file I/O + SHA1),
        // so Parallel.ForEach gives real multi-core parallelism instead of the single-threaded
        // execution you'd get from `Task.WhenAll` over async lambdas with no actual awaits inside.
        await Task.Run(() => Parallel.ForEach(files, serverFile =>
        {
            filesToDelete.TryGetValue(SystemIoProcedures.NormalizePath(serverFile.Directory), out var localFile);

            if (localFile is null)
            {
                var localPath = Path.Combine(rootDirectory, SystemIoProcedures.NormalizePath(serverFile.Directory));

                if (File.Exists(localPath))
                {
                    var fileInfo = new FileInfo(localPath);
                    // Сначала проверяем размер
                    if (fileInfo.Length == serverFile.Size)
                    {
                        // Размеры совпадают - проверяем хеш
                        localFile = new ProfileFileReadDto
                        {
                            Name = serverFile.Name,
                            Directory = serverFile.Directory,
                            Size = fileInfo.Length,
                        };
                    }
                    else
                    {
                        // Размеры не совпадают - сразу добавляем в список на обновление
                        filesToUpdate.Add(serverFile);
                        return;
                    }
                }
            }

            if (localFile == null)
            {
                // Файла нет локально - нужно скачать
                filesToUpdate.Add(serverFile);
            }
            else if (localFile.Size == serverFile.Size)
            {
                // Размеры совпадают - проверяем хеш
                var normalizedDirectory = SystemIoProcedures.NormalizePath(serverFile.Directory);
                var localPath = Path.Combine(rootDirectory, normalizedDirectory);
                var skipHashCheck = localPath.StartsWith(Path.Combine(rootDirectory, "assets"));

                var hashMatches = true;

                if (!skipHashCheck)
                {
                    var fileInfo = new FileInfo(localPath);

                    if (!hashCache.TryGetHash(normalizedDirectory, fileInfo.Length, fileInfo.LastWriteTimeUtc,
                            out var hash))
                    {
                        using var algorithm = SHA1.Create();
                        hash = SystemHelper.CalculateFileHash(localPath, algorithm);
                        hashCache.SetHash(normalizedDirectory, fileInfo.Length, fileInfo.LastWriteTimeUtc, hash);
                    }

                    hashMatches = hash == serverFile.Hash;
                }

                if (!hashMatches)
                {
                    filesToUpdate.Add(serverFile);
                    filesToDelete.TryRemove(SystemIoProcedures.NormalizePath(localFile.Directory), out _);
                }
                else
                {
                    // Файл актуален - исключаем из списка на удаление
                    filesToDelete.TryRemove(SystemIoProcedures.NormalizePath(localFile.Directory), out _);
                }
            }
            else
            {
                // Размеры не совпадают - нужно обновить
                filesToUpdate.Add(serverFile);
                filesToDelete.TryRemove(SystemIoProcedures.NormalizePath(localFile.Directory), out _);
            }
        }));

        hashCache.Save();

        result.FilesToUpdate = filesToUpdate;
        result.FilesToDelete = filesToDelete.Values;
        return result;
    }
}
