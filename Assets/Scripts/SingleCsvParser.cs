using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public readonly struct ItemData
{
    public readonly int itemID;
    public readonly string itemName;
    public readonly int itemType;
    public readonly string materialName;
    public readonly int materialCount;
    public readonly int level;
    public readonly string reinforceName;
    public readonly int reinforceCount;

    public ItemData(int itemID, string itemName, int itemType, string materialName, int materialCount, int level, string reinforceName, int reinforceCount)
    {
        this.itemID = itemID;
        this.itemName = itemName;
        this.itemType = itemType;
        this.materialName = materialName;
        this.materialCount = materialCount;
        this.level = level;
        this.reinforceName = reinforceName;
        this.reinforceCount = reinforceCount;
    }
}

public class SingleCsvParser : MonoBehaviour
{
    Dictionary<int, ItemData> itemDataDic = new Dictionary<int, ItemData>();

    void Awake()
    {
        string path = Path.Combine(Application.dataPath, "Resources/DataTable", "Item_Data.csv");
        path = path.Replace("\\", "/");
        ParseCsv(path);

        //itemDataDic���� ���� �����Ͽ� Json���� �����
        KnowledgeJsonGenerator.GenerateKnowledgeJson(itemDataDic);
    }

    public void ParseCsv(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[CsvParser] Error: ������ ã�� �� �����ϴ�. ���: {filePath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                Debug.LogWarning("[CsvParser] Warning: ���Ͽ� ����� �����Ͱ� ������� �ʽ��ϴ�.");
                return;
            }

            string[] headers = lines[0].Split(',').Select(header => header.Trim()).ToArray();
            
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',').Select(value => value.Trim()).ToArray();
                if (values.Length != headers.Length)
                {
                    Debug.LogWarning($"[CsvParser] Warning: {i + 1}��° ���� �÷� ���� ����� �ٸ��ϴ�.");
                    continue;
                }

                var itemId = Convert.ToInt32(values[0]);
                var itemData = new ItemData
                   (
                        itemId,
                        Convert.ToString(values[1]),
                        Convert.ToInt32(values[2]),
                        Convert.ToString(values[3]),
                        Convert.ToInt32(values[4]),
                        Convert.ToInt32(values[5]),
                        Convert.ToString(values[6]),
                        Convert.ToInt32(values[7])
                   );

                if(!itemDataDic.ContainsKey(itemId))
                {
                    itemDataDic.Add(itemId, itemData);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CsvParser] Error: ������ ó���ϴ� �� ������ �߻��߽��ϴ�. {e.Message}");
        }
    }
}

public static class KnowledgeJsonGenerator
{
    // ����ȭ�� ������ Ŭ����
    [Serializable]
    public class KnowledgeEntry
    {
        public int itemId;
        public string sentence;
    }

    [Serializable]
    public class KnowledgeData
    {
        // JsonUtility�� �ֻ��� ������ �迭�� JSON�� ���� ��ȯ���� ���ϹǷ�, ����Ʈ�� ���δ� Ŭ���� ����
        public List<KnowledgeEntry> knowledgeEntries = new List<KnowledgeEntry>();
    }


    /// <summary>
    /// CSV ������(Dictionary ����Ʈ)�� �޾� ���� ���� JSON ���ڿ��� ����
    /// </summary>
    /// <param name="csvData">CSV �ļ��� �о�� ������</param>
    /// <returns>JSON ������ ���ڿ�</returns>
    public static void GenerateKnowledgeJson(Dictionary<int, ItemData> csvData)
    {
        KnowledgeData knowledgeData = new KnowledgeData();

        foreach (var itemData in csvData)
        {
            string generatedSentence = CreateSentenceFromData(itemData.Value);
            if (string.IsNullOrEmpty(generatedSentence)) continue;

            KnowledgeEntry entry = new KnowledgeEntry
            {
                itemId = itemData.Value.itemID,
                sentence = generatedSentence
            };
            knowledgeData.knowledgeEntries.Add(entry);
        }


        // JSON ����
        string jsonResult = JsonUtility.ToJson(knowledgeData, true);

        // ���Ϸ� ����
        string path = Path.Combine(Application.dataPath, "Resources/KnowledgeBase", "knowledge_base.json");
        path = path.Replace("\\", "/");
        File.WriteAllText(path, jsonResult);
    }

    /// <summary>
    /// Dictionary ������ �� ���� �޾� �ڿ��� ���� ����
    /// </summary>
    private static string CreateSentenceFromData(ItemData itemData)
    {
        int itemId = itemData.itemID;
        string itemName = itemData.itemName;
        string itemType = itemData.itemType != 0 ? $"{itemData.itemType}" : "�� �� ����";

        string baseDescription = $"{itemName}��(��) {itemType} Ÿ���� �������Դϴ�.";

        var materialInfo = $"{itemData.materialName} ������ {itemData.materialCount}��";
        

        if (!string.IsNullOrEmpty(itemData.materialName))
        {
            return baseDescription + "\n" + " �����ϱ� ���ؼ��� " + materialInfo + "�� �ʿ��մϴ�.";
        }

        return baseDescription;
    }
}