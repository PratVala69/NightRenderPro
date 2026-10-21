using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UserDatabase
{
    private static string filePath => Path.Combine(Application.persistentDataPath, "users.json");

    public static List<UserData> LoadUsers()
    {
        if (!File.Exists(filePath))
            return new List<UserData>();

        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<UserListWrapper>(json).users;
    }

    public static void SaveUsers(List<UserData> users)
    {
        UserListWrapper wrapper = new UserListWrapper { users = users };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(filePath, json);
    }

    [System.Serializable]
    private class UserListWrapper
    {
        public List<UserData> users;
    }
}

[System.Serializable]
public class UserData
{
    public string firstName;
    public string lastName;
    public string companyName;
    public string mobile;
    public string email;
    public string password;
}