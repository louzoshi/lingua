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
