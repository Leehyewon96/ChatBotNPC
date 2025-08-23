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

    [SerializeField] OpenAiApiClient apiClient; // �ν����� â���� OpenAiApiClient�� �ִ� ������Ʈ�� ����
    [SerializeField] TextMeshProUGUI chatText; //ä�� �ؽ�Ʈ ������Ʈ ����
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
        string loadingText = "���� ��..."; // �ε� ǥ��
        var newChat = AddChat(loadingText, ChatType.AI);
        
        // �񵿱�� API ȣ�� �� ��� �޾ƿ���
        string response = await apiClient.SendMessageToOpenAI(playerQuestion);

        // UI�� ���� �亯 ǥ��
        newChat.gameObject.SetActive(false);
        newChat.SetText(response);
        sendButton.interactable = true;
        StartCoroutine(RefreshScrollRect(newChat));

        //��Ʈ�� ��� �߰� �ʿ�
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