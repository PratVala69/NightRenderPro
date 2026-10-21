using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LocalClientDatabase
{
    private static string filePath = Application.persistentDataPath + "/clients.json";

    public static List<AddClientData> LoadClients()
    {
        if (!File.Exists(filePath))
            return new List<AddClientData>();

        string json = File.ReadAllText(filePath);
        ClientList clientList = JsonUtility.FromJson<ClientList>(json);
        return clientList.clients;
    }

    public static void SaveClients(List<AddClientData> clients)
    {
        ClientList clientList = new ClientList { clients = clients };
        string json = JsonUtility.ToJson(clientList, true);
        File.WriteAllText(filePath, json);
    }
}

[System.Serializable]
public class AddClientData
{
    public string firstName;
    public string mobile;
    public string companyName;
    public string address;
    public string notes;
    public string createdAt;
}

[System.Serializable]
public class ClientList
{
    public List<AddClientData> clients = new List<AddClientData>();
}