using System;
using System.Collections;
using System.Net;
using BackseatCommanderMod.Server;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using UnityEngine;

namespace BackseatCommanderMod
{
    [BepInPlugin("engineering.sea-x.BackseatCommander", "Backseat Commander", "0.1.0")]
    public class BackseatCommanderMod : BaseUnityPlugin
    {
        private ConfigEntry<string> configBindAddress;
        private ConfigEntry<int> configBindPort;
        private ConfigEntry<string> configPlubicFacingHost;
        private CommanderServer server;
        private GameInstance game = null;

        private void Awake()
        {
            Static.Logger = Logger;
            BindConfigs();

            Harmony.CreateAndPatchAll(typeof(BackseatCommanderMod).Assembly, "engineering.sea-x.BackseatCommander");
            StartCoroutine(WaitForGameInstance());
        }

        private void OnDestroy()
        {
            server?.Dispose();
            server = null;
        }

        private void BindConfigs()
        {
            configBindAddress = Config.Bind(
                "General",
                "BindAddress",
                "0.0.0.0",
                "The TCP address/host to which the HTTP and WebSocket server binds to. The default 0.0.0.0 means that the server will be accessible from anywhere. Can be any IPv4 address, but it may be useful to limit it to your LAN address, e.g. 192.168.1.123, which will only listen to connections from your local network."
            );
            configBindPort = Config.Bind(
                "General",
                "BindPort",
                6674,
                "The port to which the HTTP and WebSocket server binds to. Must be a number between 1025-65535, or 0 for a random available port."
            );
            configPlubicFacingHost = Config.Bind(
                "General",
                "PublicFacingHostName",
                "",
                "The public facing host name. Used by the server in redirect URLs and for checking the WebSocket request origin. Leave empty to default to the same as the bind host. If exposing the server to the public, or serving behind a reverse proxy, set to the host name (e.g. example.com or example.com:12345 or 192.168.100.1:12345, no protocol [http://] or paths [/]) via which the browser accesses the commander page."
            );
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
            game.Messages.Unsubscribe<GameStateChangedMessage>(OnGameStateChanged);

            StartServer();
            game.Messages.Subscribe<GameStateChangedMessage>(GameLoadedOnGameStateChanged);
        }

        private void GameLoadedOnGameStateChanged(MessageCenterMessage message)
        {
            if (!(message is GameStateChangedMessage msg))
            {
                return;
            }

            switch (msg.CurrentState)
            {
                case GameState.FlightView:
                case GameState.Map3DView:
                case GameState.PhotoMode:
                case GameState.Launchpad:
                case GameState.Runway:
                    RegisterViewStateBindings();
                    break;

                default:
                    UnregisterViewStateBindings();
                    break;

                    //case GameState.Invalid:
                    //case GameState.WarmUpLoading:
                    //case GameState.MainMenu:
                    //case GameState.KerbalSpaceCenter:
                    //case GameState.VehicleAssemblyBuilder:
                    //case GameState.BaseAssemblyEditor:
                    //case GameState.ColonyView:
                    //case GameState.PlanetViewer:
                    //case GameState.TrainingCenter:
                    //case GameState.TrackingStation:
                    //case GameState.ResearchAndDevelopment:
                    //case GameState.Flag:
                    //    // unsub?
                    //    break;
            }
        }

        private void UnregisterViewStateBindings()
        {
            var provider = game?.ViewController?.DataProvider?.UniverseDataProvider;
            if (provider == null)
            {
                return;
            }

            provider.TimeRateIndex.OnChanged -= OnChangedTimeRateIndex;
        }

        private void RegisterViewStateBindings()
        {
            var provider = game?.ViewController?.DataProvider?.UniverseDataProvider;
            if (provider == null)
            {
                return;
            }

            provider.TimeRateIndex.OnChanged += OnChangedTimeRateIndex;
        }

        private void OnChangedTimeRateIndex()
        {
            server.CommaderService.OnTimeRateIndexChanged(
                game?.ViewController?.DataProvider?.UniverseDataProvider?.TimeRateIndex?.GetValue() ?? 0
            );
        }

        private void StartServer()
        {
            Logger.LogInfo($"[StartServer] Starting server");
            server = new CommanderServer(
                host: IPAddress.Parse(configBindAddress.Value.Trim()),
                port: configBindPort.Value,
                publicFacingHost: configPublicFacingUrl.Value
            );
            server.Start();
        }
    }
}
