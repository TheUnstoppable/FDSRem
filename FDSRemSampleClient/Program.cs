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
using System.Text;
using System.Linq;
using System.Threading;
using FDSRem;
using System.Net;
using System.Threading.Tasks;

namespace FDSRemSampleClient
{
    class Program
    {
        static RenRemClient rem = null;
        static string Host = null;
        static int Port = 0;
        static string Pass = null;

        static string Line = "";

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Clear();

            Console.Write("Server Host: ");
            Host = Console.ReadLine();

            Console.Write("Server Port: ");
            Port = int.Parse(Console.ReadLine());

            Console.Write("Server Password: ");
            Pass = Console.ReadLine();

            Console.Write("Connection Keep Alive? (y/n): ");
            string KeepAlive = Console.ReadLine();

            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            rem = new RenRemClient(Host, Port)
            {
                KeepAlive = KeepAlive.Equals("y", StringComparison.OrdinalIgnoreCase)
            };

            rem.DataReceivedEvent += Rem_DataReceivedEvent;
            rem.ExceptionThrownEvent += Rem_ExceptionThrownEvent;
            rem.ConnectedEvent += Rem_ConnectedEvent;
            rem.DisconnectedEvent += Rem_DisconnectedEvent;

            rem.Start(Pass);

            while (true)
            {
                ConsoleKeyInfo key = default;
                while (Console.KeyAvailable)
                {
                    key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (Line.Length > 0)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write(new string(' ', Line.Length + 2));
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Line = Line.Substring(0, Line.Length - 1);
                            Console.Write("> " + Line);
                        }
                        else
                        {
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write("> ");
                        }
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        newline = true;
                        break;
                    }
                    else
                    {
                        Line += key.KeyChar;
                    }
                }

                if (key.Key != ConsoleKey.Enter)
                {
                    continue;
                }
                else
                {
                    Console.SetCursorPosition(0, Console.CursorTop + 1);
                }

                try
                {
                    if (!string.IsNullOrEmpty(Line))
                    {
                        var line = Line;
                        Line = "";

                        if (line.StartsWith("!help"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("!start: Starts the RenRemClient connection.");
                            Console.WriteLine("!stop: Stops the RenRemClient connection.");
                            Console.WriteLine("!host <host>: Change host name.");
                            Console.WriteLine("!port <port>: Change port.");
                            Console.WriteLine("!pass <password>: Change password.");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else if (line.StartsWith("!start"))
                        {
                            rem.Start(Pass);
                        }
                        else if (line.StartsWith("!stop"))
                        {
                            rem.Stop();
                        }
                        else if (line.StartsWith("!pass"))
                        {
                            Pass = line.Split(' ').Last();
                            rem.Stop();
                            rem.Start(Pass);
                        }
                        else if (line.StartsWith("!port"))
                        {
                            Port = int.Parse(line.Split(' ').Last());

                            rem.Dispose();
                            rem = new RenRemClient(Host, Port)
                            {
                                KeepAlive = KeepAlive.Equals("y", StringComparison.OrdinalIgnoreCase)
                            };

                            rem.DataReceivedEvent += Rem_DataReceivedEvent;
                            rem.ExceptionThrownEvent += Rem_ExceptionThrownEvent;
                            rem.ConnectedEvent += Rem_ConnectedEvent;
                            rem.DisconnectedEvent += Rem_DisconnectedEvent;

                            rem.Start(Pass);
                        }
                        else if (line.StartsWith("!host"))
                        {
                            Host = line.Split(' ').Last();

                            rem.Dispose();
                            rem = new RenRemClient(Host, Port)
                            {
                                KeepAlive = KeepAlive.Equals("y", StringComparison.OrdinalIgnoreCase)
                            };

                            rem.DataReceivedEvent += Rem_DataReceivedEvent;
                            rem.ExceptionThrownEvent += Rem_ExceptionThrownEvent;
                            rem.ConnectedEvent += Rem_ConnectedEvent;
                            rem.DisconnectedEvent += Rem_DisconnectedEvent;

                            rem.Start(Pass);
                        }
                        else
                        {
                            rem.Send(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("> ");
                Thread.Sleep(10);
            }
        }

        private static void Rem_DisconnectedEvent(DisconnectReason Reason)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Line.Length + 2));
            Console.SetCursorPosition(0, Console.CursorTop);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Disconnected: {Reason}");
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("> " + Line);
        }

        private static void Rem_ConnectedEvent()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Line.Length + 2));
            Console.SetCursorPosition(0, Console.CursorTop);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Connected.");
            Console.WriteLine($"=============== Message of the Day ===============\n{rem.MessageOfTheDay}\n==================================================");
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("> " + Line);
        }

        private static void Rem_ExceptionThrownEvent(Exception Exception)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Line.Length + 2));
            Console.SetCursorPosition(0, Console.CursorTop);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Exception.Message);
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("> " + Line);
        }

        static bool newline = false;
        static int lastLinePos=0;
        private static void Rem_DataReceivedEvent(string Data)
        {
            if (newline)
            {
                Console.WriteLine();
                lastLinePos = 0;
                newline = false;
            }

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Line.Length + 2));
            Console.SetCursorPosition(lastLinePos, newline ? Console.CursorTop+1 : Console.CursorTop-1);

            lastLinePos = Data.Split('\n').Last().Length;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(Data);
            Console.ForegroundColor = ConsoleColor.White;

            Console.SetCursorPosition(0, Console.CursorTop+1);
            Console.Write("> " + Line);
        }
    }
}
