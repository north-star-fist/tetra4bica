namespace Sergei.Safonov.Persistence {

    /// <summary>
    /// Inteface for saving and loading stuff.
    /// </summary>
    public interface ISaveManager {
        public LoadResult<T> Load<T>(string key);

        public void Save<T>(string key, T value);

        public struct LoadResult<T> {
            public static LoadResult<T> Fail => default;

            public bool IsSuccessful;
            public T Value;

            public LoadResult(T value) {
                IsSuccessful = true;
                Value = value;
            }
        }
    }
}
