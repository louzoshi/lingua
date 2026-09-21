# Lingua

Plataforma de convivência para uma escola de inglês: os alunos publicam em murais, conversam
por mensagem direta, acompanham as aulas gravadas e entregam os trabalhos — tudo em um espaço
fechado, onde a professora vê e modera o que acontece.

O projeto nasceu de uma API de blog e foi reescrito em cima daquela base (autenticação JWT,
Entity Framework com mapeamento explícito, envelope de resposta padronizado).

## O problema

Uma professora de inglês queria um lugar onde os alunos usassem o idioma entre eles fora da
aula. Grupo de mensagens comum não servia: ela não conseguia separar turmas, acompanhar quem
estava participando nem moderar o que era publicado.

## O que a plataforma faz

**Murais**
- Mural geral, visível para todos os alunos matriculados em alguma turma
- Mural por turma, restrito a quem está matriculado
- Posts com texto, fotos, vídeos e GIFs; emoji em qualquer campo de texto
- Assuntos (Grammar, Free Talk, Culture...), tags, curtidas, comentários e respostas a comentários
- Autor edita e apaga o que escreveu; professora edita, esconde ou apaga qualquer post ou comentário

**Mensagens diretas**
- Conversa privada entre duas pessoas, entregue em tempo real, com emoji e GIF
- Qualquer aluno ativo fala com qualquer colega da escola e com a professora
- Aluno sinaliza uma mensagem para a professora; a professora lê qualquer conversa pela
  moderação e esconde mensagens que passem do ponto — os alunos sabem disso na própria tela

**Área de estudo**
- Aulas agendadas com link do encontro
- Gravação publicada depois da aula
- Presença registrada pelo aluno, que vira histórico no perfil
- Materiais (vídeo, artigo, podcast, exercício) por turma ou para a escola inteira

**Trabalhos**
- Link do enunciado e prazo, por turma
- Aluno entrega o link do trabalho; reenvio atualiza a mesma entrega
- Professora corrige com nota e feedback

**Perfis e ranking**
- Perfil com nível (A1 a C2), bio, turmas e estatísticas
- Ranking de interação por turma e por período

**Financeiro (só professora)**
- Pacotes vendidos, com preço de tabela e aulas por semana
- Plano de cada aluno: pacote, valor cobrado, dia do vencimento, situação e observações
- Contrato assinado em PDF, guardado fora do `wwwroot` e servido só para a professora
- Mensalidades recebidas e visão do mês: quem pagou, quem falta, quanto entrou, quem não tem contrato

A pontuação do ranking premia o que faz a turma conversar:

| Ação | Pontos |
| --- | --- |
| Publicar um post | 10 |
| Comentar | 4 |
| Entregar um trabalho | 20 |
| Participar de uma aula | 15 |
| Receber uma curtida | 2 |
| Curtir / mandar mensagem | 1 |

## Controle de acesso

Não existe cadastro aberto: a professora convida cada aluno e a senha inicial vai por e-mail.
As regras de visibilidade ficam todas em `Services/AccessService.cs` — nenhum controller ou
página decide sozinho o que alguém pode ver.

A professora controla o acesso em três níveis, todos pela aba **Alunos** de Gerenciar:

| Ação | Efeito |
| --- | --- |
| Remover da turma | Aluno perde o mural, as aulas, os trabalhos e os colegas daquela turma |
| Sem nenhuma turma ativa | Perde também o mural geral, os materiais da escola e os contatos; só fala com a professora |
| Desativar conta | Login recusado e sessão derrubada na hora (`Services/SessionGuard.cs`); some dos contatos, do ranking e das listas de colegas. Nada é apagado; dá para reativar |
| Arquivar turma | Turma some para os alunos; matrículas nela deixam de contar como acesso |
| Esconder tudo | Curadoria em lote: tira do ar todos os posts e comentários de um aluno, sem apagar |

| Perfil | Pode |
| --- | --- |
| Aluno | Ler e publicar nos murais que alcança, conversar com quem divide turma, entregar trabalhos |
| Professor | Tudo do aluno, mais criar turmas, matricular, agendar aulas, publicar materiais e trabalhos, corrigir e moderar |
| Admin | Tudo do professor, mais gerenciar professores |

## Tecnologias

- .NET 10 / ASP.NET Core
- Blazor Server (interface) e API REST com JWT (clientes externos)
- Entity Framework Core — SQLite em desenvolvimento, PostgreSQL em produção
- SignalR para as mensagens diretas
- `Lingua.Notifications` — worker .NET separado, que entrega o que sai da plataforma
- Swagger em desenvolvimento

## Rodando

```bash
dotnet run
```

