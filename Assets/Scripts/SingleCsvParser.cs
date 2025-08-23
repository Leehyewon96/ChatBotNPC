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

        //itemDataDic으로 문장 생성하여 Json으로 만들기
        KnowledgeJsonGenerator.GenerateKnowledgeJson(itemDataDic);
    }

    public void ParseCsv(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[CsvParser] Error: 파일을 찾을 수 없습니다. 경로: {filePath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                Debug.LogWarning("[CsvParser] Warning: 파일에 헤더와 데이터가 충분하지 않습니다.");
                return;
            }

            string[] headers = lines[0].Split(',').Select(header => header.Trim()).ToArray();
            
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',').Select(value => value.Trim()).ToArray();
                if (values.Length != headers.Length)
                {
                    Debug.LogWarning($"[CsvParser] Warning: {i + 1}번째 줄의 컬럼 수가 헤더와 다릅니다.");
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
            Debug.LogError($"[CsvParser] Error: 파일을 처리하는 중 오류가 발생했습니다. {e.Message}");
        }
    }
}

public static class KnowledgeJsonGenerator
{
    // 직렬화할 데이터 클래스
    [Serializable]
    public class KnowledgeEntry
    {
        public int itemId;
        public string sentence;
    }

    [Serializable]
    public class KnowledgeData
    {
        // JsonUtility는 최상위 레벨이 배열인 JSON을 직접 변환하지 못하므로, 리스트를 감싸는 클래스 생성
        public List<KnowledgeEntry> knowledgeEntries = new List<KnowledgeEntry>();
    }


    /// <summary>
    /// CSV 데이터(Dictionary 리스트)를 받아 최종 지식 JSON 문자열을 생성
    /// </summary>
    /// <param name="csvData">CSV 파서로 읽어온 데이터</param>
    /// <returns>JSON 형식의 문자열</returns>
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


        // JSON 생성
        string jsonResult = JsonUtility.ToJson(knowledgeData, true);

        // 파일로 저장
        string path = Path.Combine(Application.dataPath, "Resources/KnowledgeBase", "knowledge_base.json");
        path = path.Replace("\\", "/");
        File.WriteAllText(path, jsonResult);
    }

    /// <summary>
    /// Dictionary 데이터 한 줄을 받아 자연어 문장 생성
    /// </summary>
    private static string CreateSentenceFromData(ItemData itemData)
    {
        int itemId = itemData.itemID;
        string itemName = itemData.itemName;
        string itemType = itemData.itemType != 0 ? $"{itemData.itemType}" : "알 수 없는";

        string baseDescription = $"{itemName}은(는) {itemType} 타입의 아이템입니다.";

        var materialInfo = $"{itemData.materialName} 아이템 {itemData.materialCount}개";
        

        if (!string.IsNullOrEmpty(itemData.materialName))
        {
            return baseDescription + "\n" + " 제작하기 위해서는 " + materialInfo + "가 필요합니다.";
        }

        return baseDescription;
    }
}