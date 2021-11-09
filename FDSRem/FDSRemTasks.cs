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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FDSRem
{
    public partial class RenRemClient : IDisposable
    {
        bool ReadTaskError = false;

        private void ReadTask()
        {
            while (!_cancel.Token.IsCancellationRequested && !_disposing)
            {
                if (Status != ConnectionStatus.Disconnected)
                {
                    bool taken = false;
                    try
                    {
                        Monitor.Enter(_taskLock, ref taken);
                        while (!_cancel.Token.IsCancellationRequested && !_disposing && _client.Available > 0)
                        {
                            IPEndPoint Remote = null;
                            byte[] buf = _client.Receive(ref Remote);

                            if (Remote.Address.ToString() == Address.ToString() && Remote.Port == Port)
                            {
                                string Line = CryptographyClass.Decrypt(buf);

                                if (PreReceiveEvent(ref Line))
                                    DataReceivedEvent?.Invoke(Line);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        ReadTaskError = true;
                        ExceptionThrownEvent?.Invoke(ex);
                    }
                    finally
                    {
                        if (taken)
                        {
                            Monitor.Exit(_taskLock);
                        }
                    }
                }

                _cancel.Token.ThrowIfCancellationRequested();
                Thread.Sleep(TaskDelay);
            }
        }

        private void ParallelConnectivityWorker()
        {
            while (!_cancel.Token.IsCancellationRequested && !_disposing)
            {
                bool taken = false;
                try
                {
                    Monitor.Enter(_taskLock, ref taken);
                    InitConnection();
                    _cancel.Token.ThrowIfCancellationRequested();

                    CheckTimeout();
                    _cancel.Token.ThrowIfCancellationRequested();

                    KeepAliveTask();
                    _cancel.Token.ThrowIfCancellationRequested();
                }
                finally
                {
                    if (taken)
                    {
                        Monitor.Exit(_taskLock);
                    }
                }

                Thread.Sleep(_attempts > 1 || Status == ConnectionStatus.Connecting ? AttemptDelay : KeepAliveDelay);
            }
        }

        private int _attempts = 0;
        private bool _loginRespond = false;
        private void InitConnection()
        {
            if (_attempts < MaxAttempts)
            {
                if (Status == ConnectionStatus.Connecting)
                {
                    SendPassword();
                }
            }
            else if(!_loginRespond)
            {
                string Message = $"RenRem did not responded to connection request{(_attempts == 1 ? "" : "s")} after {_attempts} tr{(_attempts == 1 ? "y" : "ies")}.";

                SilentStop(false);
                throw new Exception(Message);
            }
        }

        private void CheckTimeout()
        {
            if (_attempts > MaxAttempts)
            {
                if (KeepAlive)
                {
                    _loginRespond = false;
                    _attempts = 0;

                    Status = ConnectionStatus.Connecting;
                }
                else
                {
                    SilentStop(false);
                }

                DisconnectedEvent?.Invoke(DisconnectReason.ClientTimeOut);
            }
        }

        private void KeepAliveTask()
        {
            if (Status != ConnectionStatus.Connecting && KeepAlive)
            {
                SendPassword();
            }
        }

        private bool PreReceiveEvent(ref string Line)
        {
            Line = Line.TrimEnd('\n');

            if (Line.StartsWith("** Connection timed out - Bye! **")) // Connection timed out.
            {
                DisconnectedEvent?.Invoke(DisconnectReason.ServerTimeOut);
                Status = ConnectionStatus.Disconnected;

                if(KeepAlive)
                {
                    SendPassword();
                }
                else
                {
                    SilentStop();
                }
            }
            else if (Line.StartsWith("** Server exiting - Connection closed! **")) // Server shutting down.
            {
                DisconnectedEvent?.Invoke(DisconnectReason.ServerShutdown);
                Status = ConnectionStatus.Disconnected;

                SilentStop(false);
            }
            else if (Line.StartsWith("Password accepted.\n"))
            {
                string[] Lines = Line.Split('\n');

                _loginRespond = true;
                _attempts = 0;

                MessageOfTheDay = string.Join(Environment.NewLine, Lines.Skip(1));

                if (Status != ConnectionStatus.Connected)
                    ConnectedEvent?.Invoke();

                Status = ConnectionStatus.Connected;
            }
            else
            {
                return true;
            }

            return false;
        }

        private async void HandleException(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                if (ex is SocketException || ex is IOException || ex is NullReferenceException || ReadTaskError)
                {
                    DisconnectedEvent?.Invoke(DisconnectReason.Error);
                    SilentStop(false);
                }

                if (!(ex is OperationCanceledException)) //Don't let them know Task failed successfully.
                {
                    ExceptionThrownEvent?.Invoke(ex);
                }
            }
        }
    }
}
