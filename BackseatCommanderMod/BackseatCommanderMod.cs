using System;
using System.Collections;
using System.Net;
using System.Reflection;
using BackseatCommanderMod.Server;
using BepInEx;
using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BackseatCommanderMod
{
    [BepInPlugin("engineering.sea-x.BackseatCommander", "Backseat Commander", "0.1.0")]
    public class BackseatCommanderMod : BaseUnityPlugin
    {
        private CommanderServer? server;
        private GameInstance? game = null;

        private void Awake()
        {
            Static.Logger = Logger;
            Harmony.CreateAndPatchAll(typeof(BackseatCommanderMod).Assembly, "engineering.sea-x.BackseatCommander");

            StartCoroutine(WaitForGameInstance());
        }

        private void OnDestroy()
        {
            server?.Dispose();
            server = null;
        }

        private IEnumerator WaitForGameInstance()
        {
            for (; ; )
            {
                game = FindObjectOfType<GameInstance>();
                if (game != null)
                {
                    game?.Messages.Subscribe<GameStateChangedMessage>(OnGameStateChanged);
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnGameStateChanged(MessageCenterMessage message)
        {
            if (!(message is GameStateChangedMessage msg))
            {
                Logger.LogError($"[OnGameStateChanged] GameStateChangedMessage wasn't of type GameStateChangedMessage");
                return;
            }

            Logger.LogInfo($"[OnGameStateChanged] Got {msg.CurrentState}");

            switch (msg.CurrentState)
            {
                case GameState.Invalid:
                case GameState.WarmUpLoading:
                case GameState.Loading:
                    return;
            }

            Logger.LogInfo($"[OnGameStateChanged] Got {msg.CurrentState}, starting initialization");
            game?.Messages.Unsubscribe<GameStateChangedMessage>(OnGameStateChanged);
            StartServer();
        }

        private void StartServer()
        {
            Logger.LogInfo($"[StartServer] Starting server");
            server = new CommanderServer(IPAddress.Loopback, 12345);
            server.Start();
        }
    }
}
