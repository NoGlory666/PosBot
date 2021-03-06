// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using PosgradoBot.Common.Model.User;
using PosgradoBot.Data;

namespace PosgradoBot
{
    public class PosBot<T> : ActivityHandler where T : Dialog
    {
        private readonly BotState _userState;
        private readonly BotState _conversationState;
        private readonly Dialog _dialog;
        private readonly IDataBaseService _databaseService;

        public PosBot(UserState userState, ConversationState conversationState, T dialog, IDataBaseService databaseService)
        {
            _userState = userState;
            _conversationState = conversationState;
            _dialog = dialog;
            _databaseService = databaseService;
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hola soy el asistente virtual de la Escuela Militar de Ingenier�a Posgrado Unidad Acad�mica Santa Cruz, dime como puedo ayudarte."), cancellationToken);
                }
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await SaveUserAsync(turnContext);
            await _dialog.RunAsync(
                turnContext,
                _conversationState.CreateProperty<DialogState>(nameof(DialogState)),
                cancellationToken
                );
        }

        private async Task SaveUserAsync(ITurnContext<IMessageActivity> turnContext)
        {
            var userModel = new UserModel();
            userModel.idChannel = turnContext.Activity.From.Id;
            userModel.userNameChannel = turnContext.Activity.From.Name;
            userModel.channel = turnContext.Activity.ChannelId;
            userModel.registerDate = DateTime.Now;

            var user = await _databaseService.User.FirstOrDefaultAsync(x => x.idChannel == turnContext.Activity.From.Id);
            if (user == null)
            {
                _databaseService.User.Add(userModel);
                _databaseService.SaveChangesSQL();
            }
        }
    }
}
