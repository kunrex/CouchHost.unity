#  CouchHost.unity  

A Unity package that simplifies local couch multiplayer setup. 

This package works hand in hand with <a href="https://github.com/kunrex/controller.ionic">controller.ionic</a> to simulate couch multiplayer on a local network! Simply install the app on your local device to connect to any game that uses this package!

---

## 🚀 Features  
✅ **Simplified Local Multiplayer Setup** – Streamlines controller connections and player management.  
✅ **INstall & Play** – Just install the app and start playing.  
✅ **Flexible Input Handling** – Supports multiple controllers seamlessly.  
✅ **Optimized for Performance** – Minimal overhead for smooth gameplay.  


## 📝 Installation  
1. **Download & Import**  (coming soon :D)
   - Download the `CouchHost.unitypackage` file.  
   - In Unity, go to `Assets > Import Package > Custom Package` and select the file.  

2. **Setup in Your Scene**  
   - Create a new game object in Unity, call it whatever you want! I will refer to it as "Server". Next, add the **preprovided** script on the game object called `Server`. Also add a **custom script** on Server called `ClientManager`
   - Create another game object in Unity, cald it whatever you want! I will refer to it as "Client". Next, add a **custom script** on Client called `Client`.

3. In `ClientManager` paste:

```cs
using Hosting.Unity;

using UnityEngine;

public sealed class ClientManager : UnityClientManager
{
    [SerializeField] private UnityClientObject playerPrefab;
    
    protected override void OnServerStart()
    {
        Debug.Log($"Server started at {RoomCode}");
    }

    protected override void OnStartRuntime()
    {
        Debug.Log($"Runtime started for Room: {RoomCode}, no new connections will be accepted");
    }

    protected override void OnStopRuntime()
    {
        Debug.Log($"Runtime stopped for Room: {RoomCode}, connections will now be accepted");
    }

    protected override void OnClientConnect(UnityClientObject client)
    {
        Debug.Log($"Runtime started for Room: {RoomCode}, no new connections will be accepted");
    }

    protected override void OnClientDisconnect(UnityClientObject client)
    {
        Debug.Log($"Runtime started for Room: {RoomCode}, no new connections will be accepted");
    }

    protected override UnityClientObject InstantiateClientObject(string hash)
    {
        return Instantiate(playerPrefab);
    }
}
```

4. In `Client` paste:
```cs
using UnityEngine;

using Hosting.Unity;

public sealed class Client : UnityClientObject
{
    protected override void OnStart()
    {
        Debug.Log("Hello World!");
    }
}
```

5. Make the Client game object a prefab and assign it in the inspctor of `ClientManager`. You can delete Client from the scene now!


6. **Run & Play**  
   - Press Play in Unity and start your couch multiplayer session! Look in the Debug Log for a message: 
```
Server started at RCCABAAG
```
`RCCABAAG` Is your room code. An instance of `controller.ionic` can enter this code to join this room! Every time this happens an instance of `Client` (your client prefab) is created! 

## Technicalities

Every scene must define a `Server` and an instance of `UnityClientManager`. These are singletons where `UnityClientManager` exists as an inherited implemenation. 

`UnityClientManager` is responsible for creating `UnityClientObject`s whenever a new connection is established by the server.

`UnityClientObject` also exists as an inherited implementation. It contains all properties associated with the controller. So one controller controls one ClientObject.

## Functions, Events and Properties:

### 1. UnityClientManager
1. `OnServerStart()`: Called when the server starts accepting connections for the very first time.

2. `StartRuntime()`: Starts the server RUNTIME. No new connections will be accepted now.
3. `OnStartRuntime()`: Called if RUNTIME starts effectively.

2. `StopRuntime()`: Stops the server RUNTIME. New connections will be accepted now.
3. `OnStopRuntime()`: Called if RUNTIME stops effectively.

4. `OnClientConnect()`: Called when a new client connects;
5. `OnClientDisconnect()`: Called whena  client disconnects;

6. `InstantiateClientObject()`: Defines how a client is instantiated in the scene.

### 2.UnityClientObject
1. `OnStart()`: Called when Unity's Start() is called.

2. `JoyStickA` (`Vector2`): Joystick A information.
3. `JoyStickA` (`Vector2`): Joystick B information.

4. `APerssed` (`bool`): Is A pressed.
5. `BPerssed` (`bool`): Is B pressed.
6. `XPerssed` (`bool`): Is X pressed.
7. `YPerssed` (`bool`): Is Y pressed

8. `UpPressed` (`bool`): Is Up pressed.
9. `DownPressed` (`bool`): Is Down pressed.
10. `RightPressed` (`bool`): Is Right pressed.
11. `LeftPressed` (`bool`): Is Left pressed

## 📝 License  
This project is licensed under the **MIT License**.  

