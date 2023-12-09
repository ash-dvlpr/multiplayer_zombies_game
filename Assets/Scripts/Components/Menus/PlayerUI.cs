using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUI : AMenu {
    public override MenuID MenuKey { get => MenuID.PlayerUI; }

    // ====================== References =====================
    [field: SerializeField] public ResourceBar Bar { get; private set; }

    // ===================== Custom Code =====================
    // TODO: if singleplayer, pause the game when opening the pause menu 
    // TODO: when pause menu open, lock user input

    public override void OpenMenu() { 
        // Lock Mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        base.OpenMenu();
    }
    public override void CloseMenu() { 
        // Unlock Mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        base.CloseMenu();
    }

    // ===================== UI Actions ======================
    
}
