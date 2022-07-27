namespace BasicReverseProxy.Cache.Settings
{
    public enum CacheActionType
    {
        Store,
        Expire
    }

    public class CacheSettings
    {
        public bool Enable { get; set; } = false;
        public int? Expiration { get; set; } = 1800;
        public CacheActionType Action { get; set; } = CacheActionType.Store;
    }
}
