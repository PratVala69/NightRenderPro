using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MeshDetailLocker : MonoBehaviour
{
    public ARMeshManager meshManager;

    //void OnEnable() => meshManager.meshesChanged += OnMeshesChanged;
    //void OnDisable() => meshManager.meshesChanged -= OnMeshesChanged;

    private void Start()
    {
        meshManager.density = 1f;
    }

    private void Update()
    {
        Debug.Log(meshManager.density);
    }

    void OnMeshesChanged(ARMeshesChangedEventArgs args)
    {
        // Коли система додає новий шматок мешу
        foreach (var meshFilter in args.added)
        {
            // Можна додати логіку: якщо ми близько до об'єкта, 
            // ми його "заморожуємо"
        }

        // КРИТИЧНИЙ МОМЕНТ: Ігноруємо оновлення (updated)
        foreach (var meshFilter in args.updated)
        {
            // Якщо ви хочете, щоб меш НІКОЛИ не ставав гіршим:
            // Ми просто вимикаємо оновлення для тих об'єктів, які вже існують
            // Або просто нічого тут не робимо, не даючи менеджеру замінити геометрію
        }
    }
}