Sobe em `https://localhost:5001`, cria o banco SQLite (`lingua.db`) e semeia papéis, assuntos
e a conta de professor. A senha inicial dessa conta aparece no log do primeiro boot:

```
warn: Seeder[0] Conta de professor criada: teacher@lingua.local / senha inicial: ...
```

Para escolher o e-mail, o nome e a senha dessa conta:

```bash
dotnet user-secrets set "Seed:TeacherName" "Nome da professora"
dotnet user-secrets set "Seed:TeacherEmail" "professora@exemplo.com"
dotnet user-secrets set "Seed:TeacherPassword" "uma_senha_forte"
```

### Configuração

```bash
dotnet user-secrets set "JwtKey" "sua_chave_com_pelo_menos_32_caracteres"
dotnet user-secrets set "Tenor:ApiKey" "chave_do_tenor"
```

O SMTP não fica mais aqui: quem entrega e-mail é o worker de notificações, e a configuração
dele está em [Notificações](#notificações). Sem chave do Tenor, o seletor de GIF aceita só o
link de um GIF colado à mão.

Arquivos enviados ficam fora do git: mídia de posts em `wwwroot/uploads/` (servida como
estático, com nome aleatório) e contratos em `App_Data/contracts/` (servidos só pela rota
`/financeiro/contratos/{id}`, restrita à professora). Em produção, esses dois diretórios
precisam de volume persistente.

### Produção com PostgreSQL

```json
{
  "Database": { "Provider": "postgres" },
  "ConnectionStrings": { "DefaultConnection": "Host=...;Database=lingua;Username=...;Password=..." },
  "PublicUrl": "https://seu-dominio"
}
```

Migration de EF é específica de banco, então existem dois conjuntos, um por provider, cada um
com seu contexto:

```bash
dotnet ef migrations add NomeDaMigration --context SqliteDataContext   --output-dir Migrations/Sqlite   --namespace Lingua.Migrations.Sqlite
dotnet ef migrations add NomeDaMigration --context PostgresDataContext --output-dir Migrations/Postgres --namespace Lingua.Migrations.Postgres
```

Toda mudança de modelo precisa dos dois comandos. A aplicação aplica as migrations do provider
configurado na inicialização. O worker de notificações não tem migration nenhuma: o dono do
schema é esta aplicação, e ele só lê e escreve nas tabelas que ela cria.

## Organização

```
Components/           Interface em Blazor (Pages, Layout, Shared)
Controllers/          API REST, uma controller por área
Services/             Regras de negócio: acesso, posts, conversas, ranking, perfis
Models/               Entidades
Data/                 Contextos por provider, mapeamentos e seed
ViewModels/           Entrada e saída da API e dos formulários
Hubs/                 Hub SignalR das mensagens
Migrations/           Um conjunto por provider
Lingua.Notifications/ Worker de notificações, projeto à parte
```

Três decisões que valem explicação:

- `Services/ScopeRunner.cs` — no Blazor Server o escopo de DI dura o circuito inteiro. As
  páginas pedem os serviços por ele, e cada operação ganha um `DbContext` novo e descartável
  em vez de um que viveria horas acumulando entidades.
- `Services/ChatNotifier.cs` — a página de mensagens roda no próprio servidor, então escuta um
  notificador em processo em vez de abrir um cliente SignalR contra o hub local. O hub segue
  disponível para clientes externos da API.
- `Services/NotificationService.cs` — a aplicação não abre conexão com servidor de e-mail. Ela
  grava a mensagem na tabela `Notification` e devolve a tela; entregar é do worker. Veja
  [Notificações](#notificações).

## Notificações

Tudo que sai da plataforma para fora — hoje, o convite com a senha inicial e o lembrete de
mensalidade — passa por um serviço próprio, em outro processo: `Lingua.Notifications`.

Ele existe por dois motivos concretos:

- **Nada de SMTP dentro da requisição.** Antes, convidar um aluno abria uma conexão com o
  servidor de e-mail e só devolvia a tela quando ela respondesse. Um servidor lento travava a
  professora; um servidor fora do ar perdia o convite, que virava uma linha de log. Agora a
  aplicação grava a mensagem e responde na hora.
- **Trabalho que ninguém pede.** O financeiro já sabe quem vence quando e quem ainda não pagou
  o mês, mas um app web só faz o que alguém clicou. Lembrar de cobrar precisa de alguém
  acordando sozinho todo dia.

### Como a fila funciona

A fila é a tabela `Notification`, no mesmo banco — não há broker à parte. Isso é de propósito:
gravar a notificação entra na **mesma transação** do que a originou, então criar o aluno e
enfileirar o convite dele não podem dar certo pela metade.

O worker faz duas coisas:

| Componente | O que faz |
| --- | --- |
| `OutboxDispatcher` | Varre o que está pendente, entrega por SMTP, tenta de novo com backoff exponencial e desiste depois de `MaxAttempts` — a linha vira `Failed` e fica no banco, com o erro |
| `BillingReminderScheduler` | Uma vez por dia, enfileira aviso para quem tem mensalidade vencendo em até `DaysBefore` dias e ainda não pagou o mês |

Dois detalhes que evitam dor de cabeça na operação:

- **Reserva por tempo, não por lápide.** Ao pegar um lote, o despachante empurra `NextAttemptAt`
  para a frente. Se o processo morrer no meio do envio, as mensagens voltam a ser elegíveis
  sozinhas quando a reserva vence — sem estado preso nem limpeza manual.
- **Chave de idempotência.** O lembrete usa `DedupeKey = billing:{plano}:{ano-mês}`, com índice
  único no banco. Rodar de novo, reiniciar o worker ou subir duas instâncias não manda o aviso
  duas vezes.

Sem SMTP configurado a plataforma continua funcionando como sempre funcionou: as mensagens
ficam na fila, sem gastar tentativa, e saem assim que houver servidor. A senha inicial do aluno
segue aparecendo na tela de gerenciar e no log.

### Rodando o worker

```bash
cd Lingua.Notifications
dotnet run
```

Em desenvolvimento ele aponta para o mesmo `lingua.db` do app (`../lingua.db`). Em produção é a
mesma string de conexão do PostgreSQL, e o serviço sobe como um processo próprio, ao lado do
app — dois containers, um banco.

### Configuração do worker

```bash
cd Lingua.Notifications
dotnet user-secrets set "SmtpConfiguration:Host" "smtp.exemplo.com"
dotnet user-secrets set "SmtpConfiguration:UserName" "usuario"
dotnet user-secrets set "SmtpConfiguration:Password" "senha"
```

| Chave | Padrão | O que é |
| --- | --- | --- |
| `SmtpConfiguration:Host` | vazio | Vazio desliga a entrega; a fila só acumula |
| `SmtpConfiguration:EnableSsl` | `true` | STARTTLS. Desligue só contra um relay local, como o MailHog |
| `Dispatcher:PollSeconds` | `15` | Espera entre varreduras com a fila vazia |
| `Dispatcher:BatchSize` | `20` | Mensagens por lote |
| `Dispatcher:MaxAttempts` | `5` | Tentativas antes de desistir |
| `Dispatcher:LeaseMinutes` | `5` | Reserva de um lote, para o caso de o worker cair no meio |
| `Billing:Enabled` | `false` | **Desligado por padrão**: cobrar aluno automaticamente é decisão da escola |
| `Billing:RunAtHourUtc` | `12` | Hora UTC da rodada diária (12 UTC ≈ 9h de Brasília) |
| `Billing:DaysBefore` | `3` | Antecedência do aviso, em dias |

## API

Autenticação por `Authorization: Bearer <token>` de `POST /v1/accounts/login`.
Swagger em `/swagger` no ambiente de desenvolvimento.

| Área | Endpoints |
| --- | --- |
| Contas | `POST /v1/accounts`, `POST /v1/accounts/login`, `PUT /v1/accounts/me`, `PUT /v1/accounts/me/password`, `POST /v1/accounts/me/image` |
| Turmas | `GET|POST /v1/classrooms`, `GET|PUT|DELETE /v1/classrooms/{id}`, `POST|DELETE /v1/classrooms/{id}/students` |
| Posts | `GET|POST /v1/posts`, `GET|PUT|DELETE /v1/posts/{id}`, `POST /v1/posts/{id}/reactions`, `POST /v1/posts/{id}/comments`, `PUT /v1/posts/{id}/moderation` |
| Assuntos | `GET|POST /v1/topics`, `DELETE /v1/topics/{id}` |
| Mensagens | `GET|POST /v1/conversations`, `GET /v1/conversations/contacts`, `GET|POST /v1/conversations/{id}/messages`, `POST /v1/messages/{id}/flag` |
| Estudo | `GET|POST /v1/lessons`, `PUT|DELETE /v1/lessons/{id}`, `POST /v1/lessons/{id}/attendance`, `GET|POST /v1/resources` |
| Trabalhos | `GET|POST /v1/assignments`, `PUT|DELETE /v1/assignments/{id}`, `POST /v1/assignments/{id}/submissions`, `GET /v1/assignments/{id}/submissions`, `PUT /v1/submissions/{id}/grade` |
| Perfis e ranking | `GET /v1/profiles/{slug}`, `GET /v1/ranking` |

## Licença

MIT.
