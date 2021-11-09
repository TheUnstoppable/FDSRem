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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FDSRem
{
    public partial class RenRemClient : IDisposable
    {
        private UdpClient _client = null;
        private Task _readTask = null;
        private Task _connectivityTask = null;
        private CancellationTokenSource _cancel = null;
        private object _taskLock = null;
        private bool _disposing = false;
        private string _password { get => CryptographyClass.Password; set => CryptographyClass.Password = value; }

        /// <summary>
        /// IP address of RenRem server.
        /// </summary>
        public IPAddress Address { get; private set; }

        /// <summary>
        /// Port of RenRem server.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Local port which the local socket bound to.
        /// </summary>
        public int LocalPort { get; private set; }

        /// <summary>
        /// Current connection status of <see cref="RenRemClient"/>.
        /// </summary>
        public ConnectionStatus Status { get; private set; }

        /// <summary>
        /// The latest connection acceptance message received from RenRem.
        /// </summary>
        public string MessageOfTheDay { get; private set; }

        /// <summary>
        /// The delay, in milliseconds to check incoming packets from FDS.
        /// </summary>
        public int TaskDelay { get; set; } = 100;

        /// <summary>
        /// Specifies <see cref="RenRemClient"/> should keeps your connection to RenRem alive or not.
        /// </summary>
        public bool KeepAlive { get; set; } = false;

        /// <summary>
        /// The time, in milliseconds to wait between the lines for keep alive.
        /// </summary>
        public readonly int KeepAliveDelay = 20000;

        /// <summary>
        /// Maximum attempts to connect to RenRem.
        /// </summary>
        public readonly int MaxAttempts = 10;

        /// <summary>
        /// The delay, in milliseconds to wait between connection attempts to RenRem. (Including when server does not respond to keep alive requests, if enabled.)
        /// </summary>
        public readonly int AttemptDelay = 1000;

        private RenRemClient()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes <see cref="RenRemClient"/> with an end point.
        /// </summary>
        /// <param name="EndPoint">End point of RenRem.</param>
        /// <param name="LocalPort">Local port to bind the client.</param>
        public RenRemClient(IPEndPoint EndPoint, int? LocalPort = null)
        {
            if (EndPoint == null)
                throw new ArgumentNullException("EndPoint");
            
            this.Address = EndPoint.Address;
            this.Port = EndPoint.Port;
            this.LocalPort = LocalPort ?? 0;

            _taskLock = new object();

            Status = ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Initializes <see cref="RenRemClient"/> with IP address and port.
        /// </summary>
        /// <param name="Address">IP address of where the FDS is.</param>
        /// <param name="Port">RenRem port of FDS.</param>
        /// <param name="LocalPort">Local port to bind the client.</param>
        public RenRemClient(IPAddress Address, int Port, int? LocalPort = null)
        {
            if (Address == null)
                throw new ArgumentNullException("Address");

            this.Address = Address;
            this.Port = Port;
            this.LocalPort = LocalPort ?? 0;

            _taskLock = new object();

            Status = ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Initializes <see cref="RenRemClient"/> with host address and port.
        /// </summary>
        /// <param name="Host">Host name of where the FDS is.</param>
        /// <param name="Port">RenRem port of FDS.</param>
        /// <param name="LocalPort">Local port to bind the client.</param>
        public RenRemClient(string Host, int Port, int? LocalPort = null)
        {
            if (Host == null)
                throw new ArgumentNullException("Host");

            Address = Dns.GetHostAddresses(Host).FirstOrDefault() ?? throw new NullReferenceException($"Could not find any IP addresses for \"{Host}\".");
            this.Port = Port;
            this.LocalPort = LocalPort ?? 0;

            _taskLock = new object();

            Status = ConnectionStatus.Disconnected;
        }

        public async void Dispose()
        {
            _disposing = true;

            _attempts = 0;
            _loginRespond = false;
            _cancel.Cancel(true);

            try
            {
                await Task.WhenAll(_readTask, _connectivityTask);
            }
            catch
            {
                
            }

            _readTask.Dispose();
            _connectivityTask.Dispose();

            _cancel.Dispose();
            _client.Dispose();
        }
    }
}
