using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace Gml.Client.Helpers.Files;

/// <summary>
/// Persists SHA1 hashes keyed by (relative path, size, last-write time) so unchanged files
/// don't need to be re-read and re-hashed on every launch.
/// </summary>
internal sealed class FileHashCache
{
    private class Entry
    {
        public long Size { get; set; }
        public long LastWriteTimeTicks { get; set; }
        public string Hash { get; set; } = string.Empty;
    }

    private readonly string _cachePath;
    private readonly ConcurrentDictionary<string, Entry> _entries;
    private volatile bool _isDirty;

    private FileHashCache(string cachePath, ConcurrentDictionary<string, Entry> entries)
    {
        _cachePath = cachePath;
        _entries = entries;
    }

    public static FileHashCache Load(string rootDirectory)
    {
        var cachePath = Path.Combine(rootDirectory, "shared data", "file_hash_cache.json");

        try
        {
            if (File.Exists(cachePath))
            {
                var json = File.ReadAllText(cachePath);
                var entries = JsonSerializer.Deserialize<ConcurrentDictionary<string, Entry>>(json);

                if (entries is not null)
                    return new FileHashCache(cachePath, entries);
            }
        }
        catch
        {
            // Corrupted or unreadable cache - fall back to an empty one and let it rebuild.
        }

        return new FileHashCache(cachePath, new ConcurrentDictionary<string, Entry>());
    }

    public bool TryGetHash(string relativePath, long size, DateTime lastWriteTimeUtc, out string hash)
    {
        if (_entries.TryGetValue(relativePath, out var entry) &&
            entry.Size == size &&
            entry.LastWriteTimeTicks == lastWriteTimeUtc.Ticks)
        {
            hash = entry.Hash;
            return true;
        }

        hash = string.Empty;
        return false;
    }

    public void SetHash(string relativePath, long size, DateTime lastWriteTimeUtc, string hash)
    {
        _entries[relativePath] = new Entry
        {
            Size = size,
            LastWriteTimeTicks = lastWriteTimeUtc.Ticks,
            Hash = hash
        };
        _isDirty = true;
    }

    public void Save()
    {
        if (!_isDirty) return;

        try
        {
            var directory = Path.GetDirectoryName(_cachePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_cachePath, JsonSerializer.Serialize(_entries));
        }
        catch
        {
            // Best-effort cache - a failed write just means the next launch re-hashes everything.
        }
    }
}
