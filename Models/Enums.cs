namespace Lingua.Models;

/// <summary>Nível de inglês do aluno, no padrão do Quadro Comum Europeu.</summary>
public enum EnglishLevel
{
    A1 = 1,
    A2 = 2,
    B1 = 3,
    B2 = 4,
    C1 = 5,
    C2 = 6
}

/// <summary>Onde o post foi publicado.</summary>
public enum PostScope
{
    /// <summary>Mural geral, visível para todos os alunos da escola.</summary>
    General = 1,

    /// <summary>Mural de uma turma específica.</summary>
    Classroom = 2
}

/// <summary>Estado de moderação de um conteúdo escrito por aluno.</summary>
public enum ModerationStatus
{
    /// <summary>Visível para o público do conteúdo.</summary>
    Published = 1,

    /// <summary>Aguardando liberação do professor.</summary>
    PendingReview = 2,

    /// <summary>Retirado do ar pelo professor.</summary>
    Hidden = 3
}

public enum ResourceKind
{
    Video = 1,
    Article = 2,
    Podcast = 3,
    Exercise = 4,
    Other = 5
}

/// <summary>Ações que valem ponto no ranking de interação.</summary>
public enum InteractionType
{
    PostCreated = 1,
    CommentCreated = 2,
    ReactionGiven = 3,
    ReactionReceived = 4,
    DirectMessageSent = 5,
    LessonAttended = 6,
    AssignmentSubmitted = 7
}

/// <summary>Tipo de arquivo anexado a um post.</summary>
public enum MediaKind
{
    Image = 1,
    Video = 2,
    Gif = 3
}

/// <summary>Situação do plano financeiro de um aluno.</summary>
public enum PlanStatus
{
    /// <summary>Aluno pagando e frequentando.</summary>
    Active = 1,

    /// <summary>Pausa combinada, sem cobrança no período.</summary>
    Paused = 2,

    /// <summary>Plano encerrado.</summary>
    Ended = 3
}

/// <summary>O que originou uma notificação na fila de saída.</summary>
public enum NotificationKind
{
    /// <summary>Convite de aluno, com a senha inicial.</summary>
    StudentInvite = 1,

    /// <summary>Aviso de mensalidade perto do vencimento.</summary>
    BillingReminder = 2
}

/// <summary>Situação de uma notificação na fila de saída.</summary>
public enum NotificationStatus
{
    /// <summary>Esperando o worker pegar, ou aguardando a próxima tentativa.</summary>
    Pending = 1,

    /// <summary>Entregue.</summary>
    Sent = 2,

    /// <summary>Desistiu depois de esgotar as tentativas. Fica no banco para a professora ver.</summary>
    Failed = 3
}
