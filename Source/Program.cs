/*
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

using DarkestBot.Commands;
using DarkestBot.Commands.Payloads;
using DarkestBot.Login;
using DarkestBot.Model;
using Serilog;
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

            var state = await State.LoadAsync(cancelTokenSource.Token);

            if (state == null)
            {
                Log.Error("Unable to read state file. Human intervention required.");
                return;
            }

            try
            {
                await RunAsync(state, cancelTokenSource.Token);
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

        private static async Task RunAsync(State state, CancellationToken token = default)
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

            var streamReader = new FChatStreamReader(ws, state);

            var identifyCommand = new PayloadCommand<IdentityPayload>(MessageTypes.IDN, new IdentityPayload
            {
                Method = "ticket",
                Account = ticket.Account,
                Ticket = ticket.Value,
                Character = "Darkest Bot",
                ClientName = "DarkestBot",
                ClientVersion = "0.0.1"
            });

            streamReader.EnqueueMessage(identifyCommand.MakeFChatCommand());

            if (!string.IsNullOrEmpty(state.RoomId))
            {
                var joinCommand = new PayloadCommand<JoinChannelPayload>(MessageTypes.JCH, new JoinChannelPayload
                {
                    Channel = state.RoomId
                });
                streamReader.EnqueueMessage(joinCommand.MakeFChatCommand());
            }

            await streamReader.ReadStreamAsync(token);
        }
    }
}
