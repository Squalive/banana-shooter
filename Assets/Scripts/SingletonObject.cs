
using UnityEngine;

public class SingletonObject<T> :MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get => _instance;
        protected set => _instance = value;
    }
}
