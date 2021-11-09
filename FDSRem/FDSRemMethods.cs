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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FDSRem
{
    public partial class RenRemClient : IDisposable
    {
        /// <summary>
        /// Connects and authorizes to RenRem.
        /// </summary>
        /// <param name="Password">Password of RenRem.</param>
        public void Start(string Password)
        {
            if (Password == null || Password.Length != 8)
                throw new ArgumentException("Password must not be null and has to be exactly 8 letters long.", nameof(Password));

            if (Status != ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Instance is already connected.");

            _password = Password;

            _client = this.LocalPort == 0 ? new UdpClient() : new UdpClient(LocalPort);

            _client.Connect(Address, Port);
            _cancel = new CancellationTokenSource();

            Status = ConnectionStatus.Connecting;

            _readTask = new Task(ReadTask, _cancel.Token, TaskCreationOptions.LongRunning);
            _readTask.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);

            _connectivityTask = new Task(ParallelConnectivityWorker, _cancel.Token, TaskCreationOptions.LongRunning);
            _connectivityTask.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);

            _readTask?.Start();
            _connectivityTask?.Start();

            var Pass = CryptographyClass.Encrypt(_password);
            _client.Send(Pass, Pass.Length);
        }

        /// <summary>
        /// Connects and authorizes to RenRem asynchronously.
        /// </summary>
        /// <param name="Password">Password of RenRem.</param>
        /// <returns>A <see cref="Task"/> object which represents the current status of the task.</returns>
        public Task StartAsync(string Password)
        {
            return Task.Run(() => Start(Password));
        }

        /// <summary>
        /// Ends the connection to RenRem.
        /// </summary>
        public void Stop()
        {
            if (Status == ConnectionStatus.Disconnected)
                throw new InvalidOperationException("Instance is already disconnected.");

            var Data = CryptographyClass.Encrypt("bye");
            _client.Send(Data, Data.Length);

            _cancel.Cancel();
            _client.Dispose();
            _client = null;

            Status = ConnectionStatus.Disconnected;
            DisconnectedEvent?.Invoke(DisconnectReason.Closed);

            _attempts = 0;
            _loginRespond = false;
        }

        /// <summary>
        /// Sends a line to the RenRem.
        /// </summary>
        /// <param name="Line">Command line.</param>
        public void Send(string Line)
        {
            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("Line could not be sent, socket is not connected.");

            if (Line.Contains("bye", StringComparison.OrdinalIgnoreCase) || Line.Contains("connect", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("RenRem reserved commands and containing lines are disallowed.");
            }
            else
            {
                var Data = CryptographyClass.Encrypt(Line);
                _client.Send(Data, Data.Length);
            }
        }

        /// <summary>
        /// Sends a line to the RenRem asynchronously.
        /// </summary>
        /// <param name="Line">Command line.</param>
        /// <returns>A <see cref="Task"/> object which represents the current status of the task.</returns>
        public Task SendAsync(string Line)
        {
            return Task.Run(() => Send(Line));
        }

        private void SilentStop(bool saybye = true)
        {
            Status = ConnectionStatus.Disconnected;

            if (saybye)
            {
                var Data = CryptographyClass.Encrypt("bye");
                _client.Send(Data, Data.Length);
            }

            _client.Close();
            _cancel.Cancel();

            _attempts = 0;
            _loginRespond = false;
        }

        private void SendPassword(bool incAttempt = true)
        {
            var Data = CryptographyClass.Encrypt(_password);
            _client.Send(Data, Data.Length);

            if(incAttempt)
                _attempts++;
        }
    }
}
