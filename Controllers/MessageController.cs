using Lingua.Data;
using Lingua.Extensions;
using Lingua.Hubs;
using Lingua.Models;
using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Controllers;

public class MessageController : ApiController
{
    /// <summary>Caixa de entrada com a última mensagem e o que ainda não foi lido.</summary>
    [HttpGet("v1/conversations")]
    public async Task<IActionResult> GetConversationsAsync([FromServices] LinguaDataContext context)
    {
        var userId = CurrentUserId;

        var conversations = await context
            .Conversations
            .AsNoTracking()
            .Where(x => x.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(x => x.LastMessageAt)
            .Select(x => new ConversationSummaryViewModel
            {
                Id = x.Id,
                LastMessageAt = x.LastMessageAt,
                OtherUserId = x.Participants.Where(p => p.UserId != userId).Select(p => p.User.Id).FirstOrDefault(),
                OtherUserName = x.Participants.Where(p => p.UserId != userId).Select(p => p.User.Name).FirstOrDefault()!,
                OtherUserSlug = x.Participants.Where(p => p.UserId != userId).Select(p => p.User.Slug).FirstOrDefault()!,
                OtherUserImage = x.Participants.Where(p => p.UserId != userId).Select(p => p.User.Image).FirstOrDefault(),
                LastMessage = x.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Body).FirstOrDefault(),
                UnreadCount = x.Messages.Count(m =>
                    m.SenderId != userId &&
                    m.SentAt > x.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAt).FirstOrDefault())
            })
            .ToListAsync();

        return Ok(ResultViewModel<List<ConversationSummaryViewModel>>.Success(conversations));
    }

    /// <summary>Quem o usuário pode chamar no privado: colegas de turma e professores.</summary>
    [HttpGet("v1/conversations/contacts")]
    public async Task<IActionResult> GetContactsAsync([FromServices] AccessService access)
    {
        var contacts = await access.ContactsForAsync(CurrentUserId);

        return Ok(ResultViewModel<dynamic>.Success(
            contacts.Select(x => new { x.Id, x.Name, x.Slug, x.Image, x.Level })));
    }

    [HttpPost("v1/conversations")]
    public async Task<IActionResult> StartAsync(
        [FromBody] StartConversationViewModel model,
        [FromServices] ConversationService conversations)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var conversation = await conversations.GetOrCreateAsync(CurrentUserId, model.UserId);

        return conversation == null
            ? BadRequest(ResultViewModel<string>.Fail("Vocês não dividem nenhuma turma"))
            : Ok(ResultViewModel<dynamic>.Success(new { conversation.Id }));
    }

    [HttpGet("v1/conversations/{id:int}/messages")]
    public async Task<IActionResult> GetMessagesAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context,
        [FromServices] ConversationService conversations,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        if (!await conversations.IsParticipantAsync(id, CurrentUserId))
            return Forbid();

        pageSize = Math.Clamp(pageSize, 1, 100);

        var messages = await context
            .DirectMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == id && x.Status != ModerationStatus.Hidden)
            .OrderByDescending(x => x.SentAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new DirectMessageViewModel
            {
                Id = x.Id,
                ConversationId = x.ConversationId,
                Body = x.Body,
                SentAt = x.SentAt,
                SenderId = x.SenderId,
                SenderName = x.Sender.Name,
                SenderImage = x.Sender.Image,
                IsFlagged = x.IsFlagged
            })
            .ToListAsync();

        messages.Reverse();
        await conversations.MarkAsReadAsync(id, CurrentUserId);

        return Ok(ResultViewModel<List<DirectMessageViewModel>>.Success(messages));
    }

    [HttpPost("v1/conversations/{id:int}/messages")]
    public async Task<IActionResult> SendAsync(
        [FromRoute] int id,
        [FromBody] SendMessageViewModel model,
        [FromServices] ConversationService conversations,
        [FromServices] IHubContext<ChatHub> hub)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var message = await conversations.SendAsync(id, CurrentUserId, model.Body);
        if (message == null)
            return Forbid();

        var payload = ConversationService.ToViewModel(message);

        var recipients = await conversations.RecipientsAsync(id, CurrentUserId);
        var groups = recipients.Select(ChatHub.GroupFor).Append(ChatHub.GroupFor(CurrentUserId)).ToList();
        await hub.Clients.Groups(groups).SendAsync("ReceiveMessage", payload);

        return Created($"v1/conversations/{id}", ResultViewModel<DirectMessageViewModel>.Success(payload));
    }

    /// <summary>Sinaliza uma mensagem para o professor revisar.</summary>
    [HttpPost("v1/messages/{id:int}/flag")]
    public async Task<IActionResult> FlagAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context,
        [FromServices] ConversationService conversations)
    {
        var message = await context.DirectMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (message == null)
            return NotFound(ResultViewModel<string>.Fail("Mensagem não encontrada"));

        if (!await conversations.IsParticipantAsync(message.ConversationId, CurrentUserId))
            return Forbid();

        message.IsFlagged = true;
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Mensagem enviada para revisão"));
    }
}
