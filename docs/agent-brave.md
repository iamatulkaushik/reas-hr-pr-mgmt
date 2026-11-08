# The punch agent (Brave): what it is and how to run it

`RevolutionAgent.exe` (project `Revolution.Agent`) runs on a computer on the factory LAN. It asks the server for the company's active devices, reads each one with the reader for its brand, and sends the new punches to the server. The server never connects to a device and a device never connects to the server (ADR 0013).

## Settings
`agent.settings.json` sits next to the program. It holds the connection string of the agent login (`hrms_agent`, created by the Setup tool), the agent key issued under company settings, and the seconds between rounds (15 to 3600, default 60). The connection string and the key are stored protected for this computer, never in plain text. Create the file once, on that computer, in an administrator window:

    RevolutionAgent protect "<connection string>" "<agent key>" 60

## Running
- `RevolutionAgent once` does one round and stops. The result is in `agent.log`.
- `RevolutionAgent run` runs in a window until Ctrl+C.
- With no argument it runs as a Windows Service. Install it with `sc.exe create RevolutionAgent binPath= "<full path>\RevolutionAgent.exe" start= auto`, then start it from Services. The Setup tool can take this over later.

## The log
`agent.log` holds a time and one line per device: the device code and counts (pulled, stored, rejected, waiting for an employee) or a plain error. It never holds a key, a connection string, a host or a punch. At about 2 MB it moves to `agent.log.old`.

## What a round does
For each active device: pull from five minutes before the newest punch the server holds (all of it the first time), throw away punches that cannot be believed (no id and no card, a time more than a day ahead or three years back), and send the rest. The server stores a punch once, so pulling twice does no harm. A device that cannot be read, or a brand with no reader, is reported and the next device goes on. A server that refuses fails the round and the agent tries again at the next one. Punches stay on the device until it is read, so nothing is lost while the agent is down.

## Brands
A reader is a class that implements `IPunchSource` for one brand, in its own project, listed in `AgentReaders`. None is listed yet: the eSSL reader (K30 Pro and K90 Pro) waits for the Owner's decision between pulling over the ZK protocol and devices that push. Until then an eSSL device shows as "No reader for brand ESSL".

## What is not tested here
The service, the settings protection and the server calls need Windows and a database. The round, the pull coordinator and the batch checks are tested with fakes (`Revolution.Modules.Biometric.Tests`).
