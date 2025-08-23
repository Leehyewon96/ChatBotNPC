using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcController : MonoBehaviour
{
    enum ChatType
    {
        None = 0,
        User=1,
        AI=2,
    }

    [SerializeField] OpenAiApiClient apiClient; // 인스펙터 창에서 OpenAiApiClient가 있는 오브젝트를 연결
    [SerializeField] TextMeshProUGUI chatText; //채팅 텍스트 오브젝트 원본
    [SerializeField] Button sendButton;
    [SerializeField] TMP_InputField userInput;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Transform content;

    private void Start()
    {
        BindUI();
    }

    void BindUI()
    {
        sendButton.onClick.AddListener(() =>
            {
                if (userInput == null || string.IsNullOrEmpty(userInput.text))
                    return;

                SendQuestion(userInput.text);
            });
    }

    void SendQuestion(string question)
    {
        var userChat = AddChat(question, ChatType.User);
        StartCoroutine(RefreshScrollRect(userChat));

        OnPlayerAskQuestion(question);
        userInput.text = string.Empty;
    }


    public async void OnPlayerAskQuestion(string question)
    {
        sendButton.interactable = false;
        string playerQuestion = question;
        string loadingText = "생각 중..."; // 로딩 표시
        var newChat = AddChat(loadingText, ChatType.AI);
        
        // 비동기로 API 호출 후 결과 받아오기
        string response = await apiClient.SendMessageToOpenAI(playerQuestion);

        // UI에 최종 답변 표시
        newChat.gameObject.SetActive(false);
        newChat.SetText(response);
        sendButton.interactable = true;
        StartCoroutine(RefreshScrollRect(newChat));

        //스트림 출력 추가 필요
    }

    IEnumerator RefreshScrollRect(TextMeshProUGUI newChat)
    {
        yield return new WaitForEndOfFrame();
        newChat.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    TextMeshProUGUI AddChat(string chat, ChatType chatType)
    {
        var newChat = Instantiate(chatText, content);
        newChat.SetText(chat);

        switch (chatType)
        {
            case ChatType.User:
                newChat.alignment = TextAlignmentOptions.MidlineRight;
                break;
            case ChatType.AI:
                newChat.alignment = TextAlignmentOptions.MidlineLeft;
                break;
            default:
                break;
        }

        return newChat;
    }
}