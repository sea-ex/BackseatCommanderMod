using System;
using System.Collections;
using System.Net;
using BackseatCommanderMod.Server;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.impl;
using System.Collections.Concurrent;
using UnityEngine;

namespace BackseatCommanderMod
{
    [BepInPlugin("engineering.sea-x.BackseatCommander", "Backseat Commander", "0.1.0")]
    public class BackseatCommanderMod : BaseUnityPlugin
    {
        // sorry awful hack
        public static BackseatCommanderMod Instance;

        private ConfigEntry<string> configBindAddress;
        private ConfigEntry<int> configBindPort;
        private ConfigEntry<string> configPublicFacingUrl;

        private CommanderServer server;

        private GameInstance game = null;

        ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();

        private bool inFlightScene = false;
        private bool userRequestedStart = false;

        private void Awake()
        {
            Instance = this;
            Static.Logger = Logger;
            BindConfigs();

            Harmony.CreateAndPatchAll(typeof(BackseatCommanderMod).Assembly, "engineering.sea-x.BackseatCommander");
            StartCoroutine(WaitForGameInstance());
        }

        private void OnDestroy()
        {
            inFlightScene = false;
            userRequestedStart = false;

            server?.Dispose();
            server = null;
        }

        private void Update()
        {
            if (!inFlightScene) return;

            while (mainThreadQueue.TryDequeue(out Action action))
            {
                Logger.LogDebug("Running update");
                action.Invoke();
            }
        }

        private void BindConfigs()
        {
            configBindAddress = Config.Bind(
                "General",
                "BindAddress",
                "127.0.0.1",
                "The TCP address/host to which the HTTP and WebSocket server binds to. The default 0.0.0.0 means that the server will be accessible from anywhere. Can be any IPv4 address, but it may be useful to limit it to your LAN address, e.g. 192.168.1.123, which will only listen to connections from your local network."
            );
            configBindPort = Config.Bind(
                "General",
                "BindPort",
                6674,
                "The port to which the HTTP and WebSocket server binds to. Must be a number between 1025-65535, or 0 for a random available port."
            );
            configPublicFacingUrl = Config.Bind(
                "General",
                "PublicFacingUrl",
                "",
                "The public facing origin. Used by the server in redirect URLs and for checking the WebSocket request origin. Leave empty to default to the same as the bind host. If exposing the server to the public, or serving behind a reverse proxy, set to the host name with protocol (e.g. https://example.com or http://example.com:12345 or http://192.168.100.1:12345, no paths [/]) via which the browser accesses the commander page."
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

            this.inFlightScene = false;
            provider.TimeRateIndex.OnChanged -= OnChangedTimeRateIndex;
        }

        private void RegisterViewStateBindings()
        {
            var provider = game?.ViewController?.DataProvider?.UniverseDataProvider;
            if (provider == null)
            {
                return;
            }

            this.inFlightScene = true;
            provider.TimeRateIndex.OnChanged += OnChangedTimeRateIndex;
        }

        private void OnChangedTimeRateIndex()
        {
            server.CommaderService.OnTimeRateIndexChanged(
                game?.ViewController?.DataProvider?.UniverseDataProvider?.TimeRateIndex?.GetValue() ?? 0
            );
        }

        private VesselComponent activeVessel = null;

        internal void RegisterCommanderServiceSession(CommanderService service)
        {
            service.OnStart += CommaderService_OnStart;
            service.OnStop += CommaderService_OnStop;
            service.OnGyroscopeData += CommaderService_OnGyroscopeData;
        }

        internal void UnregisterCommanderServiceSession(CommanderService service)
        {
            service.OnStart -= CommaderService_OnStart;
            service.OnStop -= CommaderService_OnStop;
            service.OnGyroscopeData -= CommaderService_OnGyroscopeData;
        }

        private void StartServer()
        {
            Logger.LogInfo($"[StartServer] Starting server");
            server = new CommanderServer(
                host: IPAddress.Parse(configBindAddress.Value.Trim()),
                port: configBindPort.Value,
                publicFacingUrl: configPublicFacingUrl.Value
            );
            server.Start();
        }

        private void CommaderService_OnStop(object sender, EventArgs e)
        {
            userRequestedStart = false;
            activeVessel = null;
        }

        private void CommaderService_OnStart(object sender, EventArgs e)
        {
            mainThreadQueue.Enqueue(() =>
            {
                activeVessel = game?.ViewController?.GetActiveSimVessel(true);
                if (activeVessel == null)
                {
                    Logger.LogInfo("activeVessel was null");
                    return;
                }

                var autopilot = activeVessel.Autopilot;
                if (autopilot.Enabled)
                {
                    autopilot.Deactivate();
                }
                if (autopilot.AutopilotMode != AutopilotMode.Normal)
                {
                    autopilot.SetMode(AutopilotMode.Normal);
                }

                userRequestedStart = true;
            });
        }

        private void CommaderService_OnGyroscopeData(object sender, GyroscopeDataEventArgs e)
        {
            if (activeVessel == null) return;

            mainThreadQueue.Enqueue(() =>
            {
                var sas = activeVessel.Autopilot.SAS;
                sas.SetTargetOrientation(new Vector(sas.ReferenceFrame, e.Angle), false);
            });
        }
    }
}
