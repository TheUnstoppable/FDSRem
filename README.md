# FDSRem
FDSRem is a library to communicate with C&amp;C Renegade servers over RenRem protocol with useful extra features such as keep alive.


### Features
- **Keep Alive**: This feature keeps the connection to RenRem alive and runs a timeout check in the client-side.
   - Keep alive sends the password to server every 20 seconds to keep the connection alive. After 10 failed attempts (server does not acknowledge the password, or library fails to send password) connection status will be set to `ConnectionStatus.Connecting`, and library will automatically attempt to reconnect.
   - If keep alive closes connection due to a client-side timeout, `DisconnectedEvent` will raise with `DisconnectReason.ClientTimeOut`.
   - After another 10 failed attempts while connection status is `ConnectionStatus.Connecting`, keep alive procedure will end, connection status will be set to `ConnectionStatus.Disconnected` and connection will need to be restarted manually.
   - If keep alive is disabled, application or service which is using the library will need to perform connectivity check while sending commands to RenRem for cases like connection closure without acknowledging clients.
   - If keep alive is disabled, `DisconnectedEvent` will never raise with `DisconnectReason.ClientTimeOut`.
- **Automatic Connection Handling**: Library will automatically handle and supress connection begin/closure messages.
   - Messages that represent connection closures, which are `** Connection timed out - Bye! **` and `** Server exiting - Connection closed! **` will be automatically handled and suppressed from `DataReceivedEvent`. Instead, these will be raised as an event via `DisconnectedEvent`, with `DisconnectReason.ServerTimeOut` and `DisconnectReason.ServerShutdown`.
   - Message beginning with `Password accepted.` will be automatically handled and suppressed from `DataReceivedEvent`. Instead, this will raise `ConnectedEvent`. Continuing lines after `Password accepted.` will be extracted to `RenRemClient.MessageOfTheDay` property.

### Examples
There is an example below of initializing `RenRemClient` with a host name.
```csharp
var renremPort = 4849;
var client = new RenRemClient("myserver.com", renremPort);
```

Alternatively, you can specify a local port number to bind the `UdpClient`.
Every `RenRemClient` constructor accepts the last parameter as local port optionally.
```csharp
var renremPort = 4849;
var localPort = 1337;
var client = new RenRemClient("myserver.com", renremPort, localPort);
```

It is also possible to initialize `RenRemClient` with an IP address instead.
```csharp
var renremPort = 4849;
var ipAddress = System.Net.IPAddress.Parse("127.0.0.1");
var client = new RenRemClient(ipAddress, renremPort);
```

Or an `IPEndPoint` instead.
```csharp
var renremPort = 4849;
var ipAddress = System.Net.IPAddress.Parse("127.0.0.1");
var ep = new System.Net.IPEndPoint(ipAddress, renremPort);
var client = new RenRemClient(ep);
```

To enable Keep Alive feature, you need to set `KeepAlive` property to `true`.
```csharp
client.KeepAlive = true;
```

- You can start connection to RenRem synchronously using `client.Start()`, or asynchronously using `client.StartAsync()` methods.
- You can send lines to RenRem synchronously or asynchronously, using `client.Send()` or `client.SendAsync()` methods.
- Disposing `RenRemClient` forces an immediate closure, and does not raise any events. Use `client.Stop()` if you want events to raise with a graceful closure.
