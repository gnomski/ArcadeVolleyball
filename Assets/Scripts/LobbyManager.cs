using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("Игровые элементы")]
    [SerializeField] private Transform playersListContainer;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button changeNameButton;
    [SerializeField] private TMP_Text currentRoomText;

    [Header("Попап смены имени")]
    [SerializeField] private GameObject nameChangePopup;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button submitNameButton;
    [SerializeField] private Button closePopupButton;

    [Header("Попап приглашения")]
    [SerializeField] private GameObject invitePopup;
    [SerializeField] private TMP_Text inviteText;
    [SerializeField] private Button acceptInviteButton;
    [SerializeField] private Button declineInviteButton;

    [Header("Попап отказа")]
    [SerializeField] private GameObject declinePopup;
    [SerializeField] private TMP_Text declineText;
    [SerializeField] private Button okButton;

    private const byte INVITE_EVENT = 1;
    private const byte DECLINE_EVENT = 2;
    private const byte MOVE_TO_NEW_ROOM_EVENT = 3;

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    private int inviterId;

    private bool isSwitchingRoom = false;
    private string pendingRoomName;

    /// <summary>
    /// Инициализация лобби, подключение к Photon и настройка UI.
    /// </summary>
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        // Загружаем имя игрока из сохранений
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            PhotonNetwork.NickName = savedName;
        }
        else
        {
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);
        }

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        UpdatePlayerNameUI();
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AddCallbackTarget(this);

        invitePopup.SetActive(false);
        declinePopup.SetActive(false);
        nameChangePopup.SetActive(false);
        submitNameButton.interactable = false;

        okButton.onClick.AddListener(HideDeclinePopup);
        changeNameButton.onClick.AddListener(OpenNameChangePopup);
        submitNameButton.onClick.AddListener(ChangePlayerName);
        closePopupButton.onClick.AddListener(CloseNameChangePopup);
        nameInputField.onValueChanged.AddListener(delegate { UpdateSubmitButtonState(); });

        declineInviteButton.onClick.AddListener(() => DeclineInvite(inviterId));
        acceptInviteButton.onClick.AddListener(() => AcceptInvite(inviterId));

        UpdateRoomNameUI();
    }

    /// <summary>
    /// Обновляет название текущей комнаты в UI.
    /// </summary>
    private void UpdateRoomNameUI()
    {
        if (currentRoomText != null)
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                currentRoomText.text = $"Комната: {PhotonNetwork.CurrentRoom.Name}";
            }
            else
            {
                currentRoomText.text = ""; // Очистка текста, если комната не задана
            }
        }
    }

    private void OpenNameChangePopup()
    {
        nameInputField.text = PhotonNetwork.NickName;
        nameChangePopup.SetActive(true);
        UpdateSubmitButtonState();
    }

    private void CloseNameChangePopup()
    {
        nameChangePopup.SetActive(false);
    }

    private void UpdateSubmitButtonState()
    {
        submitNameButton.interactable = !string.IsNullOrEmpty(nameInputField.text);
    }

    private void ChangePlayerName()
    {
        string newName = nameInputField.text;
        PhotonNetwork.NickName = newName;

        PlayerPrefs.SetString("PlayerName", newName);
        PlayerPrefs.Save();

        SetPlayerName();
        UpdatePlayerNameUI();
        CloseNameChangePopup();
    }

    private void UpdatePlayerNameUI()
    {
        playerNameText.text = PhotonNetwork.NickName;
    }

    private void SetPlayerName()
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["PlayerName"] = PhotonNetwork.NickName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        cachedRoomList = roomList;
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Вышли из комнаты, заходим в лобби.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        if (isSwitchingRoom && !string.IsNullOrEmpty(pendingRoomName))
        {
            PhotonNetwork.JoinOrCreateRoom(pendingRoomName, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
            isSwitchingRoom = false;
            pendingRoomName = null;
        }
        else
        {
            PhotonNetwork.JoinOrCreateRoom("GlobalRoom", new RoomOptions { MaxPlayers = 20 }, TypedLobby.Default);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("PlayerName"))
        {
            UpdateRoomPlayers();
        }
    }

    public override void OnJoinedRoom()
    {
        SetPlayerName();
        UpdateRoomPlayers();
        UpdateRoomNameUI();
        Debug.Log($"Вошли в комнату: {PhotonNetwork.CurrentRoom.Name}");

        if (PhotonNetwork.CurrentRoom.Name.StartsWith("Match_") && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomPlayers();
        if (PhotonNetwork.CurrentRoom.Name.StartsWith("Match_") && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomPlayers();
    }

    private void UpdateRoomPlayers()
    {
        foreach (Transform child in playersListContainer)
            Destroy(child.gameObject);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerName = player.CustomProperties.ContainsKey("PlayerName")
                ? (string)player.CustomProperties["PlayerName"]
                : "Unknown";

            GameObject entry = Instantiate(playerEntryPrefab, playersListContainer);
            entry.transform.Find("PlayerNameText").GetComponent<TMP_Text>().text = playerName;
            Button inviteButton = entry.transform.Find("InviteButton").GetComponent<Button>();

            if (player != PhotonNetwork.LocalPlayer)
            {
                inviteButton.onClick.AddListener(() => SendInvite(player));
            }
            else
            {
                inviteButton.interactable = false;
            }
        }
    }

    private void SendInvite(Player targetPlayer)
    {
        object[] content = new object[] { PhotonNetwork.NickName, PhotonNetwork.LocalPlayer.ActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetPlayer.ActorNumber } };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(INVITE_EVENT, content, options, sendOptions);
        inviterId = targetPlayer.ActorNumber; // Сохраняем ID приглашённого
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == INVITE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            string inviterName = (string)data[0];
            int inviterId = (int)data[1];

            ShowInvitePopup(inviterName, inviterId);
        }
        else if (photonEvent.Code == DECLINE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            string playerName = (string)data[0];

            ShowDeclinePopup(playerName);
        }
        else if (photonEvent.Code == MOVE_TO_NEW_ROOM_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            string matchRoom = (string)data[0];

            Debug.Log($"Получена команда войти в {matchRoom}");

            pendingRoomName = matchRoom;
            isSwitchingRoom = true;

            PhotonNetwork.LeaveRoom();
        }
    }

    private void ShowInvitePopup(string inviterName, int inviterId)
    {
        inviteText.text = $"{inviterName} приглашает вас в матч!";
        invitePopup.SetActive(true);

        acceptInviteButton.onClick.RemoveAllListeners();
        declineInviteButton.onClick.RemoveAllListeners();

        declineInviteButton.onClick.AddListener(() => DeclineInvite(inviterId));
        acceptInviteButton.onClick.AddListener(() => AcceptInvite(inviterId));
    }

    /// <summary>
    /// Отклоняет приглашение и отправляет отказ пригласившему игроку.
    /// </summary>
    private void DeclineInvite(int inviterId)
    {
        invitePopup.SetActive(false);

        object[] content = new object[] { PhotonNetwork.NickName };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { inviterId } };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(DECLINE_EVENT, content, options, sendOptions);
    }

    private void ShowDeclinePopup(string playerName)
    {
        declineText.text = $"{playerName} отклонил ваше приглашение";
        declinePopup.SetActive(true);
    }

    private void HideDeclinePopup()
    {
        declinePopup.SetActive(false);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    /// <summary>
    /// Принимает приглашение и запрашивает номер новой комнаты.
    /// </summary>
    private void AcceptInvite(int inviterId)
    {
        invitePopup.SetActive(false);
        this.inviterId = inviterId;

        StartCoroutine(FindNextMatchRoom());
    }

    /// <summary>
    /// Запрашивает номер первой свободной комнаты `Match_X`.
    /// </summary>
    private IEnumerator FindNextMatchRoom()
    {
        Debug.Log("Начали поиск комнаты...");

        yield return new WaitForSeconds(1f); // ждем немного, чтобы список комнат успел прийти от Photon

        int matchNumber = 1;
        string matchRoom;

        while (true)
        {
            matchRoom = $"Match_{matchNumber}";

            if (cachedRoomList.Any(room => room.Name == matchRoom))
                matchNumber++;
            else
                break;
        }

        MovePlayersToRoom(matchRoom); ;
    }

    /// <summary>
    /// Переносит обоих игроков в новую комнату.
    /// </summary>
    private void MovePlayersToRoom(string matchRoom)
    {
        Debug.Log($"Создаём новую комнату: {matchRoom}");

        object[] content = new object[] { matchRoom };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { inviterId } };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(MOVE_TO_NEW_ROOM_EVENT, content, options, sendOptions);

        isSwitchingRoom = true;
        pendingRoomName = matchRoom;

        PhotonNetwork.LeaveRoom();
    }

    /// <summary>
    /// Ждём выхода из комнаты и заходим в новую комнату.
    /// </summary>
    private IEnumerator JoinNewRoomWhenReady(string matchRoom)
    {
        while (PhotonNetwork.InRoom)
        {
            yield return null;
        }

        PhotonNetwork.JoinOrCreateRoom(matchRoom, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }
}