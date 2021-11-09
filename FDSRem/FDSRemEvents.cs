/*
    FDSRem - C&C Renegade FDS Communicator Library
    Copyright (C) 2021 Unstoppable

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
    See the LICENSE file for more details.
*/


using System;

namespace FDSRem
{
    public enum DisconnectReason
    {
        /// <summary>
        /// <see cref="RenRemClient"/> raised Stop event.
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Server closes connection on inactivity.
        /// </summary>
        ServerTimeOut,

        /// <summary>
        /// Client doesn't get response from server.
        /// </summary>
        ClientTimeOut,

        /// <summary>
        /// Server sent shutdown.
        /// </summary>
        ServerShutdown,

        /// <summary>
        /// Socket exception.
        /// </summary>
        Error
    }

    public enum ConnectionStatus
    {
        /// <summary>
        /// <see cref="RenRemClient"/> is attempting to connect to RenRem.
        /// </summary>
        Connecting = 0,

        /// <summary>
        /// <see cref="RenRemClient"/> is connected to RenRem.
        /// </summary>
        Connected,

        /// <summary>
        /// <see cref="RenRemClient"/> is not connected to RenRem.
        /// </summary>
        Disconnected
    }

    public delegate void DataReceive(string Data);
    public delegate void Connect();
    public delegate void Disconnect(DisconnectReason Reason);
    public delegate void Error(Exception Exception);

    public partial class RenRemClient : IDisposable
    {
        /// <summary>
        /// Raises when a new output from FDS is received.
        /// </summary>
        public event DataReceive DataReceivedEvent;

        /// <summary>
        /// Raises when <see cref="RenRemClient"/> connects to RenRem successfully.
        /// </summary>
        public event Connect ConnectedEvent;

        /// <summary>
        /// Raises when <see cref="RenRemClient"/> disconnects from RenRem.
        /// </summary>
        public event Disconnect DisconnectedEvent;

        /// <summary>
        /// Raises when an exception is thrown in the worker threads.
        /// </summary>
        public event Error ExceptionThrownEvent;
    }
}
