﻿/*
 * Copyright (c) 2025 Aller
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using DarkestBot.Login;
using DarkestBot.Model;
using DarkestBot.Protocol;
using DarkestBot.Protocol.Commands;
using DarkestBot.Protocol.Commands.Payloads;
using Serilog;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace DarkestBot
{
    public class Program
    {
        static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var cancelTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                cancelTokenSource.Cancel();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                cancelTokenSource.Cancel();
            };

            var stateManager = await StateManager.LoadAsync(cancelTokenSource.Token);

            if (stateManager == null)
            {
                Log.Error("Unable to read state file. Human intervention required.");
                return;
            }

            try
            {
                await RunAsync(stateManager, cancelTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Log.Information("Shutting bot down.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred while running the bot!");
            }
        }

        private static async Task RunAsync(StateManager stateManager, CancellationToken token = default)
        {
            var ticketFactory = new TicketFactory();
            var ticket = await ticketFactory.GetTicketAsync(token);
            if (ticket == null)
            {
                Log.Fatal("Could not get ticket!");
                return;
            }

            using var ws = new ClientWebSocket();
            var serverUri = new Uri("wss://chat.f-list.net/chat2-dev");
            await ws.ConnectAsync(serverUri, token);
            Log.Information("Connected!");

            var outgoingCommandQueue = new ConcurrentCommandQueue();
            var messageHandler = new MessageHandler(stateManager, outgoingCommandQueue);
            var streamReader = new FChatStreamReader(ws, stateManager, outgoingCommandQueue, messageHandler);

            var identifyCommand = CommandFactory.Identify(
                method: "ticket",
                account: ticket.Account,
                ticket: ticket.Value,
                character: "Darkest Bot",
                clientName: "DarkestBot",
                clientVersion: "0.0.1");

            outgoingCommandQueue.SendCommand(identifyCommand);

            if (!string.IsNullOrEmpty(stateManager.State.RoomId))
            {
                var joinCommand = CommandFactory.JoinChannel(stateManager.State.RoomId);
                outgoingCommandQueue.SendCommand(joinCommand);
            }

            await streamReader.ReadStreamAsync(token);
        }
    }
}
