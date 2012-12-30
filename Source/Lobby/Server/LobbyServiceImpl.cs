﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Sanguosha.Lobby.Core;
using System.Threading;

namespace Sanguosha.Lobby.Server
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
    public class LobbyServiceImpl : ILobbyService
    {
        Dictionary<int, Room> rooms;
        int newRoomId;
        Dictionary<Account, Guid> loggedInAccountToGuid;
        Dictionary<Guid, Account> loggedInGuidToAccount;
        Dictionary<IGameClient, Guid> loggedInChannelsToGuid;
        Dictionary<Guid, IGameClient> loggedInGuidToChannel;
        Dictionary<Guid, Room> loggedInGuidToRoom;

        private bool VerifyClient(LoginToken token)
        {
            if (loggedInGuidToAccount.ContainsKey(token.token))
            {
                if (loggedInGuidToChannel.ContainsKey(token.token))
                {
                    if (loggedInGuidToChannel[token.token] == OperationContext.Current.GetCallbackChannel<IGameClient>())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public LobbyServiceImpl()
        {
            rooms = new Dictionary<int, Room>();
            loggedInGuidToAccount = new Dictionary<Guid, Account>();
            loggedInGuidToChannel = new Dictionary<Guid, IGameClient>();
            loggedInAccountToGuid = new Dictionary<Account, Guid>();
            loggedInChannelsToGuid = new Dictionary<IGameClient, Guid>();
            loggedInGuidToRoom = new Dictionary<Guid, Room>();
            newRoomId = 1;
        }

        public LoginStatus Login(int version, string username, out LoginToken token)
        {
            Console.WriteLine("{0} logged in", username);
            var connection = OperationContext.Current.GetCallbackChannel<IGameClient>(); 
            token = new LoginToken();
            token.token = System.Guid.NewGuid();
            Account account = new Account() { Username = username };
            loggedInGuidToAccount.Add(token.token, account);
            loggedInGuidToChannel.Add(token.token, connection);
            loggedInAccountToGuid.Add(account, token.token);
            loggedInChannelsToGuid.Add(connection, token.token);
            return LoginStatus.Success;
        }

        public void Logout(LoginToken token)
        {
            if (VerifyClient(token))
            {
                Console.WriteLine("{0} logged out", loggedInGuidToAccount[token.token].Username);
                loggedInGuidToAccount.Remove(token.token);
            }
        }

        public IEnumerable<Room> GetRooms(LoginToken token, bool notReadyRoomsOnly)
        {
            if (VerifyClient(token))
            {
                List<Room> ret = new List<Room>();
                foreach (var pair in rooms)
                {
                    if (!notReadyRoomsOnly || pair.Value.State == RoomState.Waiting)
                    {
                        ret.Add(pair.Value);
                    }
                }
                return ret;
            }
            return null;
        }

        public Room CreateRoom(LoginToken token, string password = null)
        {
            if (VerifyClient(token))
            {
                if (loggedInGuidToRoom.ContainsKey(token.token))
                {
                    return null;
                }

                while (rooms.ContainsKey(newRoomId))
                {
                    newRoomId++;
                }
                Room room = new Room();
                for (int i = 0; i < 8; i++)
                {
                    room.Seats.Add(new Seat() { State = SeatState.Empty });
                }
                room.Seats[0].Account = loggedInGuidToAccount[token.token];
                room.Seats[0].State = SeatState.Host;
                room.Id = newRoomId;
                room.OwnerId = 0;
                rooms.Add(newRoomId, room);
                if (loggedInGuidToRoom.ContainsKey(token.token))
                {
                    loggedInGuidToRoom.Remove(token.token);
                }
                loggedInGuidToRoom.Add(token.token, room);
                Console.WriteLine("created room {0}", newRoomId);
                return room;
            }
            Console.WriteLine("Invalid createroom call");
            return null;
        }

        public RoomOperationResult EnterRoom(LoginToken token, int roomId, bool spectate, string password = null)
        {
            if (VerifyClient(token))
            {
                Console.WriteLine("{1} Enter room {0}", roomId, token.token);
                if (loggedInGuidToRoom.ContainsKey(token.token))
                {
                    return RoomOperationResult.Locked;
                }
                if (rooms.ContainsKey(roomId))
                {
                    if (rooms[roomId].State == RoomState.Gaming) return RoomOperationResult.Locked;
                    int seatNo = 0;
                    foreach (var seat in rooms[roomId].Seats)
                    {
                        Console.WriteLine("Testing seat {0}", seatNo);
                        if (seat.Account == null && seat.State == SeatState.Empty)
                        {
                            loggedInGuidToRoom.Add(token.token, rooms[roomId]);
                            seat.Account = loggedInGuidToAccount[token.token];
                            seat.State = SeatState.GuestTaken;
                            NotifyRoomLayoutChanged(roomId);
                            Console.WriteLine("Seat {0}", seatNo);
                            return RoomOperationResult.Success;
                        }
                        seatNo++;
                    }
                    Console.WriteLine("Full");
                    return RoomOperationResult.Full;
                }
            }
            Console.WriteLine("Rogue enter room calls");
            return RoomOperationResult.Auth;
        }

        public RoomOperationResult ExitRoom(LoginToken token, int roomId)
        {
            if (VerifyClient(token))
            {
                if (loggedInGuidToRoom.ContainsKey(token.token))
                {
                    foreach (var seat in loggedInGuidToRoom[token.token].Seats)
                    {
                        if (seat.Account == loggedInGuidToAccount[token.token])
                        {
                            seat.Account = null;
                            seat.State = SeatState.Empty;
                            loggedInGuidToRoom.Remove(token.token);
                            if (!loggedInGuidToRoom[token.token].Seats.Any(state => state.State != SeatState.Empty))
                            {
                                rooms.Remove(loggedInGuidToRoom[token.token].Id);
                                return RoomOperationResult.Success;
                            }
                            NotifyRoomLayoutChanged(roomId);
                            return RoomOperationResult.Success;
                        }
                    }
                }
            }
            return RoomOperationResult.Auth;
        }

        private void NotifyRoomLayoutChanged(int roomId)
        {
            foreach (var notify in rooms[roomId].Seats)
            {
                if (notify.Account != null)
                {
                    loggedInGuidToChannel[loggedInAccountToGuid[notify.Account]].NotifyRoomUpdate(rooms[roomId].Id, rooms[roomId]);
                }
            }
        }

        private void NotifyGameStart(int roomId)
        {
            var current = OperationContext.Current.GetCallbackChannel<IGameClient>();
            foreach (var notify in rooms[roomId].Seats)
            {
                if (notify.Account != null)
                {
                    var channel = loggedInGuidToChannel[loggedInAccountToGuid[notify.Account]];
                    if (channel != current) channel.NotifyGameStart("");
                }
            }
        }

        public RoomOperationResult RoomOperations(LoginToken token, RoomOperation op, int arg1, int arg2)
        {
            if (VerifyClient(token))
            {
                if (op == RoomOperation.ChangeSeat)
                {
                    if (!loggedInGuidToRoom.ContainsKey(token.token)) { return RoomOperationResult.Locked; }
                    var room = loggedInGuidToRoom[token.token];
                    if (room.State == RoomState.Gaming)
                    {
                        return RoomOperationResult.Locked;
                    }
                    if (arg1 < 0 || arg1 >= room.Seats.Count) return RoomOperationResult.Invalid;
                    var seat = room.Seats[arg1];
                    if (seat.Account == null && seat.State == SeatState.Empty)
                    {
                        foreach (var remove in room.Seats)
                        {
                            if (remove.Account == loggedInGuidToAccount[token.token])
                            {
                                seat.State = remove.State;
                                seat.Account = remove.Account;
                                remove.Account = null;
                                remove.State = SeatState.Empty;
                                NotifyRoomLayoutChanged(newRoomId);

                                return RoomOperationResult.Success;
                            }
                        }
                    }
                    Console.WriteLine("Full");
                    return RoomOperationResult.Full;
                }
                if (op == RoomOperation.StartGame)
                {
                    if (!loggedInGuidToRoom.ContainsKey(token.token)) { return RoomOperationResult.Locked; }
                    var thread = new Thread(GameService.StartGameService) { IsBackground = true };
                    thread.Start();
                    NotifyGameStart(loggedInGuidToRoom[token.token].Id);
                    return RoomOperationResult.Success;
                }
            }
            return RoomOperationResult.Auth;
        }
    }
}
