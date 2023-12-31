﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Managing.Server;

public enum ClientType : byte {
    None   = 0,
    Host   = 1,
    Client = 2,
}

public abstract class FishNetLobby<T> : ALobby where T : Transport {
    public LocalConnectionState ServerState { get; protected set; } = LocalConnectionState.Stopped;
    public LocalConnectionState ClientState { get; protected set; } = LocalConnectionState.Stopped;

    // ====================== Variables ======================
    private NetworkManager networkManager;
    private Multipass multipass;
    private Transport serverTransport;
    protected ClientType clientType;

    // ==================== Configuration ====================
    /// <summary>
    /// Configure the NetworkManager's Transports.
    /// A propper implementation must:
    /// <list type="bullet">
    /// <item>Set the <see cref="serverTransport"/> reference.</item>
    /// <item>Use <see cref="Multipass.SetClientTransport()"/> to set multipass's client transport.</item> 
    /// </list>
    /// </summary>
    protected virtual void ConfigTransports() {
        serverTransport = multipass.GetTransport<T>();
        multipass.SetClientTransport<T>();
    }

    // ===================== Custom Code =====================
    protected virtual void StartServer() {
        networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        serverTransport.StartConnection(true);
    }
    protected virtual void StopServer() {
        serverTransport.StopConnection(true);
        networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    }

    protected virtual void ConnectClient() {
        networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        networkManager.ClientManager.StartConnection();
    }
    protected virtual void DisconnectClient() {
        networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        networkManager.ClientManager.StopConnection();
    }

    private void OnClientConnectionState(ClientConnectionStateArgs obj) {
        ClientState = obj.ConnectionState;
        //Debug.Log($"C: {ClientState}");

        // Exit the lobby
        if (LocalConnectionState.Stopped == ClientState) {
            GameManager.ExitToTittleScreen();
        }
    }
    private void OnServerConnectionState(ServerConnectionStateArgs obj) {
        ServerState = obj.ConnectionState;
        //Debug.Log($"S: {ServerState}");

        // Join the lobby
        if (ClientType.Host == clientType) { 
            if (LocalConnectionState.Started == ServerState) {
                NetSceneManager.Instance.LoadCityScene();
                JoinLobby();
            }
        }
    }

    // ================== Outside Facing API ==================
    public override GameState PrepareLobby() {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        multipass = networkManager.TransportManager.GetTransport<Multipass>();
        ConfigTransports();

        return GameState.InLobby;
    }

    public override void HostLobby() {
        clientType = ClientType.Host;
        StartServer();
    }

    public override void JoinLobby() {
        ConnectClient();
    }

    public override void CloseLobby() {
        if (ClientType.Host == clientType) {
            NetSceneManager.Instance.UnloadCityScene();
            DisconnectClient();
            StopServer();
        }
        else { 
            DisconnectClient();
            NetSceneManager.Instance.UnloadCityScene(true);
        }
    }

    public override GameState StartGame() {
        GameManager.Instance.NotifyRoundStart();
        MenuManager.OpenMenu(MenuID.PlayerUI);
        return GameState.InGame;
    }
}

