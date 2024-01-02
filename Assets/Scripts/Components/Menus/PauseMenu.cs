using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PauseMenu : AMenu {
    public override MenuID MenuKey { get => MenuID.Pause; }

    [SerializeField] GameObject roomCodePanel;
    [SerializeField] TMP_Text roomCodeDisplay;

    // ===================== Custom Code =====================

    public override void OpenMenu() {
        GameManager.ClientInPauseMenu = true;

        PauseGame();
        UpdateRoomCode();
        base.OpenMenu();
    }
    public override void CloseMenu() {
        GameManager.ClientInPauseMenu = false;

        UnpauseGame();
        ClearRoomCode();
        base.CloseMenu();
    }

    // ===================== Custom Code =====================
    private void UpdateRoomCode() {
        if (LobbyType.MultiPlayer == GameManager.LobbyType) { 
            // Set lobby code text
            var lobby = (MultiPlayerLobby) GameManager.GetLobby;
            roomCodeDisplay.text = $"{lobby.LobbyCode}";

            roomCodePanel.SetActive(true);
        } else {
            ClearRoomCode();
        }
    }
    
    private void ClearRoomCode() { 
        roomCodePanel.SetActive(false);
    }

    private void PauseGame() { 
        if (LobbyType.SinglePlayer == GameManager.LobbyType) Time.timeScale = 0f;
    }
    
    private void UnpauseGame() { 
        if (LobbyType.SinglePlayer == GameManager.LobbyType) Time.timeScale = 1f;
    }

    // ===================== UI Actions ======================
    public void OnClick_CopyCode() {
        GUIUtility.systemCopyBuffer = roomCodeDisplay.text;
        Debug.Log($"Code copied to clipboard: {GUIUtility.systemCopyBuffer}");
    }
    public void OnClick_Resume() {
        MenuManager.OpenMenu(MenuID.PlayerUI);
    }
    public void OnClick_Settings() {
        MenuManager.OpenMenu(MenuID.Settings);
    }
    public void OnClick_MainMenu() {
        MenuManager.CloseMenu();
        GameManager.ExitToTittleScreen();
    }
}
