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

using DarkestBot.Model;
using Serilog;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace DarkestBot.Protocol
{
    internal sealed class FChatStreamReader
    {
        private const int ReadBufferSize = 4096;
        private const double MinSecondsBetweenSend = 1.0;
        private const double DelayPadding = 0.7;

        private readonly ClientWebSocket _webSocket;
        private readonly MessageHandler _messageHandler;
        private readonly State _state;

        private readonly ConcurrentQueue<string> _outgoingMessages = new();

        public FChatStreamReader(ClientWebSocket webSocket, State state)
        {
            _webSocket = webSocket;
            _messageHandler = new MessageHandler(state);
            _state = state;
        }

        private async Task SendOutgoingMessages(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                if (_outgoingMessages.TryDequeue(out var message))
                {
                    var sendBytes = Encoding.UTF8.GetBytes(message);
                    var segment = new ArraySegment<byte>(sendBytes);
                    await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, token);
                }

                var delayLength = Math.Max(_state.ChannelMessageDelay, MinSecondsBetweenSend) + DelayPadding;
                await Task.Delay(TimeSpan.FromSeconds(delayLength), token);
            }
        }

        public void EnqueueMessage(string message)
        {
            _outgoingMessages.Enqueue(message);
        }

        public async Task ReadStreamAsync(CancellationToken token = default)
        {
            var readBuffer = new byte[ReadBufferSize];
            var memoryStream = new MemoryStream();

            var sendTask = SendOutgoingMessages(token);

            while (_webSocket.State == WebSocketState.Open)
            {
                memoryStream.Position = 0;
                memoryStream.SetLength(0);

                WebSocketReceiveResult result;
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(readBuffer), token);
                    memoryStream.Write(readBuffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log.Information("Server closing connection gracefully.");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                    break;
                }

                var message = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                var messageToSend = await _messageHandler.HandleMessageAsync(message, token);
                if (messageToSend != null)
                {
                    _outgoingMessages.Enqueue(messageToSend);
                }
            }

            await sendTask;
        }
    }
}
