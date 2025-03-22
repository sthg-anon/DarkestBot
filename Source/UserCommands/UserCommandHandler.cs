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
using DarkestBot.Protocol;
using DarkestBot.UserCommands.Commands;
using Serilog;
using System.Text.Json;

namespace DarkestBot.UserCommands
{
    internal sealed class UserCommandHandler
    {
        private const string PotionPurchaseFailMessageStart = "Failed: You could not afford to buy a potion for";

        private readonly Queue<string> _potionBuyers = new();

        private readonly UserCommandMode _mode;
        private readonly IUserCommand[] _commands;

        public UserCommandHandler(JsonSerializerOptions jsonOptions, State state, ICommandSender commandSender, UserCommandMode mode)
        {
            _mode = mode;
            _commands = [
                new DataDumpCommand(jsonOptions, state),
                new BuyPotionCommand(_potionBuyers),
                new DiceBotGivePotionCommand(_potionBuyers),
                new DiceBotRefusePotionCommand(_potionBuyers)
            ];
        }

        public async Task HandleCommandAsync(string character, string message, IChatResponder responder, CancellationToken token = default)
        {
            foreach (var command in _commands)
            {
                if ((command.AllowedModes & _mode) == _mode)
                {
                    command.TryExecute(character, message, responder);
                }
            }
        }
    }
}
