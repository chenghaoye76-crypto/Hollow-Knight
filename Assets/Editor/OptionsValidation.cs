using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class OptionsValidation
{
    [MenuItem("Tools/Hollow Knight/Validate Options", true)]
    private static bool CanValidate() => EditorApplication.isPlaying;

    [MenuItem("Tools/Hollow Knight/Validate Options")]
    public static void Validate()
    {
        string[] names = { "Key.Left", "Pad.0", "Pad.1", "Pad.2" };
        var saved = new Dictionary<string, string>();
        foreach (string name in names)
            if (PlayerPrefs.HasKey("Options." + name)) saved[name] = PlayerPrefs.GetString("Options." + name);
        Keyboard keyboard = null;
        Gamepad pad = null;
        var actions = new[] { new InputAction("Move"), new InputAction("Jump"), new InputAction("Dash"),
            new InputAction("Attack"), new InputAction("UpAttack"), new InputAction("DownAttack") };
        try
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Panel/OptionsPanel");
            Require(prefab != null, "Options prefab exists");
            foreach (string child in new[] { "Options", "Fleur", "Back", "Button/Audio", "Button/Video", "Button/Game", "Button/Keyboard", "Button/Controller" })
                Require(prefab.transform.Find(child) != null, "Missing UI reference: " + child);
            for (int i = 0; i < 9; i++)
                Require(Resources.Load<Sprite>($"Sprites/Inventory & UI/Fleurs/Warning_Fleur{i:0000}") != null, "Missing fleur frame " + i);

            string right = GameOptions.Key(1);
            Require(!GameOptions.SetKey(0, right), "Duplicate key must be rejected");
            string testKey = "leftArrow";
            foreach (string candidate in new[] { "leftArrow", "f9", "f10", "f11", "f12", "home", "end", "pageUp" })
            {
                bool used = false;
                for (int i = 1; i < GameOptions.Actions.Length; i++) used |= GameOptions.Key(i) == candidate;
                if (!used) { testKey = candidate; break; }
            }
            Require(GameOptions.SetKey(0, testKey), "A free key can be rebound");
            Require(PlayerPrefs.GetString("Options.Key.Left") == testKey, "Key is persisted");
            string oldJump = GameOptions.Pad(0), oldDash = GameOptions.Pad(1);
            GameOptions.SetPad(0, oldDash);
            Require(GameOptions.Pad(0) == oldDash && GameOptions.Pad(1) == oldJump, "Controller conflicts swap");

            keyboard = InputSystem.AddDevice<Keyboard>();
            pad = InputSystem.AddDevice<Gamepad>();
            GameOptions.ConfigureInput(actions[0], actions[1], actions[2], actions[3], actions[4], actions[5]);
            int bindingCount = actions[0].bindings.Count;
            GameOptions.ConfigureInput(actions[0], actions[1], actions[2], actions[3], actions[4], actions[5]);
            Require(actions[0].bindings.Count == bindingCount, "Reconfiguration must not duplicate bindings");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(((UnityEngine.InputSystem.Controls.KeyControl)keyboard[testKey]).keyCode));
            InputSystem.Update();
            Require(actions[0].ReadValue<Vector2>().x < -0.9f, "Rebound keyboard key moves left");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueDeltaStateEvent(pad.leftStick, new Vector2(0.8f, 0f));
            InputSystem.Update();
            Require(actions[0].ReadValue<Vector2>().x > 0.5f, "Controller stick moves right");
            InputSystem.QueueDeltaStateEvent(pad.leftStick, Vector2.zero);
            string jumpButton = GameOptions.Pad(0);
            var padState = new GamepadState();
            if (jumpButton == "leftTrigger") padState.leftTrigger = 1f;
            else if (jumpButton == "rightTrigger") padState.rightTrigger = 1f;
            else
            {
                string enumName = jumpButton.Replace("button", "");
                padState = padState.WithButton((GamepadButton)Enum.Parse(typeof(GamepadButton), enumName, true));
            }
            InputSystem.QueueStateEvent(pad, padState);
            InputSystem.Update();
            Require(actions[1].ReadValue<float>() > 0.9f, "Configured controller jump works");
            InputSystem.QueueStateEvent(pad, new GamepadState());
            var upKey = ((UnityEngine.InputSystem.Controls.KeyControl)keyboard[GameOptions.Key(2)]).keyCode;
            var attackKey = ((UnityEngine.InputSystem.Controls.KeyControl)keyboard[GameOptions.Key(6)]).keyCode;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(upKey));
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(upKey, attackKey));
            InputSystem.Update();
            Require(actions[4].ReadValue<float>() > 0.9f, "Up + Attack follows configured keys");
            Debug.Log("OPTIONS VALIDATION PASSED: UI references, 9 animation frames, persistence, duplicate rejection, pad swapping, repeated binding setup, keyboard movement, gamepad movement/jump and directional attack.");
        }
        finally
        {
            foreach (var action in actions) action.Dispose();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (pad != null) InputSystem.RemoveDevice(pad);
            foreach (string name in names)
            {
                if (saved.TryGetValue(name, out string value)) PlayerPrefs.SetString("Options." + name, value);
                else PlayerPrefs.DeleteKey("Options." + name);
            }
            PlayerPrefs.Save();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Options validation failed: " + message);
    }
}
