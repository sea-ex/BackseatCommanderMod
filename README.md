# BackseatCommanderMod

Proof-of-concept mod for Kerbal Space Program 2 which lets you control your ship's direction using your phone's gyroscope/orientation sensor over LAN. Mobile interface runs in a web browser (Chrome on Android).

## Running

Only works on Windows 8.1>= and probably only on Chrome on Android. iOS probably doesn't work.

1. Install the BepInEx modloader for KSP2, from SpaceDock or elsewhere
2. Download the latest .zip from the GitHub Releases
3. Extract to the KSP2 install directory
4. Edit BepInEx\config\engineering.sea-x.BackseatCommander.cfg with a text editor to replace the IP address in your `PublicFacingUrl` with your computer's LAN address (ipconfig or view windows network settings), keep the port as it is
5. Install [Node.js v18](https://nodejs.org/en/download/)
6. In the BepInEx\plugins\enmgineering.sea-x.BackseatCommander directory, run `start-proxy.bat`
   - This will start a HTTPS->HTTP proxy on your computer. We need to serve the web page over HTTPS to get access to sensitive sensors (orientation/gyroscope).
   - Windows Firewall will ask if you want to allow connections. Either allow it, or manually allow inbound connections from your LAN or just from your phone's IP address
   - Your phone must be connected to the same LAN as your computer (on same WiFi, for example)
7. Start KSP2
8. Load up a save where you're in orbit
9. Open the URL of your computer's LAN address in your phone's browser (e.g. `https://192.168.1.123:6674/`)
10. Click on "Permissions" on the website
11. Enjoy controlling your ship

## How it works

The mod starts a WebSocket server inside KSP2. It serves a simple React app, bundled into a single HTML file, which your phone executes. The frontend uses the RelativeOrientationSensor Web API to get a quaternion of the phone's rotation, which it transmits over WebSocket to the mod. The mod then manipulates your vessel's SAS to change its target orientation. It doesn't work very well, and I suspect I have a wrong reference frame for the orientation or something. But end-to-end, it works.

// todo

## License

MIT, see LICENSE
