using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public TMP_InputField playerNameInput;
    public Transform playersListContainer; // Контейнер для списка игроков
    public GameObject playerEntryPrefab; // Префаб игрока (имя + кнопка)
    public GameObject invitePopup; // Окно приглашения
    public TMP_Text inviteText;
    public Button acceptInviteButton, declineInviteButton;

    private const byte INVITE_EVENT = 1;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        PhotonNetwork.ConnectUsingSettings();
        playerNameInput.onValueChanged.AddListener(delegate { OnNameChanged(); });

        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("GlobalRoom", new RoomOptions { MaxPlayers = 20 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        SetPlayerName();
        UpdateRoomPlayers();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomPlayers();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomPlayers();
    }

    public void OnNameChanged()
    {
        if (!PhotonNetwork.IsConnected || string.IsNullOrEmpty(playerNameInput.text))
            return;

        PhotonNetwork.NickName = playerNameInput.text;
        SetPlayerName();
        UpdateRoomPlayers();
    }

    void SetPlayerName()
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["PlayerName"] = PhotonNetwork.NickName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    void UpdateRoomPlayers()
    {

        foreach (Transform child in playersListContainer)
            Destroy(child.gameObject);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerName = player.CustomProperties.ContainsKey("PlayerName") ? (string)player.CustomProperties["PlayerName"] : "Unknown";

            GameObject entry = Instantiate(playerEntryPrefab, playersListContainer);
            entry.transform.Find("PlayerNameText").GetComponent<TMP_Text>().text = playerName;
            Button inviteButton = entry.transform.Find("InviteButton").GetComponent<Button>();

            if (player != PhotonNetwork.LocalPlayer)
            {
                inviteButton.onClick.AddListener(() => SendInvite(player));
            }
            else
            {
                inviteButton.gameObject.SetActive(false); // Скрываем кнопку у себя
            }
        }
    }

    void SendInvite(Player targetPlayer)
    {
        object[] content = new object[] { PhotonNetwork.NickName, targetPlayer.ActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetPlayer.ActorNumber } };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(INVITE_EVENT, content, options, sendOptions);
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
    }

    void ShowInvitePopup(string inviterName, int inviterId)
    {
        inviteText.text = $"{inviterName} приглашает вас в матч!";
        invitePopup.SetActive(true);

        acceptInviteButton.onClick.RemoveAllListeners();
        acceptInviteButton.onClick.AddListener(() => AcceptInvite(inviterId));

        declineInviteButton.onClick.RemoveAllListeners();
        declineInviteButton.onClick.AddListener(() => invitePopup.SetActive(false));
    }

    void AcceptInvite(int inviterId)
    {
        invitePopup.SetActive(false);
        string matchRoom = $"Match_{inviterId}_{PhotonNetwork.LocalPlayer.ActorNumber}";
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.JoinOrCreateRoom(matchRoom, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}