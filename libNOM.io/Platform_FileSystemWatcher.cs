using CommunityToolkit.Diagnostics;

using LazyCache;

using libNOM.io.Interfaces;

using Microsoft.Extensions.Caching.Memory;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region FileSystemWatcher

    /// <summary>
    /// Enables the <see cref="FileSystemWatcher"/> if settings allowing it.
    /// </summary>
    protected void EnableWatcher()
    {
        _watcher.EnableRaisingEvents = Settings.Watcher;
    }

    /// <summary>
    /// Disables the <see cref="FileSystemWatcher"/>.
    /// </summary>
    protected void DisableWatcher()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Gets called on a watcher event and adds the new change type to the cache.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    protected void OnWatcherEvent(object source, FileSystemEventArgs e)
    {
        // Workaround to update the value and keep the immediate eviction.
        if (_cache.TryGetValue(e.Name, out Lazy<WatcherChangeTypes> lazyType))
        {
            _cache.Remove(e.Name);
            _cache.GetOrAdd(e.Name, () => (lazyType.Value | e.ChangeType), _options);
        }
        else
            _cache.GetOrAdd(e.Name, () => (e.ChangeType), _options);
    }

    /// <summary>
    /// Gets called when something gets evicted from cache.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="reason"></param>
    /// <param name="state"></param>
    protected virtual void OnCacheEviction(object key, object value, EvictionReason reason, object state)
    {
        /** Vanilla WatcherChangeTypes

        Created by game or an editor:
         * save.hg (Created)
         * mf_save.hg (Created)
         * save.hg (Changed)
         * mf_save.hg (Changed)

        Changed by game or an editor:
         * save.hg (Changed)
         * mf_save.hg (Changed)
         * save.hg (Changed)
         * mf_save.hg (Changed)

        Deleted by game or an editor:
         * save.hg (Deleted)
         * mf_save.hg (Deleted)
         */

        /** Save Streaming WatcherChangeTypes

        Created by game:
         * save.hg.stream (Created)
         * save.hg.stream (Changed)
         * mf_save.hg (Created)
         * mf_save.hg (Changed)
         * save.hg (Renamed)

        Changed by game:
         * save.hg.stream (Created)
         * save.hg.stream (Changed)
         * mf_save.hg (Changed)
         * mf_save.hg (Changed)
         * save.hg (Deleted)
         * save.hg (Renamed)

        Deleted by game:
         * save.hg (Deleted)
         * mf_save.hg (Deleted)

        All changes by an editor:
         * save.hg (Changed)
         * mf_save.hg (Changed)
         */

        if (reason is not EvictionReason.Expired and not EvictionReason.TokenExpired)
            return;

        // Choose what actually happened based on the combined change types combinations listed at the beginning of this method.
        var changeType = (WatcherChangeTypes)(value) switch
        {
            WatcherChangeTypes.Renamed => WatcherChangeTypes.Created, // Save Streaming
            WatcherChangeTypes.Deleted | WatcherChangeTypes.Renamed => WatcherChangeTypes.Changed, // Save Streaming
            WatcherChangeTypes.Created | WatcherChangeTypes.Changed => WatcherChangeTypes.Created, // Vanilla
            _ => (WatcherChangeTypes)(value),
        };
        foreach (var container in GetCacheEvictionContainers((string)(key)))
        {
            container.SetWatcherChange(changeType);
            if (container.IsSynced)
                OnWatcherDecision(container, true);
        }
    }

    public void OnWatcherDecision(Container container, bool execute)
    {
        Guard.IsNotNull(container);

        if (execute)
        {
            Reload(container);

            // Only when executed to keep old timestamps.
            container.RefreshFileInfo();
        }
        else
            container.IsSynced = false;

        container.ResolveWatcherChange();

        // Invoke as it was written but from the outside.
        container.WriteCallback.Invoke();
    }

    #endregion
}
