using UnityEngine;

namespace HyperEdit
{
    public static class Immortal
    {
        private static GameObject _gameObject;
        public static T AddImmortal<T>() where T : Component
        {
            if (_gameObject == null)
            {
                _gameObject = new GameObject("KhylibImmortal", typeof(T));
                Object.DontDestroyOnLoad(_gameObject);
            }
            return _gameObject.GetComponent<T>() ?? _gameObject.AddComponent<T>();
        }
    }
}
